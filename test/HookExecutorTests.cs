﻿/*----------------------------------------------------------------
 *  Copyright (c) ThoughtWorks, Inc.
 *  Licensed under the Apache License, Version 2.0
 *  See LICENSE.txt in the project root for license information.
 *----------------------------------------------------------------*/


using System;
using System.Collections.Generic;
using System.Reflection;
using Gauge.Dotnet.Strategy;
using Gauge.Dotnet.UnitTests.Helpers;
using Gauge.Dotnet.Wrappers;
using Gauge.CSharp.Lib;
using Gauge.Messages;
using Moq;
using NUnit.Framework;

namespace Gauge.Dotnet.UnitTests
{
    [TestFixture]
    internal class HookExecutorTests
    {
        [Test]
        public void ShoudExecuteHooks()
        {
            var mockInstance = new Mock<object>().Object;
            var mockClassInstanceManagerType = new Mock<Type>().Object;
            var mockClassInstanceManager = new Mock<object>().Object;

            var mockAssemblyLoader = new Mock<IAssemblyLoader>();
            var type = LibType.BeforeSuite;
            var methodInfo = new MockMethodBuilder(mockAssemblyLoader)
                .WithName($"{type}Hook")
                .WithFilteredHook(type)
                .WithDeclaringTypeName("my.foo.type")
                .WithNoParameters()
                .Build();
            mockAssemblyLoader.Setup(x => x.GetMethods(type)).Returns(new List<MethodInfo> {methodInfo});
            mockAssemblyLoader.Setup(x => x.ClassInstanceManagerType).Returns(mockClassInstanceManagerType);

            var mockReflectionWrapper = new Mock<IReflectionWrapper>();
            mockReflectionWrapper
                .Setup(x => x.InvokeMethod(mockClassInstanceManagerType, mockClassInstanceManager, "Get",
                    methodInfo.DeclaringType))
                .Returns(mockInstance);
            mockReflectionWrapper.Setup(x => x.Invoke(methodInfo, mockInstance, new List<object>()));

            var mockExecutionInfoMapper = new Mock<IExecutionInfoMapper>();
            mockExecutionInfoMapper.Setup(x => x.ExecutionInfoFrom(It.IsAny<ExecutionInfo>())).Returns(new {});

            var executor = new HookExecutor(mockAssemblyLoader.Object, mockReflectionWrapper.Object,
                mockClassInstanceManager, mockExecutionInfoMapper.Object);

            var result = executor.Execute("BeforeSuite", new HooksStrategy(), new List<string>(),
                new ExecutionInfo());
            Assert.True(result.Success, $"Hook execution failed: {result.ExceptionMessage}\n{result.StackTrace}");
        }

        [Test]
        public void ShoudExecuteHooksWithExecutionContext()
        {
            var mockInstance = new Mock<object>().Object;
            var mockClassInstanceManagerType = new Mock<Type>().Object;
            var mockClassInstanceManager = new Mock<object>().Object;

            var mockAssemblyLoader = new Mock<IAssemblyLoader>();
            var type = LibType.BeforeSuite;
            var methodInfo = new MockMethodBuilder(mockAssemblyLoader)
                .WithName($"{type}Hook")
                .WithFilteredHook(type)
                .WithDeclaringTypeName("my.foo.type")
                .WithParameters(new KeyValuePair<Type, string>(typeof(ExecutionContext), "context"))
                .Build();
            mockAssemblyLoader.Setup(x => x.GetMethods(type)).Returns(new List<MethodInfo> {methodInfo});
            mockAssemblyLoader.Setup(x => x.ClassInstanceManagerType).Returns(mockClassInstanceManagerType);

            var executionInfo = new ExecutionInfo();
            var mockReflectionWrapper = new Mock<IReflectionWrapper>();
            mockReflectionWrapper
                .Setup(x => x.InvokeMethod(mockClassInstanceManagerType, mockClassInstanceManager, "Get",
                    methodInfo.DeclaringType))
                .Returns(mockInstance);
            var expectedExecutionInfo = new ExecutionContext();

            var mockExecutionInfoMapper = new Mock<IExecutionInfoMapper>();
            mockExecutionInfoMapper.Setup(x => x.ExecutionInfoFrom(executionInfo))
                .Returns(expectedExecutionInfo);

            mockReflectionWrapper.Setup(x => x.Invoke(methodInfo, mockInstance, expectedExecutionInfo))
                .Verifiable();

            var executor = new HookExecutor(mockAssemblyLoader.Object, mockReflectionWrapper.Object,
                mockClassInstanceManager, mockExecutionInfoMapper.Object);

            var result = executor.Execute("BeforeSuite", new HooksStrategy(), new List<string>(),
                executionInfo);
            Assert.True(result.Success, $"Hook execution failed: {result.ExceptionMessage}\n{result.StackTrace}");
            mockReflectionWrapper.VerifyAll();
        }

        [Test]
        public void ShoudExecuteHooksAndGetTheError()
        {
            var mockInstance = new Mock<object>().Object;
            var mockClassInstanceManagerType = new Mock<Type>().Object;
            var mockClassInstanceManager = new Mock<object>().Object;

            var mockAssemblyLoader = new Mock<IAssemblyLoader>();
            var type = LibType.BeforeSuite;
            var methodInfo = new MockMethodBuilder(mockAssemblyLoader)
                .WithName($"{type}Hook")
                .WithFilteredHook(type)
                .WithDeclaringTypeName("my.foo.type")
                .Build();
            mockAssemblyLoader.Setup(x => x.GetMethods(type)).Returns(new List<MethodInfo> {methodInfo});
            mockAssemblyLoader.Setup(x => x.ClassInstanceManagerType).Returns(mockClassInstanceManagerType);

            var mockReflectionWrapper = new Mock<IReflectionWrapper>();
            mockReflectionWrapper
                .Setup(x => x.InvokeMethod(mockClassInstanceManagerType, mockClassInstanceManager, "Get",
                    methodInfo.DeclaringType))
                .Returns(mockInstance);

            var mockExecutionInfoMapper = new Mock<IExecutionInfoMapper>();
            mockExecutionInfoMapper.Setup(x => x.ExecutionInfoFrom(It.IsAny<ExecutionInfo>()))
                .Returns(new {Foo = "bar"});
            var executor = new HookExecutor(mockAssemblyLoader.Object, mockReflectionWrapper.Object,
                mockClassInstanceManager, mockExecutionInfoMapper.Object);
            mockReflectionWrapper.Setup(x => x.Invoke(methodInfo, mockInstance))
                .Throws(new Exception("hook failed"));

            var result = executor.Execute("BeforeSuite", new HooksStrategy(), new List<string>(),
                new ExecutionInfo());
            Assert.False(result.Success, "Hook execution passed, expected failure");
            Assert.AreEqual(result.ExceptionMessage, "hook failed");
        }
    }
}