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
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Gauge.CSharp.Lib;
using Gauge.CSharp.Lib.Attribute;

namespace IntegrationTestSample
{
    public class StepImplementation
    {
        [Step("A context step which gets executed before every scenario")]
        public void Context()
        {
            Console.WriteLine("This is a sample context");
        }

        [Step("Say <what> to <who>")]
        public void SaySomething(string what, string who)
        {
            Console.WriteLine("{0}, {1}!", what, who);
            GaugeMessages.WriteMessage("{0}, {1}!", what, who);
        }

        [Step("I throw an unserializable exception")]
        public void ThrowUnserializableException()
        {
            throw new CustomException("I am a custom exception");
        }

        [Step("I throw a serializable exception")]
        public void ThrowSerializableException()
        {
            GaugeScreenshots.RegisterCustomScreenshotWriter(new StringScreenshotWriter());
            throw new CustomSerializableException("I am a custom serializable exception");
        }

        [Step("I throw a serializable exception and continue")]
        [ContinueOnFailure]
        public void ContinueOnFailure()
        {
            GaugeScreenshots.RegisterCustomScreenshotWriter(new StringScreenshotWriter());
            throw new CustomSerializableException("I am a custom serializable exception");
        }

        [Step("I throw an AggregateException")]
        public void AsyncExeption()
        {
            var tasks = new[]
            {
                Task.Run(() => { throw new CustomSerializableException("First Exception"); }),
                Task.Run(() => { throw new CustomSerializableException("Second Exception"); })
            };
            Task.WaitAll(tasks);
        }

        [Step("Step with text", "and an alias")]
        public void StepWithAliases()
        {
        }

        [Step("Step that takes a table <table>")]
        public void ReadTable(Table table)
        {
            var columnNames = table.GetColumnNames();
            columnNames.ForEach(Console.Write);
            var rows = table.GetTableRows();
            rows.ForEach(
                row => Console.WriteLine(columnNames.Select(row.GetCell)
                    .Aggregate((a, b) => string.Format("{0}|{1}", a, b))));
        }

        [Step("Take Screenshot in reference Project")]
        public void TakeProjectReferenceScreenshot() {
            GaugeScreenshots.RegisterCustomScreenshotWriter(new ReferenceProject.ScreenshotWriter());
            GaugeScreenshots.Capture();
            GaugeScreenshots.RegisterCustomScreenshotWriter(new StringScreenshotWriter());
        }

        [Step("Take Screenshot in reference DLL")]
        public void TakeDllReferenceScreenshot() {
            GaugeScreenshots.RegisterCustomScreenshotWriter(new ReferenceDll.ScreenshotWriter());
            GaugeScreenshots.Capture();
            GaugeScreenshots.RegisterCustomScreenshotWriter(new StringScreenshotWriter());
        }

        [Serializable]
        public class CustomSerializableException : Exception
        {
            public CustomSerializableException(string s) : base(s)
            {
            }

            public CustomSerializableException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
        }

        public class CustomException : Exception
        {
            public CustomException(string message) : base(message)
            {
            }
        }

        public class StringScreenshotWriter : ICustomScreenshotWriter
        {
            public string TakeScreenShot()
            {
                return "screenshot.png";
            }
        }
    }
}