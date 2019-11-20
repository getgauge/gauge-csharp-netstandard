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
using System.Diagnostics;
using System.Threading;
using Gauge.CSharp.Core;
using Gauge.Messages;

namespace Gauge.Dotnet.Processors
{
    public class ExecutionStartingProcessor : HookExecutionProcessor
    {
        public ExecutionStartingProcessor(IExecutionOrchestrator executionOrchestrator)
            : base(executionOrchestrator)
        {
        }

        protected override string HookType => "BeforeSuite";


        [DebuggerHidden]
        public virtual ExecutionStatusResponse Process(ExecutionStartingRequest request)
        {
            var debuggingEnv = Utils.TryReadEnvValue("DEBUGGING");
            if (debuggingEnv != null && debuggingEnv.ToLower().Equals("true"))
            {
                // if the runner is launched in DEBUG mode, let the debugger attach.
                Console.WriteLine("Runner Ready for Debugging at Process ID " +
                                  System.Diagnostics.Process.GetCurrentProcess().Id);
                var j = 0;
                while (!Debugger.IsAttached)
                {
                    j++;
                    //Trying to debug, wait for a debugger to attach
                    Thread.Sleep(100);
                    //Timeout, no debugger connected, break out into a normal execution.
                    if (j == 300)
                        break;
                }
            }
            return ExecuteHooks(request.CurrentExecutionInfo);

        }

    }
}