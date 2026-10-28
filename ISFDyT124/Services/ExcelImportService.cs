using ClosedXML.Excel;
using ISFDyT124.DTO;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ISFDyT124.Services
{
    public class FilaExcelValidada
    {
        public string Hoja { get; set; } = string.Empty;
        public int Fila { get; set; }
        public int Dni { get; set; }
        public string Apellido { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class ResultadoValidacionExcel
    {
        public bool EsValido => Errores.Count == 0;
        public List<FilaExcelValidada> FilasValidas { get; set; } = new List<FilaExcelValidada>();
        public List<CargaMasivaFilaErrorDto> Errores { get; set; } = new List<CargaMasivaFilaErrorDto>();
        public int TotalFilas { get; set; }
    }

    public class ExcelImportService
    {
        public const long MaximoBytes = 5 * 1024 * 1024;
        public const int MaximoFilas = 1000;
        private static readonly Regex SoloLetrasRegex = new Regex(@"^[\p{L}\p{M}\s'’-]+$", RegexOptions.Compiled);
        private static readonly EmailAddressAttribute EmailValidator = new EmailAddressAttribute();

        public static string GuardarArchivoTemporal(IFormFile archivo, string? propietario = null)
        {
            if (archivo.Length > MaximoBytes)
            {
                throw new InvalidDataException("El archivo supera el límite de 5 MB.");
            }

            LimpiarArchivosTemporalesVencidos();
            var token = Guid.NewGuid().ToString("N");
            var tempPath = ObtenerRutaArchivoTemporal(token);
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                archivo.CopyTo(stream);
            }
            if (!string.IsNullOrWhiteSpace(propietario)) File.WriteAllText(tempPath + ".owner", propietario);
            return token;
        }

        public static bool ArchivoPerteneceA(string token, string propietario)
        {
            if (string.IsNullOrWhiteSpace(propietario) || !Regex.IsMatch(token ?? string.Empty, "^[a-fA-F0-9]{32}$")) return false;
            try
            {
                var ownerPath = ObtenerRutaArchivoTemporal(token!) + ".owner";
                return File.Exists(ownerPath) && string.Equals(File.ReadAllText(ownerPath), propietario, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        private static void LimpiarArchivosTemporalesVencidos()
        {
            try
            {
                foreach (var ruta in Directory.EnumerateFiles(Path.GetTempPath(), "alumnos_import_*.xlsx"))
                {
                    if (File.GetLastWriteTimeUtc(ruta) < DateTime.UtcNow.AddHours(-1))
                    {
                        File.Delete(ruta);
                        if (File.Exists(ruta + ".owner")) File.Delete(ruta + ".owner");
                    }
                }
            }
            catch
            {
                // La limpieza no debe impedir una nueva importación.
            }
        }

        public static List<CargaMasivaHojaDto> ObtenerHojas(string rutaArchivo)
        {
            using var workbook = new XLWorkbook(rutaArchivo);
            return workbook.Worksheets.Select(worksheet =>
            {
                var first = worksheet.FirstRowUsed();
                var last = worksheet.LastRowUsed();
                var filas = first == null || last == null ? 0 : Math.Max(0, last.RowNumber() - first.RowNumber());
                return new CargaMasivaHojaDto
                {
                    Nombre = worksheet.Name,
                    TotalFilas = filas,
                    TieneDatos = filas > 0
                };
            }).ToList();
        }

        public static string ObtenerRutaArchivoTemporal(string token)
        {
            // Sanitizar token para evitar path traversal
            var safeToken = Regex.Replace(token ?? string.Empty, @"[^a-zA-Z0-9]", "");
            return Path.Combine(Path.GetTempPath(), $"alumnos_import_{safeToken}.xlsx");
        }

        public static void EliminarArchivoTemporal(string token)
        {
            try
            {
                var ruta = ObtenerRutaArchivoTemporal(token);
                if (File.Exists(ruta))
                {
                    File.Delete(ruta);
                }
                var ownerPath = ruta + ".owner";
                if (File.Exists(ownerPath)) File.Delete(ownerPath);
            }
            catch
            {
                // Ignorar error al limpiar archivo temporal
            }
        }

        /// <summary>Columnas del archivo que alimentan cada dato del alumno. Los números son de columna de Excel (1 = A); 0 = sin asignar.</summary>
        public class MapeoColumnas
        {
            public int Dni { get; set; }
            public int Apellido { get; set; }
            public int Nombre { get; set; }
            public int Email { get; set; }

            /// <summary>La columna de Apellido trae también el nombre ("APELLIDO NOMBRE" o "APELLIDO, NOMBRE") y se separa al leer.</summary>
            public bool ApellidoYNombre { get; set; }
        }

        public class MapeoHoja
        {
            public string Nombre { get; set; } = string.Empty;
            public int FilaCabecera { get; set; }
            public MapeoColumnas Columnas { get; set; } = new MapeoColumnas();
        }

        public class HojaLeida
        {
            public string Nombre { get; set; } = string.Empty;
            public int FilaCabecera { get; set; }
            public List<string> Columnas { get; set; } = new List<string>();
            public List<List<string>> PreviewRows { get; set; } = new List<List<string>>();
            public MapeoColumnas Sugerido { get; set; } = new MapeoColumnas();
        }

        private static readonly string[] DniKeywords = { "dni", "documento", "doc", "nro doc", "nº doc", "nro documento", "cedula" };
        private static readonly string[] ApellidoKeywords = { "apellido", "apellidos", "surname", "primer apellido" };
        private static readonly string[] NombreKeywords = { "nombre", "nombres", "first name", "primer nombre" };
        private static readonly string[] EmailKeywords = { "email", "mail", "correo", "correo electronico", "correo electrónico", "e-mail" };

        private const int FilasExploradasParaCabecera = 20;

        public static string LetraColumna(int numeroColumna) => XLHelper.GetColumnLetterFromNumber(numeroColumna);

        /// <summary>
        /// Lee las columnas y una muestra de filas de UNA hoja. Si no se indica la fila de cabecera se detecta sola:
        /// muchas planillas traen títulos arriba y la fila con "DNI", "Apellido", etc. aparece más abajo.
        /// </summary>
        public static HojaLeida LeerHoja(string rutaArchivo, string nombreHoja, int? filaCabecera = null, int previewMaxRows = 5)
        {
            using var workbook = new XLWorkbook(rutaArchivo);
            var worksheet = workbook.Worksheets.FirstOrDefault(w => string.Equals(w.Name, nombreHoja, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidDataException($"El archivo no tiene una pestaña llamada '{nombreHoja}'.");
            return LeerHoja(worksheet, filaCabecera, previewMaxRows);
        }

        private static HojaLeida LeerHoja(IXLWorksheet worksheet, int? filaCabecera, int previewMaxRows)
        {
            var resultado = new HojaLeida { Nombre = worksheet.Name };
            var primera = worksheet.FirstRowUsed();
            var ultima = worksheet.LastRowUsed();
            if (primera == null || ultima == null) return resultado;

            var filaCab = filaCabecera.HasValue && filaCabecera.Value >= 1 ? filaCabecera.Value : DetectarFilaCabecera(worksheet);
            resultado.FilaCabecera = filaCab;

            var ultimaColumna = worksheet.Row(filaCab).LastCellUsed()?.Address.ColumnNumber ?? 0;
            for (int col = 1; col <= ultimaColumna; col++)
            {
                resultado.Columnas.Add(worksheet.Cell(filaCab, col).GetString().Trim());
            }

            var ultimaFila = ultima.RowNumber();
            for (int fila = filaCab + 1; fila <= ultimaFila && resultado.PreviewRows.Count < previewMaxRows; fila++)
            {
                var valores = new List<string>();
                for (int col = 1; col <= ultimaColumna; col++) valores.Add(worksheet.Cell(fila, col).GetString().Trim());
                if (valores.Any(v => !string.IsNullOrWhiteSpace(v))) resultado.PreviewRows.Add(valores);
            }

            resultado.Sugerido = InferirMapeo(resultado.Columnas);
            return resultado;
        }

        /// <summary>
        /// Busca entre las primeras filas la que más se parece a una cabecera (la que nombra más datos: DNI, apellido,
        /// nombre, correo). Si ninguna nombra al menos dos, usa la primera fila con dos o más celdas llenas.
        /// </summary>
        public static int DetectarFilaCabecera(IXLWorksheet worksheet)
        {
            var primera = worksheet.FirstRowUsed();
            var ultima = worksheet.LastRowUsed();
            if (primera == null || ultima == null) return 1;

            var hasta = Math.Min(ultima.RowNumber(), primera.RowNumber() + FilasExploradasParaCabecera - 1);
            int mejorFila = 0, mejorPuntaje = 1, primeraConVarias = 0;

            for (int fila = primera.RowNumber(); fila <= hasta; fila++)
            {
                var textos = worksheet.Row(fila).CellsUsed()
                    .Select(c => NormalizarTexto(c.GetString()))
                    .Where(t => t.Length > 0)
                    .ToList();
                if (primeraConVarias == 0 && textos.Count >= 2) primeraConVarias = fila;

                int puntaje = new[] { DniKeywords, ApellidoKeywords, NombreKeywords, EmailKeywords }
                    .Count(grupo => textos.Any(t => grupo.Any(k => t.Contains(k))));
                if (puntaje > mejorPuntaje) { mejorPuntaje = puntaje; mejorFila = fila; }
            }

            if (mejorFila > 0) return mejorFila;
            return primeraConVarias > 0 ? primeraConVarias : primera.RowNumber();
        }

        /// <summary>Propone qué columna es cada dato a partir del texto de las cabeceras. Los números son de columna (1 = A).</summary>
        public static MapeoColumnas InferirMapeo(IReadOnlyList<string> columnas)
        {
            var mapeo = new MapeoColumnas();
            var normalizadas = columnas.Select(NormalizarTexto).ToList();

            int Buscar(string[] palabras, params int[] excluidas)
            {
                // primero coincidencia exacta (la columna se llama justo "DNI"), después parcial
                for (int i = 0; i < normalizadas.Count; i++)
                    if (!excluidas.Contains(i + 1) && palabras.Contains(normalizadas[i])) return i + 1;
                for (int i = 0; i < normalizadas.Count; i++)
                    if (!excluidas.Contains(i + 1) && palabras.Any(k => normalizadas[i].Contains(k))) return i + 1;
                return 0;
            }

            mapeo.Dni = Buscar(DniKeywords);
            mapeo.Email = Buscar(EmailKeywords, mapeo.Dni);

            // Una sola columna que dice "Apellido y Nombres": se separa al leer.
            var combinada = normalizadas.FindIndex(t => ApellidoKeywords.Any(k => t.Contains(k)) && NombreKeywords.Any(k => t.Contains(k))) + 1;
            var apellidoPuro = Buscar(ApellidoKeywords, combinada, mapeo.Dni, mapeo.Email);
            var nombrePuro = Buscar(NombreKeywords, combinada, apellidoPuro, mapeo.Dni, mapeo.Email);

            if (apellidoPuro > 0 && nombrePuro > 0)
            {
                mapeo.Apellido = apellidoPuro;
                mapeo.Nombre = nombrePuro;
            }
            else if (combinada > 0)
            {
                mapeo.Apellido = combinada;
                mapeo.ApellidoYNombre = true;
            }
            else
            {
                mapeo.Apellido = apellidoPuro;
                mapeo.Nombre = nombrePuro;
            }

            return mapeo;
        }

        /// <summary>Separa "APELLIDO, NOMBRE" por la coma, o "APELLIDO NOMBRE NOMBRE" tomando la primera palabra como apellido.</summary>
        public static (string Apellido, string Nombre) SepararApellidoYNombre(string texto)
        {
            var limpio = Regex.Replace(texto ?? string.Empty, @"\s+", " ").Trim();
            var coma = limpio.IndexOf(',');
            if (coma >= 0) return (limpio[..coma].Trim(), limpio[(coma + 1)..].Trim());

            var partes = limpio.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            return partes.Length == 2 ? (partes[0], partes[1]) : (limpio, string.Empty);
        }

        public static ResultadoValidacionExcel ValidarYLeerFilasDeHojas(string rutaArchivo, IReadOnlyList<MapeoHoja> mapeos)
        {
            var acumulado = new ResultadoValidacionExcel();
            if (mapeos.Count == 0)
            {
                acumulado.Errores.Add(new CargaMasivaFilaErrorDto { Fila = 1, Motivo = "Debe seleccionar al menos una pestaña válida del archivo." });
                return acumulado;
            }

            using var workbook = new XLWorkbook(rutaArchivo);
            foreach (var mapeo in mapeos)
            {
                var hoja = workbook.Worksheets.FirstOrDefault(w => string.Equals(w.Name, mapeo.Nombre, StringComparison.OrdinalIgnoreCase));
                if (hoja == null)
                {
                    acumulado.Errores.Add(new CargaMasivaFilaErrorDto { Hoja = mapeo.Nombre, Fila = 1, Motivo = "La pestaña no existe en el archivo." });
                    continue;
                }

                var parcial = ValidarYLeerFilas(hoja, mapeo);
                acumulado.FilasValidas.AddRange(parcial.FilasValidas);
                acumulado.Errores.AddRange(parcial.Errores);
                acumulado.TotalFilas += parcial.TotalFilas;
            }

            if (acumulado.TotalFilas > MaximoFilas)
            {
                acumulado.Errores.Add(new CargaMasivaFilaErrorDto
                {
                    Fila = 1,
                    Motivo = $"La importación contiene {acumulado.TotalFilas} filas y el máximo permitido es {MaximoFilas}."
                });
                acumulado.FilasValidas.Clear();
            }

            return acumulado;
        }

        private static string LeerDni(IXLCell celda)
        {
            var texto = celda.GetString().Trim();
            // Una columna convertida desde CSV o formulario puede traer "43866426.0": es el número, no un DNI con puntos.
            var entero = Regex.Match(texto, @"^(\d+)[.,]0+$");
            return entero.Success ? entero.Groups[1].Value : texto;
        }

        private static ResultadoValidacionExcel ValidarYLeerFilas(IXLWorksheet worksheet, MapeoHoja mapeo)
        {
            var resultado = new ResultadoValidacionExcel();
            var dnisVistosEnHoja = new HashSet<int>();
            var cols = mapeo.Columnas;
            var hoja = worksheet.Name;

            var ultimaFila = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            var filaCab = mapeo.FilaCabecera;

            if (ultimaFila == 0 || filaCab < 1 || filaCab > ultimaFila)
            {
                resultado.Errores.Add(new CargaMasivaFilaErrorDto { Hoja = hoja, Fila = 1, Motivo = "La fila de cabecera indicada no existe en la pestaña." });
                return resultado;
            }

            var ultimaColumna = worksheet.Row(filaCab).LastCellUsed()?.Address.ColumnNumber ?? 0;
            var usadas = new List<int> { cols.Dni, cols.Apellido, cols.Email };
            if (!cols.ApellidoYNombre) usadas.Add(cols.Nombre);
            if (usadas.Any(c => c < 1 || c > ultimaColumna) || usadas.Distinct().Count() != usadas.Count)
            {
                resultado.Errores.Add(new CargaMasivaFilaErrorDto
                {
                    Hoja = hoja,
                    Fila = filaCab,
                    Motivo = "Asigne una columna distinta de la pestaña a cada dato (DNI, apellido, nombre y correo)."
                });
                return resultado;
            }

            for (int rowNum = filaCab + 1; rowNum <= ultimaFila; rowNum++)
            {
                var rawDni = LeerDni(worksheet.Cell(rowNum, cols.Dni));
                var rawApellido = worksheet.Cell(rowNum, cols.Apellido).GetString().Trim();
                var rawNombre = cols.ApellidoYNombre ? string.Empty : worksheet.Cell(rowNum, cols.Nombre).GetString().Trim();
                var rawEmail = worksheet.Cell(rowNum, cols.Email).GetString().Trim();

                if (cols.ApellidoYNombre) (rawApellido, rawNombre) = SepararApellidoYNombre(rawApellido);

                // Ignorar filas completamente vacías
                if (string.IsNullOrWhiteSpace(rawDni) && string.IsNullOrWhiteSpace(rawApellido) &&
                    string.IsNullOrWhiteSpace(rawNombre) && string.IsNullOrWhiteSpace(rawEmail))
                {
                    continue;
                }

                resultado.TotalFilas++;
                var errores = new List<string>();

                int dniNumerico = 0;
                var cleanDni = rawDni.Replace(".", "").Replace("-", "").Replace(" ", "");
                if (string.IsNullOrWhiteSpace(cleanDni))
                    errores.Add("El DNI es obligatorio y está vacío.");
                else if (!int.TryParse(cleanDni, out dniNumerico))
                    errores.Add($"El DNI '{rawDni}' no es numérico.");
                else if (dniNumerico < 1000000 || dniNumerico > 99999999)
                    errores.Add($"El DNI '{dniNumerico}' está fuera del rango válido (7 a 8 dígitos).");
                else if (dnisVistosEnHoja.Contains(dniNumerico))
                    errores.Add($"El DNI '{dniNumerico}' está duplicado dentro de esta pestaña.");
                else
                    dnisVistosEnHoja.Add(dniNumerico);

                if (string.IsNullOrWhiteSpace(rawApellido)) errores.Add("El Apellido es obligatorio.");
                else if (rawApellido.Length > 100) errores.Add("El Apellido supera el límite de 100 caracteres.");
                else if (!SoloLetrasRegex.IsMatch(rawApellido)) errores.Add("El Apellido contiene caracteres inválidos.");

                if (string.IsNullOrWhiteSpace(rawNombre)) errores.Add("El Nombre es obligatorio.");
                else if (rawNombre.Length > 100) errores.Add("El Nombre supera el límite de 100 caracteres.");
                else if (!SoloLetrasRegex.IsMatch(rawNombre)) errores.Add("El Nombre contiene caracteres inválidos.");

                if (string.IsNullOrWhiteSpace(rawEmail)) errores.Add("El Email es obligatorio.");
                else if (!EmailValidator.IsValid(rawEmail)) errores.Add($"El Email '{rawEmail}' no tiene un formato válido.");
                else if (rawEmail.Length > 254) errores.Add("El Email supera el límite de 254 caracteres.");

                if (errores.Count > 0)
                {
                    resultado.Errores.Add(new CargaMasivaFilaErrorDto
                    {
                        Hoja = hoja,
                        Fila = rowNum,
                        Dni = rawDni,
                        Apellido = rawApellido,
                        Nombre = rawNombre,
                        Email = rawEmail,
                        Motivo = string.Join(" | ", errores)
                    });
                }
                else
                {
                    resultado.FilasValidas.Add(new FilaExcelValidada
                    {
                        Hoja = hoja,
                        Fila = rowNum,
                        Dni = dniNumerico,
                        Apellido = rawApellido,
                        Nombre = rawNombre,
                        Email = rawEmail
                    });
                }
            }

            return resultado;
        }

        private static string NormalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            var normalized = texto.ToLowerInvariant().Trim();
            normalized = normalized.Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u");
            return normalized;
        }
    }
}
