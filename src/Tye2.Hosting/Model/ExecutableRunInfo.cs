// See the LICENSE file in the project root for more information.

namespace Tye2.Hosting.Model
{
    public class ExecutableRunInfo : RunInfo
    {
        public ExecutableRunInfo(string executable, string? workingDirectory, string? args)
        {
            Executable = executable;
            WorkingDirectory = workingDirectory;
            Args = args;
        }

        public string Executable { get; }

        public string? WorkingDirectory { get; }

        public string? Args { get; set; }
    }
}
