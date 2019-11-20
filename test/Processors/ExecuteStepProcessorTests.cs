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

using Gauge.Dotnet.Models;
using Gauge.Dotnet.Processors;
using Gauge.Messages;
using Moq;
using NUnit.Framework;

namespace Gauge.Dotnet.UnitTests.Processors
{
    [TestFixture]
    public class ExecuteStepProcessorTests
    {
        public void Foo(string param)
        {
        }

        [Test]
        public void ShouldProcessExecuteStepRequest()
        {
            const string parsedStepText = "Foo";
            var request = new ExecuteStepRequest
            {
                ActualStepText = parsedStepText,
                ParsedStepText = parsedStepText,
                Parameters =
                    {
                        new Parameter
                        {
                            ParameterType = Parameter.Types.ParameterType.Static,
                            Name = "Foo",
                            Value = "Bar"
                        }
                    }
            };
            var mockStepRegistry = new Mock<IStepRegistry>();
            mockStepRegistry.Setup(x => x.ContainsStep(parsedStepText)).Returns(true);
            var fooMethodInfo = new GaugeMethod { Name = "Foo", ParameterCount = 1 };
            mockStepRegistry.Setup(x => x.MethodFor(parsedStepText)).Returns(fooMethodInfo);
            var mockOrchestrator = new Mock<IExecutionOrchestrator>();
            mockOrchestrator.Setup(e => e.ExecuteStep(fooMethodInfo, It.IsAny<string[]>()))
                .Returns(() => new ProtoExecutionResult { ExecutionTime = 1, Failed = false });

            var mockTableFormatter = new Mock<ITableFormatter>();

            var processor = new ExecuteStepProcessor(mockStepRegistry.Object, mockOrchestrator.Object, mockTableFormatter.Object);
            var response = processor.Process(request);

            Assert.False(response.ExecutionResult.Failed);
        }

        [Test]
        [TestCase(Parameter.Types.ParameterType.Table)]
        [TestCase(Parameter.Types.ParameterType.SpecialTable)]
        public void ShouldProcessExecuteStepRequestForTableParam(Parameter.Types.ParameterType parameterType)
        {
            const string parsedStepText = "Foo";
            var protoTable = new ProtoTable();
            var tableJSON = "{'headers':['foo', 'bar'],'rows':[['foorow1','barrow1']]}";
            var request = new ExecuteStepRequest
            {
                ActualStepText = parsedStepText,
                ParsedStepText = parsedStepText,
                Parameters =
                    {
                        new Parameter
                        {
                            ParameterType = parameterType,
                            Table = protoTable
                        }
                    }
            };

            var mockStepRegistry = new Mock<IStepRegistry>();
            mockStepRegistry.Setup(x => x.ContainsStep(parsedStepText)).Returns(true);
            var fooMethodInfo = new GaugeMethod { Name = "Foo", ParameterCount = 1 };
            mockStepRegistry.Setup(x => x.MethodFor(parsedStepText)).Returns(fooMethodInfo);
            var mockOrchestrator = new Mock<IExecutionOrchestrator>();
            mockOrchestrator.Setup(e => e.ExecuteStep(fooMethodInfo, It.IsAny<string[]>())).Returns(() =>
                new ProtoExecutionResult
                {
                    ExecutionTime = 1,
                    Failed = false
                });

            var mockAssemblyLoader = new Mock<IAssemblyLoader>();
            mockAssemblyLoader.Setup(x => x.GetLibType(LibType.MessageCollector));
            var mockTableFormatter = new Mock<ITableFormatter>();
            mockTableFormatter.Setup(x => x.GetJSON(protoTable))
                .Returns(tableJSON);
            var processor = new ExecuteStepProcessor(mockStepRegistry.Object, mockOrchestrator.Object, mockTableFormatter.Object);
            var response = processor.Process(request);

            mockOrchestrator.Verify(executor =>
                executor.ExecuteStep(fooMethodInfo, It.Is<string[]>(strings => strings[0] == tableJSON)));
            Assert.False(response.ExecutionResult.Failed);
        }

        [Test]
        public void ShouldReportArgumentMismatch()
        {
            const string parsedStepText = "Foo";
            var request = new ExecuteStepRequest
            {
                ActualStepText = parsedStepText,
                ParsedStepText = parsedStepText
            };
            var mockStepRegistry = new Mock<IStepRegistry>();
            mockStepRegistry.Setup(x => x.ContainsStep(parsedStepText)).Returns(true);
            var fooMethod = new GaugeMethod { Name = "Foo", ParameterCount = 1 };
            mockStepRegistry.Setup(x => x.MethodFor(parsedStepText)).Returns(fooMethod);
            var mockOrchestrator = new Mock<IExecutionOrchestrator>();

            var mockTableFormatter = new Mock<ITableFormatter>();

            var processor = new ExecuteStepProcessor(mockStepRegistry.Object, mockOrchestrator.Object, mockTableFormatter.Object);
            var response = processor.Process(request);

            Assert.True(response.ExecutionResult.Failed);
            Assert.AreEqual(response.ExecutionResult.ErrorMessage,
                "Argument length mismatch for Foo. Actual Count: 0, Expected Count: 1");
        }

        [Test]
        public void ShouldReportMissingStep()
        {
            const string parsedStepText = "Foo";
            var request = new ExecuteStepRequest
            {
                ActualStepText = parsedStepText,
                ParsedStepText = parsedStepText
            };
            var mockStepRegistry = new Mock<IStepRegistry>();
            mockStepRegistry.Setup(x => x.ContainsStep(parsedStepText)).Returns(false);
            var mockOrchestrator = new Mock<IExecutionOrchestrator>();
            var mockTableFormatter = new Mock<ITableFormatter>();

            var processor = new ExecuteStepProcessor(mockStepRegistry.Object, mockOrchestrator.Object, mockTableFormatter.Object);
            var response = processor.Process(request);

            Assert.True(response.ExecutionResult.Failed);
            Assert.AreEqual(response.ExecutionResult.ErrorMessage,
                "Step Implementation not found");
        }
    }
}