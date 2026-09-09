using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ISFDyT124.DTO
{
    public class CargaMasivaPaso1Dto
    {
        [Required(ErrorMessage = "Debe seleccionar una Carrera / Cohorte.")]
        [Display(Name = "Carrera / Cohorte")]
        public int? CaCoId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar al menos una materia para inscribir a los alumnos.")]
        public List<int> SelectedCaMaIds { get; set; } = new List<int>();

        [Required(ErrorMessage = "Debe seleccionar un archivo Excel (.xlsx).")]
        public IFormFile? ArchivoExcel { get; set; }
    }

    public class CargaMasivaMapeoDto
    {
        [Required]
        public string TempFileToken { get; set; } = string.Empty;

        [Required]
        public int CaCoId { get; set; }

        public string CarreraCohorteDenominacion { get; set; } = string.Empty;

        public List<int> SelectedCaMaIds { get; set; } = new List<int>();

        public List<string> MateriasSeleccionadasNombres { get; set; } = new List<string>();

        public List<string> ColumnasExcel { get; set; } = new List<string>();

        [Required(ErrorMessage = "Debe mapear la columna correspondiente a DNI.")]
        [Display(Name = "Columna para DNI")]
        public string ColumnaDni { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe mapear la columna correspondiente a Apellido.")]
        [Display(Name = "Columna para Apellido")]
        public string ColumnaApellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe mapear la columna correspondiente a Nombre.")]
        [Display(Name = "Columna para Nombre")]
        public string ColumnaNombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe mapear la columna correspondiente a Email.")]
        [Display(Name = "Columna para Email")]
        public string ColumnaEmail { get; set; } = string.Empty;

        public List<Dictionary<string, string>> PreviewRows { get; set; } = new List<Dictionary<string, string>>();
    }

    public class CargaMasivaResultadoDto
    {
        public bool EsExitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string CarreraCohorteDenominacion { get; set; } = string.Empty;
        public int TotalFilas { get; set; }
        public int AlumnosNuevos { get; set; }
        public int AlumnosReutilizados { get; set; }
        public int InscripcionesCreadas { get; set; }
        public List<string> MateriasInscriptas { get; set; } = new List<string>();
        public List<CargaMasivaFilaErrorDto> Errores { get; set; } = new List<CargaMasivaFilaErrorDto>();
        public List<CargaMasivaAlumnoProcesadoDto> AlumnosProcesados { get; set; } = new List<CargaMasivaAlumnoProcesadoDto>();
    }

    public class CargaMasivaFilaErrorDto
    {
        public int Fila { get; set; }
        public string? Dni { get; set; }
        public string? Apellido { get; set; }
        public string? Nombre { get; set; }
        public string? Email { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }

    public class CargaMasivaAlumnoProcesadoDto
    {
        public int Fila { get; set; }
        public int Dni { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int MateriasInscriptas { get; set; }
    }
}
