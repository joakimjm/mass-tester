using System;
using System.Collections.Generic;
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
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            /*
            * Find all projects ending in .Tests
            */
            var collection = new DirectoryInfo(Environment.CurrentDirectory).EnumerateFiles("*Tests.csproj", SearchOption.AllDirectories);
            var xunitRunner = new FileInfo(args[0]);
            var type = args[1];
            var tasks = new List<Task>();

            if (!xunitRunner.Exists)
            {
                throw new FileNotFoundException("xUnit.net runner was not found at specified location: '{0}'", args[0]);
            }

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
                var assembly = project.Directory.EnumerateFiles(projectName + ".dll", SearchOption.AllDirectories).First();

                Console.WriteLine("Starting: {0}.", projectName);
                //continue;

                tasks.Add(Task.Run(async () =>
                {
                    var proc = new TestProcess(xunitRunner, assembly, type);
                    return Task.FromResult(0);

                    await proc.StartAsync();
                }));
            }

            await Task.WhenAll(tasks);

            if (Environment.UserInteractive)
            {
                Console.ReadKey();
            }
        }
    }
}
