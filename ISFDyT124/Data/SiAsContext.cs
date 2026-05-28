using Microsoft.EntityFrameworkCore;
using ISFDyT124.Models;

namespace ISFDyT124.Data
{
    public class SiAsContext : DbContext    //Define la clase SiAsContext que hereda de DbContext, lo que indica que esta clase se utilizará para interactuar con la base de datos.
    {
        public SiAsContext(DbContextOptions<SiAsContext> options) : base(options)   //Constructor que recibe opciones de configuración para la base de datos y las pasa a la clase base SiAsContext
        {
        }

        //DbSets que representan las tablas en la base de datos. Cada DbSet corresponde a una entidad o modelo en el sistema
        public DbSet<Asistencia> Asistencias { get; set; }
        public DbSet<Carrera> Carreras { get; set; }
        public DbSet<CarreraCohorte> CarreraCohortes { get; set; }
        public DbSet<CarreraMateria> CarreraMaterias { get; set; }
        public DbSet<Cohorte> Cohortes { get; set; }
        public DbSet <Login> Login { get; set; }
        public DbSet<Materia> Materias { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }


        protected override void OnModelCreating(ModelBuilder Builder)   
        {
            base.OnModelCreating(Builder);

            Builder.Entity<Usuario>()
                .HasOne(u => u.Rol)                 // Usuario tiene un Rol
                .WithMany(r => r.Usuarios)         // Un Rol tiene muchos Usuarios
                .HasForeignKey(u => u.RoId)       // La FK en Usuario es RolId
                .OnDelete(DeleteBehavior.Restrict); // Evita borrado en cascada por defecto (opcional)
        }
    }
}
