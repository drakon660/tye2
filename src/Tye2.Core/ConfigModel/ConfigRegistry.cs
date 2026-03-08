// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;

namespace Tye2.Core.ConfigModel
{
    public class ConfigRegistry
    {
        [Required]
        public string Hostname { get; set; } = null!;

        public string? PullSecret { get; set; }
    }
}
