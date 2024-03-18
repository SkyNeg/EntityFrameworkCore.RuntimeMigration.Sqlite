using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyNeg.Sqlite.RuntimeMigration
{
    [Table("_ComponentVersion")]
    public class ComponentVersion
    {
        [Key]
        [MaxLength(100)]
        public string Component { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
    }
}
