using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MassTester.Runner
{
    internal class TestProcess
    {
        private Process _process;
        private FileInfo _assembly;

        public TestProcess(FileInfo xunitRunner, FileInfo assembly, string type)
        {
            if (!xunitRunner.Exists)
            {
                throw new FileNotFoundException("File path to Command Line Administrative Interface Administration is invalid.");
            }

            _assembly = assembly;

            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    FileName = xunitRunner.FullName,
                    WorkingDirectory = assembly.Directory.FullName,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    ErrorDialog = false, //TODO: Write results xUnit 1.x XML for Jenkins' test report in solution folder /TestResults
                    Arguments = string.Format("\"{0}\" -trait \"type={1}\"", assembly, type)
                }
            };
        }

        public async Task<int> StartAsync()
        {
            var result = new TaskCompletionSource<int>();

            _process.OutputDataReceived += (obj, e) =>
            {
                if (e.Data == null)
                {
                    if (_process.ExitCode != 0)
                    {
                        Environment.ExitCode = _process.ExitCode;

                        Console.Error.WriteLine("{0} failed.", _assembly.Name);
                    }
                    else
                    {
                        Console.WriteLine("{0} completed.", _assembly.Name);
                    }

                    result.TrySetResult(_process.ExitCode);
                    _process.Dispose();
                }
                else
                {
                    Console.WriteLine(e.Data);
                }
            };

            _process.ErrorDataReceived += (obj, e) =>
            {
                Console.Error.WriteLine(e.Data);
            };

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            return await result.Task;
        }
    }
}