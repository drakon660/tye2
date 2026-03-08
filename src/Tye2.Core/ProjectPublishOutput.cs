// See the LICENSE file in the project root for more information.

using System.IO;

namespace Tye2.Core
{
    public class ProjectPublishOutput : ServiceOutput
    {
        public ProjectPublishOutput(DirectoryInfo directory)
        {
            Directory = directory;
        }

        public DirectoryInfo Directory { get; }
    }
}
