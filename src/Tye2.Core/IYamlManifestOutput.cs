// See the LICENSE file in the project root for more information.

using YamlDotNet.RepresentationModel;

namespace Tye2.Core
{
    internal interface IYamlManifestOutput
    {
        YamlDocument Yaml { get; }
    }
}
