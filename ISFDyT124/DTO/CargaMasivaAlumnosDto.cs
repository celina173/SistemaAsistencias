using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ISFDyT124.DTO
{
    public class CargaMasivaPaso1Dto
    {
        [Required(ErrorMessage = "Debe seleccionar una Carrera / Cohorte.")]
        [Display(Name = "Carrera / Cohorte")]
        public int? CaCoId { get; set; }

        // Fecha desde la que se considera inscripto a cada alumno NUEVO. Si no se informa se usa hoy.
        // Las inscripciones que ya existen no se modifican.
        [Display(Name = "Fecha de inscripción")]
        [DataType(DataType.Date)]
        public DateTime? FechaInscripcion { get; set; }

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

        public DateTime? FechaInscripcion { get; set; }

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

        public List<CargaMasivaHojaDto> HojasExcel { get; set; } = new List<CargaMasivaHojaDto>();
        public List<string> HojasSeleccionadas { get; set; } = new List<string>();
    }

    public class CargaMasivaHojaDto
    {
        public string Nombre { get; set; } = string.Empty;
        public int TotalFilas { get; set; }
        public bool TieneDatos { get; set; }
    }

    public class CargaMasivaPrevisualizacionDto
    {
        [Required]
        public string TempFileToken { get; set; } = string.Empty;
        public List<string> HojasSeleccionadas { get; set; } = new List<string>();
        public List<CargaMasivaAlumnoEditableDto> Alumnos { get; set; } = new List<CargaMasivaAlumnoEditableDto>();
        public List<CargaMasivaCarreraOpcionDto> Carreras { get; set; } = new List<CargaMasivaCarreraOpcionDto>();
        public List<CargaMasivaMateriaOpcionDto> Materias { get; set; } = new List<CargaMasivaMateriaOpcionDto>();
        public List<CargaMasivaFilaErrorDto> Errores { get; set; } = new List<CargaMasivaFilaErrorDto>();
    }

    public class CargaMasivaAlumnoEditableDto
    {
        public bool Incluir { get; set; } = true;
        public string Hoja { get; set; } = string.Empty;
        public int Fila { get; set; }
        public string Dni { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int CaCoId { get; set; }
        public DateTime? FechaInscripcion { get; set; }
        public List<int> SelectedCaMaIds { get; set; } = new List<int>();
    }

    public class CargaMasivaCarreraOpcionDto
    {
        public int CaCoId { get; set; }
        public string Denominacion { get; set; } = string.Empty;
    }

    public class CargaMasivaMateriaOpcionDto
    {
        public int CaMaId { get; set; }
        public int? CaCoId { get; set; }
        public string Denominacion { get; set; } = string.Empty;
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
        public string? Hoja { get; set; }
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
