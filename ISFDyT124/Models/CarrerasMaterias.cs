using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISFDyT124.Models
{
    public class CarrerasMaterias
    {
        [Key]
        [Display(Name = "ID Relación")]
        public int id { get; set; }

        
        [Required(ErrorMessage = "Debe seleccionar una carrera.")]
        [ForeignKey("Carrera")]
        [Display(Name = "Carrera")]
        public int ca_id { get; set; }



    
        [Required(ErrorMessage = "Debe seleccionar una materia.")]
        [ForeignKey("Materia")]
        [Display(Name = "Materia")]
        public int ma_id { get; set; }


      
        public virtual Carrera? Carrera { get; set; }


        public virtual Materia? Materia { get; set; }
    }
}
