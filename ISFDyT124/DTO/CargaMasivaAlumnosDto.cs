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

    public class ImportacionMasivaEstado
    {
        public Guid ImportId { get; set; }
        public string UsuarioId { get; set; } = string.Empty;
        public DateTime CreadoUtc { get; set; }
        public DateTime UltimoAccesoUtc { get; set; }
        public int CaCoId { get; set; }
        public List<int> SelectedCaMaIds { get; set; } = new();
        public string CarreraCohorteDenominacion { get; set; } = string.Empty;
        public List<string> MateriasSeleccionadasNombres { get; set; } = new();
        public List<string> ColumnasExcel { get; set; } = new();
        public List<Dictionary<string, string>> PreviewRows { get; set; } = new();
        public string ColumnaDni { get; set; } = string.Empty;
        public string ColumnaApellido { get; set; } = string.Empty;
        public string ColumnaNombre { get; set; } = string.Empty;
        public string ColumnaEmail { get; set; } = string.Empty;
    }

    public class CargaMasivaMapeoDto
    {
        public Guid ImportId { get; set; }

        [Required]
        public string TempFileToken { get; set; } = string.Empty;

        [Required]
        public int CaCoId { get; set; }

        public DateTime? FechaInscripcion { get; set; }

        public string CarreraCohorteDenominacion { get; set; } = string.Empty;

        public List<int> SelectedCaMaIds { get; set; } = new List<int>();

        public List<string> MateriasSeleccionadasNombres { get; set; } = new List<string>();

        // Cada pestaña tiene su propia fila de cabecera y su propia asignación de columnas: las pestañas de un
        // mismo archivo casi nunca comparten el mismo orden ni los mismos títulos.
        public List<CargaMasivaHojaDto> Hojas { get; set; } = new List<CargaMasivaHojaDto>();
    }

    public class CargaMasivaHojaDto
    {
        public string Nombre { get; set; } = string.Empty;
        public int TotalFilas { get; set; }
        public bool TieneDatos { get; set; }

        /// <summary>La pestaña se carga (marcada en el paso de mapeo).</summary>
        public bool Incluir { get; set; }

        /// <summary>Fila de Excel donde están los títulos de las columnas.</summary>
        public int FilaCabecera { get; set; }

        // Números de columna de Excel (1 = A); 0 = sin asignar.
        public int ColDni { get; set; }
        public int ColApellido { get; set; }
        public int ColNombre { get; set; }
        public int ColEmail { get; set; }

        /// <summary>La columna de Apellido trae también el nombre y se separa al leer.</summary>
        public bool ApellidoYNombre { get; set; }

        // Sólo para mostrar; se vuelven a leer del archivo y no se reciben del formulario.
        public List<string> Columnas { get; set; } = new List<string>();
        public List<List<string>> PreviewRows { get; set; } = new List<List<string>>();
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
        public Guid? ImportId { get; set; }
        public bool EsConfirmacion { get; set; }
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
