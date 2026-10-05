using Microsoft.EntityFrameworkCore;
using ISFDyT124.Models;

namespace ISFDyT124.Data
{
    public class InstitutoDbContext : DbContext
    {
        public InstitutoDbContext(DbContextOptions<InstitutoDbContext> options)
            : base(options) { }

        // Definición de DbSets para cada una de las tablas del SQL
        public DbSet<Rol> Roles { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;

        //public DbSet<Login> Logins { get; set; } = null!;
        public DbSet<Materia> Materias { get; set; } = null!;
        public DbSet<Carrera> Carreras { get; set; } = null!;
        public DbSet<Cohorte> Cohortes { get; set; } = null!;
        public DbSet<Asistencia> Asistencias { get; set; } = null!;
        public DbSet<CarreraCohorte> CarreraCohortes { get; set; } = null!;
        public DbSet<CarreraMateria> CarreraMaterias { get; set; } = null!;
        public DbSet<Inscripciones> Inscripciones { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Rol>().Property(r => r.RoId).ValueGeneratedNever();
            modelBuilder.Entity<Usuario>().Property(u => u.UsId).ValueGeneratedNever();
            //modelBuilder.Entity<Login>().Property(l => l.LoId).ValueGeneratedNever();
            // Materia, Carrera, Asistencia y CarreraMateria pasaron a IDENTITY (columna
            // autoincremental en SQL Server). CarreraCohorte se suma ahora a esa misma
            // conversión: quedaba como la única de esa familia con PK manual, lo que
            // obligaba a calcular el próximo ID a mano en cualquier alta (causa real de
            // un bug encontrado en el backfill de CarreraMateria.CaCoId) y va a hacer
            // falta de nuevo apenas exista el alta real de CarreraCohorte (ticket 4.12).
            //modelBuilder.Entity<Cohorte>().Property(co => co.CoId).ValueGeneratedNever();
            modelBuilder.Entity<CarreraMateria>().ToTable("CarreraMateria");
            modelBuilder.Entity<CarreraMateria>().Property(cm => cm.CaMaId).ValueGeneratedOnAdd();
            modelBuilder.Entity<CarreraCohorte>().Property(cc => cc.CaCoId).ValueGeneratedOnAdd();
            modelBuilder.Entity<Cohorte>().Property(co => co.CoId).ValueGeneratedNever();

            // Configurar DNI único de la tabla USUARIOS
            modelBuilder.Entity<Usuario>().HasIndex(u => u.UsDni).IsUnique();

            // Añadir índice único para el email de usuario
            modelBuilder.Entity<Usuario>().HasIndex(u => u.UsEmail).IsUnique();

            modelBuilder.Entity<CarreraMateria>()
                .HasIndex(cm => new { cm.CaCoId, cm.MaId })
                .IsUnique()
                .HasFilter("[CaCoId] IS NOT NULL");
            modelBuilder.Entity<Inscripciones>().HasIndex(i => new { i.UsId, i.CaMaId }).IsUnique();
            modelBuilder.Entity<CarreraCohorte>().HasIndex(cc => new { cc.CaId, cc.CoId }).IsUnique();

            // Configuración de las Relaciones y Claves Foráneas

            //// Relación LOGIN -> USUARIO (Uno a Uno / Muchos a Uno, según esquema de base de datos)
            //modelBuilder.Entity<Login>()
            //    .HasOne(l => l.Usuario)
            //    .WithMany(u => u.Logins)
            //    .HasForeignKey(l => l.LoUser)
            //    .OnDelete(DeleteBehavior.Cascade);

            // Relación CARRERAS_COHORTES -> CARRERAS y COHORTE
            modelBuilder.Entity<CarreraCohorte>()
                .HasOne(cc => cc.Carrera)
                .WithMany(c => c.CarreraCohortes)
                .HasForeignKey(cc => cc.CaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CarreraCohorte>()
                .HasOne(cc => cc.Cohorte)
                .WithMany(co => co.CarreraCohortes)
                .HasForeignKey(cc => cc.CoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CarreraMateria>()
                .HasOne(cm => cm.CarreraCohorte)
                .WithMany(cc => cc.CarreraMaterias)
                .HasForeignKey(cm => cm.CaCoId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CarreraMateria>()
                .HasOne(cm => cm.Materia)
                .WithMany(m => m.CarreraMaterias)
                .HasForeignKey(cm => cm.MaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Índice único filtrado: AsClientGuid es nullable (solo los registros que
            // llegaron por la cola offline lo traen), y SQL Server trata NULL como valor
            // comparable en un índice único — sin el filtro, dos filas cargadas online
            // (sin GUID) chocarían entre sí como si fueran duplicadas.
            modelBuilder.Entity<Asistencia>()
                .HasIndex(a => a.AsClientGuid)
                .IsUnique()
                .HasFilter("[AsClientGuid] IS NOT NULL");

            modelBuilder.Entity<Asistencia>()
                .HasOne(a => a.Usuario)
                .WithMany(u => u.Asistencias)
                .HasForeignKey(a => a.UsId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Asistencia>()
                .HasOne(a => a.Materias)
                .WithMany(m => m.Asistencias)
                .HasForeignKey(a => a.MaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Asistencia>()
                .HasOne(a => a.CarreraMateria)
                .WithMany()
                .HasForeignKey(a => a.CaMaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Inscripciones>()
                .HasOne(i => i.Usuarios)
                .WithMany()
                .HasForeignKey(i => i.UsId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Inscripciones>()
                .HasOne(i => i.CarreraMateria)
                .WithMany()
                .HasForeignKey(i => i.CaMaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Usuario>()
                .HasMany(u => u.CarreraMaterias)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "UsuarioCarreraMateria",
                    j => j.HasOne<CarreraMateria>().WithMany().HasForeignKey("CarreraMateriasCaMaId"),
                    j => j.HasOne<Usuario>().WithMany().HasForeignKey("UsuariosUsId")
                );
        }
    }
}
