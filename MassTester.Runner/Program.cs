using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MassTester.Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(new Arguments(args)).Wait();
        }

        static async Task MainAsync(Arguments args)
        {
            /*
            * Find all projects ending in .Tests
            */
            var solutionDirectory = Environment.CurrentDirectory;
            var collection = new DirectoryInfo(solutionDirectory).EnumerateFiles("*Tests.csproj", SearchOption.AllDirectories);
            var xunitRunner = GetXUnitRunnerFile(args["runner"]);
            var type = args["type"] ?? "unit";
            var configuration = args["configuration"] ?? "Release";
            var tasks = new List<Task>();

            #region Output header
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Mass Tester started", xunitRunner.FullName);
            Console.WriteLine("  Working dir   : {0}", solutionDirectory);
            Console.WriteLine("  Test runner   : {0}", xunitRunner.FullName);
            Console.WriteLine("  Type          : {0}", type);
            Console.WriteLine("  Configuration : {0}", configuration);
            Console.WriteLine();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            #endregion

            foreach (var project in collection)
            {
                //Poor man's assembly name
                var projectName = project.Name.Substring(0, project.Name.IndexOf(project.Extension));

                /*
                 * project -> bin\{Configuration}\{AssemblyName}.dll
                 * 
                 * Configuration should be passed as argument. Default can be Debug, or whatever.
                 * AssemblyName can be read through the project xml.
                 */
                var assembly = project.Directory
                    .EnumerateFiles(projectName + ".dll", SearchOption.AllDirectories)
                    .Where(x => x.Directory.Name == configuration)
                    .First();

                //Console.WriteLine("Starting: {0}.", projectName);
                //continue;

                tasks.Add(Task.Run(async () =>
                {
                    var proc = new TestProcess(xunitRunner, assembly, type);
                    await proc.StartAsync();
                }));
            }

            await Task.WhenAll(tasks);

            Console.WriteLine("Testing finished with code {0}.", Environment.ExitCode);

            //if (Environment.UserInteractive)
            //{
            //    Console.ReadKey();
            //}
        }

        private static FileInfo GetXUnitRunnerFile(string specifiedPath)
        {
            var result = default(FileInfo);

            if (!string.IsNullOrWhiteSpace(specifiedPath))
            {
                result = new FileInfo(specifiedPath);
            }

            if ((result == null || !result.Exists) && Directory.Exists("packages"))
            {
                result = new DirectoryInfo("packages")
                    .EnumerateFiles("xunit.console.exe", SearchOption.AllDirectories)
                    .FirstOrDefault();
            }

            if (result == null || !result.Exists)
            {
                if (!string.IsNullOrWhiteSpace(specifiedPath))
                {
                    throw new FileNotFoundException("xUnit.net runner was not located at specified path: '{0}'", specifiedPath);
                }

                throw new FileNotFoundException("xUnit.net runner couldn't be located. Please specify a path to the xUnit.net runner executable.");
            }

            var info = FileVersionInfo.GetVersionInfo(result.FullName);
            if (info.FileDescription.ToLowerInvariant() != "xunit.net console test runner")
            {
                throw new ArgumentException("Specified file was not xunit runner");
            }

            return result;
        }
    }
}
