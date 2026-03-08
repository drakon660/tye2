// See the LICENSE file in the project root for more information.

namespace Tye2.Hosting.Watch.Internal
{
    public static class FileWatcherFactory
    {
        public static IFileSystemWatcher CreateWatcher(string watchedDirectory)
              => new DotnetFileWatcher(watchedDirectory);
    }
}
