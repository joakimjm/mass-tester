using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MassTester.Runner
{
    internal class Program
    {
        #region Capture console event handler

        //As per http://stackoverflow.com/questions/474679/capture-console-exit-c-sharp
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);

        private static EventHandler _handler;

        #endregion Capture console event handler

        private enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static void Main(string[] args)
        {
            MainAsync(new Arguments(args)).Wait();
        }

        private static async Task MainAsync(Arguments args)
        {
            var config = new AppConfig();

            /*
            * Find all projects ending in .Tests
            */
            var solutionDirectory = Environment.CurrentDirectory;
            var collection = new DirectoryInfo(solutionDirectory).EnumerateFiles("*Tests.csproj", SearchOption.AllDirectories);
            var xunitRunner = GetXUnitRunnerFile(args["runner"]);
            var type = args["type"];
            var configuration = args["configuration"] ?? "Release";
            var tasks = new List<Task>();

            #region Output header

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Mass Tester started", xunitRunner.FullName);
            Console.WriteLine("  Working dir   : {0}", solutionDirectory);
            Console.WriteLine("  Test runner   : {0}", xunitRunner.FullName);
            Console.WriteLine("  Test Type     : {0}", type);
            Console.WriteLine("  Output format : {0}", config.GetOutputFormat());
            Console.WriteLine("  Configuration : {0}", configuration);
            Console.WriteLine();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;

            #endregion Output header


            var outputDir = Path.Combine(solutionDirectory, "TestResults");

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            foreach (var project in collection)
            {
                //Poor man's assembly name
                var projectName = project.Name.Substring(0, project.Name.IndexOf(project.Extension));

                /*
                 * project -> bin\{Configuration}\{AssemblyName}.dll
                 * TODO: Read AssemblyName through the project's xml.
                 */
                var assembly = project.Directory
                    .EnumerateFiles(projectName + ".dll", SearchOption.AllDirectories)
                    .Where(x => x.Directory.Name == configuration)
                    .First();

                tasks.Add(Task.Run(async () =>
                {
                    //{SolutionDir}\TestResults\{ProjectName}.xml
                    var resultPath = Path.Combine(outputDir, projectName + ".xml");

                    var proc = new RunnerProcess(xunitRunner, assembly, config.GetOutputFormat(), resultPath, type);

                    /*
                     * Hook onto the captured console event handler,
                     * in order to close running processes in case
                     * Mass Tester exits unexpectedly.
                     */
                    _handler += new EventHandler((sig) =>
                    {
                        switch (sig)
                        {
                            case CtrlType.CTRL_C_EVENT:
                            //case CtrlType.CTRL_LOGOFF_EVENT:
                            //case CtrlType.CTRL_SHUTDOWN_EVENT:
                            case CtrlType.CTRL_CLOSE_EVENT:
                            default:
                                proc.Kill();
                                return false;
                        }
                    });
                    SetConsoleCtrlHandler(_handler, true);

                    //Start the process.
                    await proc.StartAsync();
                }));
            }

            await Task.WhenAll(tasks);

            if (Environment.ExitCode > 0)
            {
                Console.WriteLine("Some tests failed.");
            }


        }

        /// <summary>
        /// Locate the xUnit.net console runner.
        /// </summary>
        /// <param name="specifiedPath">A path specified through an argument.</param>
        /// <returns></returns>
        private static FileInfo GetXUnitRunnerFile(string specifiedPath)
        {
            var result = default(FileInfo);

            if (!string.IsNullOrWhiteSpace(specifiedPath))
            {
                result = new FileInfo(specifiedPath);
            }

            if ((result == null || !result.Exists) && Directory.Exists("packages"))
            {
                //Try to look for it inside the NuGet packages.
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

            /*
             * Attempt to verify that the file is actually an xUnit.net runner
             * by looking at the file's meta data.
             */
            var info = FileVersionInfo.GetVersionInfo(result.FullName);
            if (info.FileDescription.ToLowerInvariant() != "xunit.net console test runner")
            {
                throw new ArgumentException("Specified file was not xunit runner");
            }

            return result;
        }
    }
}