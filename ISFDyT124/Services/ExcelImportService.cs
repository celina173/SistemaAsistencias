using ClosedXML.Excel;
using ISFDyT124.DTO;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ISFDyT124.Services
{
    public class FilaExcelValidada
    {
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
        private static readonly Regex SoloLetrasRegex = new Regex(@"^[A-Za-záéíóúÁÉÍÓÚüÜñÑ\s]+$", RegexOptions.Compiled);
        private static readonly EmailAddressAttribute EmailValidator = new EmailAddressAttribute();

        public static string GuardarArchivoTemporal(IFormFile archivo)
        {
            var token = Guid.NewGuid().ToString("N");
            var tempPath = ObtenerRutaArchivoTemporal(token);
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                archivo.CopyTo(stream);
            }
            return token;
        }

        public static string ObtenerRutaArchivoTemporal(string token)
        {
            // Sanitizar token para evitar path traversal
            var safeToken = Regex.Replace(token, @"[^a-zA-Z0-9]", "");
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
            }
            catch
            {
                // Ignorar error al limpiar archivo temporal
            }
        }

        public static (List<string> Columnas, List<Dictionary<string, string>> PreviewRows) LeerCabecerasYPreview(string rutaArchivo, int previewMaxRows = 5)
        {
            var columnas = new List<string>();
            var previewRows = new List<Dictionary<string, string>>();

            using (var workbook = new XLWorkbook(rutaArchivo))
            {
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    return (columnas, previewRows);
                }

                var firstRow = worksheet.FirstRowUsed();
                if (firstRow == null)
                {
                    return (columnas, previewRows);
                }

                var lastColumn = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
                for (int col = 1; col <= lastColumn; col++)
                {
                    var valor = worksheet.Cell(firstRow.RowNumber(), col).GetString().Trim();
                    columnas.Add(string.IsNullOrWhiteSpace(valor) ? $"Columna {col}" : valor);
                }

                var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? firstRow.RowNumber();
                int currentRow = firstRow.RowNumber() + 1;
                int rowsRead = 0;

                while (currentRow <= lastRowNumber && rowsRead < previewMaxRows)
                {
                    var rowData = new Dictionary<string, string>();
                    bool filaTieneDatos = false;

                    for (int col = 1; col <= columnas.Count; col++)
                    {
                        var valor = worksheet.Cell(currentRow, col).GetString().Trim();
                        rowData[columnas[col - 1]] = valor;
                        if (!string.IsNullOrWhiteSpace(valor))
                        {
                            filaTieneDatos = true;
                        }
                    }

                    if (filaTieneDatos)
                    {
                        previewRows.Add(rowData);
                        rowsRead++;
                    }
                    currentRow++;
                }
            }

            return (columnas, previewRows);
        }

        public static (string dni, string apellido, string nombre, string email) InferirMapeo(List<string> columnas)
        {
            string dni = "";
            string apellido = "";
            string nombre = "";
            string email = "";

            var dniKeywords = new[] { "dni", "documento", "doc", "nro doc", "nº doc", "nro documento", "cedula" };
            var apellidoKeywords = new[] { "apellido", "apellidos", "surname", "primer apellido" };
            var nombreKeywords = new[] { "nombre", "nombres", "first name", "primer nombre" };
            var emailKeywords = new[] { "email", "mail", "correo", "correo electronico", "correo electrónico", "e-mail" };

            foreach (var col in columnas)
            {
                var normalized = NormalizarTexto(col);

                if (string.IsNullOrEmpty(dni) && dniKeywords.Any(k => normalized.Contains(k)))
                {
                    dni = col;
                }
                else if (string.IsNullOrEmpty(apellido) && apellidoKeywords.Any(k => normalized.Contains(k)))
                {
                    apellido = col;
                }
                else if (string.IsNullOrEmpty(nombre) && nombreKeywords.Any(k => normalized.Contains(k)))
                {
                    nombre = col;
                }
                else if (string.IsNullOrEmpty(email) && emailKeywords.Any(k => normalized.Contains(k)))
                {
                    email = col;
                }
            }

            return (dni, apellido, nombre, email);
        }

        public static ResultadoValidacionExcel ValidarYLeerFilas(string rutaArchivo, string colDni, string colApellido, string colNombre, string colEmail)
        {
            var resultado = new ResultadoValidacionExcel();
            var dnisVistosEnArchivo = new HashSet<int>();

            using (var workbook = new XLWorkbook(rutaArchivo))
            {
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    resultado.Errores.Add(new CargaMasivaFilaErrorDto
                    {
                        Fila = 1,
                        Motivo = "El archivo Excel no contiene hojas de cálculo con datos."
                    });
                    return resultado;
                }

                var firstRow = worksheet.FirstRowUsed();
                if (firstRow == null)
                {
                    resultado.Errores.Add(new CargaMasivaFilaErrorDto
                    {
                        Fila = 1,
                        Motivo = "El archivo Excel está vacío."
                    });
                    return resultado;
                }

                // Obtener índice de columnas
                int colDniIdx = -1;
                int colApellidoIdx = -1;
                int colNombreIdx = -1;
                int colEmailIdx = -1;

                var lastColumn = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
                for (int col = 1; col <= lastColumn; col++)
                {
                    var header = worksheet.Cell(firstRow.RowNumber(), col).GetString().Trim();
                    if (header.Equals(colDni, StringComparison.OrdinalIgnoreCase)) colDniIdx = col;
                    if (header.Equals(colApellido, StringComparison.OrdinalIgnoreCase)) colApellidoIdx = col;
                    if (header.Equals(colNombre, StringComparison.OrdinalIgnoreCase)) colNombreIdx = col;
                    if (header.Equals(colEmail, StringComparison.OrdinalIgnoreCase)) colEmailIdx = col;
                }

                if (colDniIdx == -1 || colApellidoIdx == -1 || colNombreIdx == -1 || colEmailIdx == -1)
                {
                    resultado.Errores.Add(new CargaMasivaFilaErrorDto
                    {
                        Fila = 1,
                        Motivo = "No se pudieron localizar todas las columnas seleccionadas en la cabecera del archivo."
                    });
                    return resultado;
                }

                var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? firstRow.RowNumber();
                int totalProcesadas = 0;

                for (int rowNum = firstRow.RowNumber() + 1; rowNum <= lastRowNumber; rowNum++)
                {
                    var rawDni = worksheet.Cell(rowNum, colDniIdx).GetString().Trim();
                    var rawApellido = worksheet.Cell(rowNum, colApellidoIdx).GetString().Trim();
                    var rawNombre = worksheet.Cell(rowNum, colNombreIdx).GetString().Trim();
                    var rawEmail = worksheet.Cell(rowNum, colEmailIdx).GetString().Trim();

                    // Ignorar filas completamente vacías al final del archivo
                    if (string.IsNullOrWhiteSpace(rawDni) &&
                        string.IsNullOrWhiteSpace(rawApellido) &&
                        string.IsNullOrWhiteSpace(rawNombre) &&
                        string.IsNullOrWhiteSpace(rawEmail))
                    {
                        continue;
                    }

                    totalProcesadas++;
                    var listaErroresFila = new List<string>();

                    // 1. Validación de DNI
                    int dniNumerico = 0;
                    var cleanDni = rawDni.Replace(".", "").Replace("-", "").Replace(" ", "");
                    if (string.IsNullOrWhiteSpace(cleanDni))
                    {
                        listaErroresFila.Add("El DNI es obligatorio y está vacío.");
                    }
                    else if (!int.TryParse(cleanDni, out dniNumerico))
                    {
                        listaErroresFila.Add($"El DNI '{rawDni}' no es numérico.");
                    }
                    else if (dniNumerico < 6000000 || dniNumerico > 99999999)
                    {
                        listaErroresFila.Add($"El DNI '{dniNumerico}' está fuera del rango válido (7 a 8 dígitos).");
                    }
                    else if (dnisVistosEnArchivo.Contains(dniNumerico))
                    {
                        listaErroresFila.Add($"El DNI '{dniNumerico}' está duplicado dentro de este archivo.");
                    }
                    else
                    {
                        dnisVistosEnArchivo.Add(dniNumerico);
                    }

                    // 2. Validación de Apellido
                    if (string.IsNullOrWhiteSpace(rawApellido))
                    {
                        listaErroresFila.Add("El Apellido es obligatorio.");
                    }
                    else if (rawApellido.Length > 100)
                    {
                        listaErroresFila.Add("El Apellido supera el límite de 100 caracteres.");
                    }
                    else if (!SoloLetrasRegex.IsMatch(rawApellido))
                    {
                        listaErroresFila.Add("El Apellido contiene caracteres inválidos (solo letras y espacios permitidos).");
                    }

                    // 3. Validación de Nombre
                    if (string.IsNullOrWhiteSpace(rawNombre))
                    {
                        listaErroresFila.Add("El Nombre es obligatorio.");
                    }
                    else if (rawNombre.Length > 100)
                    {
                        listaErroresFila.Add("El Nombre supera el límite de 100 caracteres.");
                    }
                    else if (!SoloLetrasRegex.IsMatch(rawNombre))
                    {
                        listaErroresFila.Add("El Nombre contiene caracteres inválidos (solo letras y espacios permitidos).");
                    }

                    // 4. Validación de Email
                    if (string.IsNullOrWhiteSpace(rawEmail))
                    {
                        listaErroresFila.Add("El Email es obligatorio.");
                    }
                    else if (!EmailValidator.IsValid(rawEmail))
                    {
                        listaErroresFila.Add($"El Email '{rawEmail}' no tiene un formato válido.");
                    }

                    // Registro de errores o fila válida
                    if (listaErroresFila.Any())
                    {
                        resultado.Errores.Add(new CargaMasivaFilaErrorDto
                        {
                            Fila = rowNum,
                            Dni = rawDni,
                            Apellido = rawApellido,
                            Nombre = rawNombre,
                            Email = rawEmail,
                            Motivo = string.Join(" | ", listaErroresFila)
                        });
                    }
                    else
                    {
                        resultado.FilasValidas.Add(new FilaExcelValidada
                        {
                            Fila = rowNum,
                            Dni = dniNumerico,
                            Apellido = rawApellido,
                            Nombre = rawNombre,
                            Email = rawEmail
                        });
                    }
                }

                resultado.TotalFilas = totalProcesadas;
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
