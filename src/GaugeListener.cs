// Copyright 2018 ThoughtWorks, Inc.
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
using Gauge.Dotnet.Models;
using Gauge.Messages;
using Grpc.Core;

namespace Gauge.Dotnet
{
    public class GaugeListener : IGaugeListener
    {
        private readonly IStaticLoader _loader;

        public GaugeListener(IStaticLoader loader)
        {
            this._loader = loader;
        }

        public void StartServer()
        {
            var server = new Server();
            RunnerServiceHandler serviceImpl = new RunnerServiceHandler(server, _loader);
            server.Services.Add(Runner.BindService(serviceImpl));
            var port = server.Ports.Add(new ServerPort("127.0.0.1", 0, ServerCredentials.Insecure));
            server.Start();
            Console.WriteLine("Listening on port:" + port);
            server.ShutdownTask.Wait();
            Environment.Exit(Environment.ExitCode);
        }
    }
}