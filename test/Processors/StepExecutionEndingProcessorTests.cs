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
using System.Text;
using Gauge.CSharp.Lib;
using Gauge.Dotnet.Models;
using Gauge.Dotnet.Processors;
using Gauge.Dotnet.Strategy;
using Gauge.Dotnet.UnitTests.Helpers;
using Gauge.Messages;
using Moq;
using NUnit.Framework;

namespace Gauge.Dotnet.UnitTests.Processors
{
    internal class StepExecutionEndingProcessorTests
    {
        private readonly IEnumerable<string> _pendingMessages = new List<string> {"Foo", "Bar"};

        private readonly IEnumerable<byte[]> _pendingScreenshots =
            new List<byte[]> {Encoding.ASCII.GetBytes("SCREENSHOT")};

        private Mock<IExecutionOrchestrator> _mockMethodExecutor;
        private ProtoExecutionResult _protoExecutionResult;
        private StepExecutionEndingRequest _stepExecutionEndingRequest;
        private StepExecutionEndingProcessor _stepExecutionEndingProcessor;

        [SetUp]
        public void Setup()
        {
            var mockHookRegistry = new Mock<IHookRegistry>();
            var mockAssemblyLoader = new Mock<IAssemblyLoader>();
            var mockMessageCollectorType = new Mock<Type>();
            var mockScreenshtCollectorType = new Mock<Type>();

            mockAssemblyLoader.Setup(x => x.GetLibType(LibType.MessageCollector))
                .Returns(mockMessageCollectorType.Object);
            mockAssemblyLoader.Setup(x => x.GetLibType(LibType.ScreenshotCollector))
                .Returns(mockScreenshtCollectorType.Object);
            var mockMethod = new MockMethodBuilder(mockAssemblyLoader)
                .WithName("Foo")
                .WithFilteredHook(LibType.BeforeSpec)
                .Build();
            var hooks = new HashSet<IHookMethod>
            {
                new HookMethod(LibType.BeforeSpec, mockMethod, mockAssemblyLoader.Object)
            };
            mockHookRegistry.Setup(x => x.AfterStepHooks).Returns(hooks);
            _stepExecutionEndingRequest = new StepExecutionEndingRequest
            {
                CurrentExecutionInfo = new ExecutionInfo
                {
                    CurrentSpec = new SpecInfo(),
                    CurrentScenario = new ScenarioInfo()
                }
            };

            _mockMethodExecutor = new Mock<IExecutionOrchestrator>();
            _protoExecutionResult = new ProtoExecutionResult
            {
                ExecutionTime = 0,
                Failed = false
            };

            _mockMethodExecutor.Setup(x =>
                    x.ExecuteHooks("AfterStep", It.IsAny<HooksStrategy>(), It.IsAny<IList<string>>(),
                        It.IsAny<ExecutionContext>()))
                .Returns(_protoExecutionResult);
            _mockMethodExecutor.Setup(x =>
                x.GetAllPendingMessages()).Returns(_pendingMessages);
            _mockMethodExecutor.Setup(x =>
                x.GetAllPendingScreenshots()).Returns(_pendingScreenshots);
            _stepExecutionEndingProcessor = new StepExecutionEndingProcessor(_mockMethodExecutor.Object);
        }

        [Test]
        public void ShouldExtendFromHooksExecutionProcessor()
        {
            AssertEx.InheritsFrom<TaggedHooksFirstExecutionProcessor, StepExecutionEndingProcessor>();
        }

        [Test]
        public void ShouldReadPendingMessages()
        {
            var response = _stepExecutionEndingProcessor.Process(_stepExecutionEndingRequest);

            Assert.True(response != null);
            Assert.True(response.ExecutionResult != null);
            Assert.AreEqual(2, response.ExecutionResult.Message.Count);
            Assert.AreEqual(1, response.ExecutionResult.Screenshots.Count);

            foreach (var pendingMessage in _pendingMessages)
                Assert.Contains(pendingMessage, response.ExecutionResult.Message.ToList());
        }

        [Test]
        public void ShouldGetTagListFromScenarioAndSpec()
        {
            var specInfo = new SpecInfo
            {
                Tags = {"foo"},
                Name = "",
                FileName = "",
                IsFailed = false
            };
            var scenarioInfo = new ScenarioInfo
            {
                Tags = {"bar"},
                Name = "",
                IsFailed = false
            };
            var currentScenario = new ExecutionInfo
            {
                CurrentScenario = scenarioInfo,
                CurrentSpec = specInfo
            };

            var tags = AssertEx.ExecuteProtectedMethod<StepExecutionEndingProcessor>("GetApplicableTags", currentScenario)
                .ToList();
            Assert.IsNotEmpty(tags);
            Assert.AreEqual(2, tags.Count);
            Assert.Contains("foo", tags);
            Assert.Contains("bar", tags);
        }

        [Test]
        public void ShouldGetTagListFromScenarioAndSpecAndIgnoreDuplicates()
        {
            var specInfo = new SpecInfo
            {
                Tags = {"foo"},
                Name = "",
                FileName = "",
                IsFailed = false
            };
            var scenarioInfo = new ScenarioInfo
            {
                Tags = {"foo"},
                Name = "",
                IsFailed = false
            };
            var currentScenario = new ExecutionInfo
            {
                CurrentScenario = scenarioInfo,
                CurrentSpec = specInfo
            };
            var currentExecutionInfo = new StepExecutionEndingRequest
            {
                CurrentExecutionInfo = currentScenario
            };

            var tags = AssertEx.ExecuteProtectedMethod<StepExecutionEndingProcessor>("GetApplicableTags", currentScenario)
                .ToList();
            Assert.IsNotEmpty(tags);
            Assert.AreEqual(1, tags.Count);
            Assert.Contains("foo", tags);
        }
    }
}