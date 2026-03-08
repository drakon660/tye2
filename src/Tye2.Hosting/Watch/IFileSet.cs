// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Tye2.Hosting.Watch
{
    public interface IFileSet : IEnumerable<string>
    {
        bool Contains(string filePath);
    }
}
