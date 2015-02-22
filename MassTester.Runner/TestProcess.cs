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

        /// <summary>
        /// Kill the process if it hasn't already exited.
        /// </summary>
        public void Kill()
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill();
                }
            }
            catch { /*Getting here is most likely due to the object already being disposed, which is nice at this point.*/ }
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

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine("{0}: Failed with code {1}.", _assembly.Name, _process.ExitCode);
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine("{0}: Completed.", _assembly.Name);
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }

                    result.TrySetResult(_process.ExitCode);
                    _process.Dispose();
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        Console.WriteLine("{0}: {1}", _assembly.Name, e.Data);
                    }
                }
            };

            _process.ErrorDataReceived += (obj, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Error.WriteLine("{0}: {1}", _assembly.Name, e.Data);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            };

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            return await result.Task;
        }
    }
}