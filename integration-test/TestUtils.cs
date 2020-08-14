/*----------------------------------------------------------------
 *  Copyright (c) ThoughtWorks, Inc.
 *  Licensed under the Apache License, Version 2.0
 *  See LICENSE.txt in the project root for license information.
 *----------------------------------------------------------------*/


using System.IO;
using System.Linq;

namespace Gauge.Dotnet.IntegrationTests
{
    internal class TestUtils
    {
        public static string GetIntegrationTestSampleDirectory(string type="")
        {
            /* Rather than assuming what directory integration tests are executing from
             * We will discover 'up' the IntegrationTestSample project
            */
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            System.Console.WriteLine(dir.FullName);
            return FindIntegrationTestDirectory(dir, type).FullName;
        }

        private static DirectoryInfo FindIntegrationTestDirectory(DirectoryInfo dir, string type)
        {
            var testdataDir = dir.GetDirectories().FirstOrDefault(d => d.Name.Equals("_testdata"));
            if (testdataDir != null){
                return testdataDir.GetDirectories().First(d => d.Name.Equals($"Sample{type}"));
            }
            if (dir.Parent != null) // not on system boundry
                return FindIntegrationTestDirectory(dir.Parent, type);
            throw new DirectoryNotFoundException("Failed to find Sample directory");
        }
    }
}