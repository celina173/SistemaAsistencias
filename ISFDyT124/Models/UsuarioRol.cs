using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISFDyT124.Models
{
    public class UsuarioRol
    {
        [Key]
        public int UsRoId { get; set; }

        public int UsId { get; set; }
        public int RoId { get; set; }

        [ForeignKey("UsId")]
        public virtual Usuario Usuario { get; set; } = null!;

        [ForeignKey("RoId")]
        public virtual Rol Rol { get; set; } = null!;
    }
}
