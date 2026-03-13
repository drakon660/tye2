// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Tye2
{
    internal class TempFile : IDisposable
    {
        public static TempFile Create()
        {
            return new TempFile(Path.GetTempFileName());
        }

        public void Dispose()
        {
            File.Delete(FilePath);
        }

        public TempFile(string filePath)
        {
            FilePath = filePath;
        }

        public string FilePath { get; }

    }
}
