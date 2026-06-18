using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISFDyT124.Models
{
    public class CarreraMateria
    {
        [Key]
        [Display(Name = "ID Relación")]
        public int CaMaId { get; set; }
        

        [Display(Name = "Carrera")]
        [Required(ErrorMessage = "Debe seleccionar una carrera.")]
        [ForeignKey("Carrera")]
        public int CaId { get; set; }

        [Display(Name = "Materia")]
        [Required(ErrorMessage = "Debe seleccionar una materia.")]
        [ForeignKey("Materia")]
        public int MaId { get; set; }
        



      
        public virtual Carrera? Carrera { get; set; }


        public virtual Materia? Materia { get; set; }

        public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}
