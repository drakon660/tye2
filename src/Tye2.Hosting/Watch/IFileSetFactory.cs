// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Tye2.Hosting.Watch
{
    public interface IFileSetFactory
    {
        Task<IFileSet> CreateAsync(CancellationToken cancellationToken);
    }
}
