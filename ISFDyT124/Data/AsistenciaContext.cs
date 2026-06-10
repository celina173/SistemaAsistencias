using Microsoft.EntityFrameworkCore;
using ISFDyT124.Models;

namespace ISFDyT124.Data
{
    public class AsistenciaContext : DbContext
    {
        public AsistenciaContext(DbContextOptions<AsistenciaContext> options) : base(options)
        {
        }

        public DbSet<Carrera> Carreras { get; set; }
        public DbSet<Materia> Materias { get; set; }
        public DbSet<CarrerasMaterias> CarrerasMaterias { get; set; }
    }
}