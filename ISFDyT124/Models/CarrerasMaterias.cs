using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISFDyT124.Models
{
    public class CarrerasMaterias
    {
        [Key]
        public int CaMaId { get; set; }

        [ForeignKey("Carrera")]
        public int CaId { get; set; }

        [ForeignKey("Materia")]
        public int MaId { get; set; }

        public Carrera? Carrera { get; set; }

        public Materia? Materia { get; set; }
    }
}
