// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;

namespace Tye2.Core.ConfigModel
{
    public class BuildProperty
    {
        [Required]
        public string Name { get; set; } = default!;

        [Required]
        public string Value { get; set; } = default!;
    }
}
