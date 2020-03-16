﻿// Copyright 2018 ThoughtWorks, Inc.
//
// This file is part of Gauge-Dotnet.
//
// Gauge-Dotnet is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Gauge-Dotnet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Gauge-Dotnet.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Gauge.CSharp.Lib;
using Gauge.Dotnet.Extensions;
using Gauge.Dotnet.Models;
using Gauge.Dotnet.Wrappers;

namespace Gauge.Dotnet
{
    public class AssemblyLoader : IAssemblyLoader
    {
        private const string GaugeLibAssembleName = "Gauge.CSharp.Lib";
        private readonly IAssemblyWrapper _assemblyWrapper;
        private readonly IReflectionWrapper _reflectionWrapper;
        private Assembly _targetLibAssembly;

        private readonly IActivatorWrapper _activatorWrapper;
        private readonly IStepRegistry _registry;

        public AssemblyLoader(IAssemblyWrapper assemblyWrapper, IEnumerable<string> assemblyLocations,
            IReflectionWrapper reflectionWrapper, IActivatorWrapper activatorWrapper, IStepRegistry registry)
        {
            _assemblyWrapper = assemblyWrapper;
            _reflectionWrapper = reflectionWrapper;
            _activatorWrapper = activatorWrapper;
            AssembliesReferencingGaugeLib = new List<Assembly>();
            _registry = registry;
            foreach (var location in assemblyLocations)
                ScanAndLoad(location);

            LoadTargetLibAssembly();
            SetDefaultTypes();
        }

        public List<Assembly> AssembliesReferencingGaugeLib { get; }
        public Type ScreenshotWriter { get; private set; }
        public Type ClassInstanceManagerType { get; private set; }

        public IEnumerable<MethodInfo> GetMethods(LibType type)
        {
            var attributeType = _targetLibAssembly.GetType(type.FullName());

            bool MethodFilter(MethodInfo info)
            {
                return info.GetCustomAttributes(false)
                    .Any(attributeType.IsInstanceOfType);
            }

            IEnumerable<MethodInfo> MethodSelector(Type t)
            {
                return _reflectionWrapper.GetMethods(t).Where(MethodFilter);
            }

            return AssembliesReferencingGaugeLib.SelectMany(assembly => assembly.GetTypes().SelectMany(MethodSelector));
        }

        public Type GetLibType(LibType type)
        {
            return _targetLibAssembly.GetType(type.FullName());
        }


        public IStepRegistry GetStepRegistry()
        {
            var infos = GetMethods(LibType.Step);
            foreach (var info in infos)
            {
                var stepTexts = info.GetCustomAttributes(GetLibType(LibType.Step))
                    .SelectMany(x => x.GetType().GetProperty("Names").GetValue(x, null) as string[]);
                foreach (var stepText in stepTexts)
                {
                    var stepValue = GetStepValue(stepText);
                    if (_registry.ContainsStep(stepValue))
                    {
                        _registry.MethodFor(stepValue).MethodInfo = info;
                        _registry.MethodFor(stepValue).ContinueOnFailure = info.IsRecoverableStep(this);
                    }
                    else
                    {
                        var hasAlias = stepTexts.Count() > 1;
                        var stepMethod = new GaugeMethod
                        {
                            Name = info.FullyQuallifiedName(),
                            ParameterCount = info.GetParameters().Length,
                            StepText = stepText,
                            HasAlias = hasAlias,
                            Aliases = stepTexts,
                            MethodInfo = info,
                            ContinueOnFailure = info.IsRecoverableStep(this),
                            StepValue = stepValue,
                            IsExternal = true,
                        };
                        _registry.AddStep(stepValue, stepMethod);
                    }

                }
            }
            return _registry;
        }

        public object GetClassInstanceManager()
        {
            if (ClassInstanceManagerType == null) return null;
            var classInstanceManager = _activatorWrapper.CreateInstance(ClassInstanceManagerType);
            Logger.Debug("Loaded Instance Manager of Type:" + classInstanceManager.GetType().FullName);
            _reflectionWrapper.InvokeMethod(ClassInstanceManagerType, classInstanceManager, "Initialize",
                AssembliesReferencingGaugeLib);
            return classInstanceManager;
        }

        private static string GetStepValue(string stepText)
        {
            return Regex.Replace(stepText, @"(<.*?>)", @"{}");
        }

        private void ScanAndLoad(string path)
        {
            Logger.Debug($"Loading assembly from : {path}");
            var assembly = _assemblyWrapper.LoadFrom(path);

            var isReferencingGaugeLib = assembly.GetReferencedAssemblies()
                .Select(name => name.Name)
                .Contains(GaugeLibAssembleName);

            if (!isReferencingGaugeLib)
                return;

            AssembliesReferencingGaugeLib.Add(assembly);


            try
            {
                if (ScreenshotWriter is null)
                    ScanForCustomScreenshotWriter(assembly.GetTypes());

                if (ScreenshotWriter is null)
                    ScanForCustomScreengrabber(assembly.GetTypes());

                if (ClassInstanceManagerType is null)
                    ScanForCustomInstanceManager(assembly.GetTypes());
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var e in ex.LoaderExceptions)
                    Logger.Error(e.ToString());
            }
        }

        private void ScanForCustomScreenshotWriter(IEnumerable<Type> types)
        {
            var implementingTypes = types.Where(type =>
                type.GetInterfaces().Any(t => t.FullName == "Gauge.CSharp.Lib.ICustomScreenshotWriter"));
            ScreenshotWriter = implementingTypes.FirstOrDefault();
            if (ScreenshotWriter is null) return;
            var csg = (ICustomScreenshotWriter)_activatorWrapper.CreateInstance(ScreenshotWriter);
            GaugeScreenshots.RegisterCustomScreenshotWriter(csg);
        }

        private void ScanForCustomScreengrabber(IEnumerable<Type> types)
        {
            var implementingTypes = types.Where(type =>
                type.GetInterfaces().Any(t => t.FullName == "Gauge.CSharp.Lib.ICustomScreenshotGrabber"));
            ScreenshotWriter = implementingTypes.FirstOrDefault();
            if (ScreenshotWriter is null) return;
            var csg = (ICustomScreenshotGrabber)_activatorWrapper.CreateInstance(ScreenshotWriter);
            GaugeScreenshots.RegisterCustomScreenshotGrabber(csg);
        }

        private void ScanForCustomInstanceManager(IEnumerable<Type> types)
        {
            var implementingTypes = types.Where(type =>
                type.GetInterfaces().Any(t => t.FullName == "Gauge.CSharp.Lib.IClassInstanceManager"));
            ClassInstanceManagerType = implementingTypes.FirstOrDefault();
        }

        private void SetDefaultTypes()
        {
            ClassInstanceManagerType = ClassInstanceManagerType ??
                                       _targetLibAssembly.GetType(LibType.DefaultClassInstanceManager.FullName());
            ScreenshotWriter =
                ScreenshotWriter ?? _targetLibAssembly.GetType(LibType.DefaultScreenshotWriter.FullName());
        }

        private void LoadTargetLibAssembly()
        {
            _targetLibAssembly = _assemblyWrapper.GetCurrentDomainAssemblies()
                .First(x => string.CompareOrdinal(x.GetName().Name, GaugeLibAssembleName) == 0);
        }
    }
}