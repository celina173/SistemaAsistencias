using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISFDyT124.Models
{
    public class CarreraCohorte
    {
        [Key]
        public int CaCoId { get; set; }

        public int CaId { get; set; }
        public int CoId { get; set; }

        [ForeignKey("CaId")]
        public virtual Carrera Carrera { get; set; } = null!;

        [ForeignKey("CoId")]
        public virtual Cohorte Cohorte { get; set; } = null!;

        public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}
