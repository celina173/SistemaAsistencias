using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISFDyT124.Models
{
    public class CarrerasMaterias
    {
        [Key]
<<<<<<< HEAD
        public int CaMaId { get; set; }
=======
        [Display(Name = "ID Relación")]
        public int id { get; set; }
>>>>>>> 3c6cfa8ae4530af7b793ddf2710c4a04f9f0f2d7

        
        [Required(ErrorMessage = "Debe seleccionar una carrera.")]
        [ForeignKey("Carrera")]
<<<<<<< HEAD
        public int CaId { get; set; }
=======
        [Display(Name = "Carrera")]
        public int ca_id { get; set; }
>>>>>>> 3c6cfa8ae4530af7b793ddf2710c4a04f9f0f2d7



    
        [Required(ErrorMessage = "Debe seleccionar una materia.")]
        [ForeignKey("Materia")]
<<<<<<< HEAD
        public int MaId { get; set; }
=======
        [Display(Name = "Materia")]
        public int ma_id { get; set; }
>>>>>>> 3c6cfa8ae4530af7b793ddf2710c4a04f9f0f2d7


      
        public virtual Carrera? Carrera { get; set; }


        public virtual Materia? Materia { get; set; }
    }
}
