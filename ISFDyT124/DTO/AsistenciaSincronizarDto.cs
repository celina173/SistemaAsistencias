namespace ISFDyT124.DTO
{
    // Una fila de la cola offline que el navegador manda a sincronizar. A diferencia
    // de AsistenciaCrearDto (que viene de un submit de formulario), esta llega como
    // JSON desde el JS de la cola, y trae un GUID generado en el cliente para que el
    // servidor pueda detectar reintentos duplicados de sincronización.
    public class AsistenciaSincronizarDto
    {
        public Guid ClientGuid { get; set; }
        public int? UsId { get; set; }
        public int MaId { get; set; }
        public DateTime Fecha { get; set; }

        // Momento real en que el docente lo cargó en el dispositivo, offline.
        public DateTime FechaCarga { get; set; }
        public bool Presente { get; set; }
        public bool Justificacion { get; set; }
    }
}
