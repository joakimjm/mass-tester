using System.Diagnostics;
using System.IO;
using System.Text;
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
                throw new System.IO.FileNotFoundException("File path to Command Line Administrative Interface Administration is invalid.");
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
                    ErrorDialog = false,
                    Arguments = string.Format("\"{0}\" -trait \"type={1}\"", assembly, type)
                }
            };

            System.Console.WriteLine("Arguments: " + string.Format("\"{0}\" -trait \"type={1}\"", assembly, type));
        }

        public async Task<string> StartAsync()
        {
            var result = new TaskCompletionSource<string>();

            var responseData = new StringBuilder();
            var error = new StringBuilder();

            _process.OutputDataReceived += (obj, e) =>
            {
                if (e.Data == null)
                {
                    _process.Dispose();
                    result.TrySetResult(responseData.ToString());
                    System.Console.WriteLine("{0} ended.", _assembly.Name);
                }
                else
                {
                    responseData.AppendLine(e.Data);
                }
            };

            _process.ErrorDataReceived += (obj, e) =>
            {
                System.Console.Error.WriteLine(e.Data);
                error.AppendLine(e.Data);
            };

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            return await result.Task;
        }
    }
}