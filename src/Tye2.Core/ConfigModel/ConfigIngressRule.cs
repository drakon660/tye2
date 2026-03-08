// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;

namespace Tye2.Core.ConfigModel
{
    public class ConfigIngressRule
    {
        public string? Path { get; set; }
        public string? Host { get; set; }
        public bool PreservePath { get; set; }

        [Required]
        public string? Service { get; set; }
    }
}
