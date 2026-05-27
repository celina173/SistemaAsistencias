using System.ComponentModel.DataAnnotations;

namespace ISFDyT124.Models
{
    public class Materia
    {
        [Key]
        public int MaId { get; set; }

        [Required]
        [StringLength(100)]
        public string MaDenominacion { get; set; }

        [StringLength(50)]
        public string? MaModalidad { get; set; }

        public int? MaCantModulos { get; set; }

        public ICollection<CarrerasMaterias>? CarrerasMaterias { get; set; }
    }
}