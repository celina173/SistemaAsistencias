using Microsoft.EntityFrameworkCore;
using System.Drawing.Drawing2D;
using ISFDyT124.Models;

namespace ISFDyT124.Data
{
    public class InstitutoDbContext : DbContext
    {
        public InstitutoDbContext(DbContextOptions<InstitutoDbContext> options)
            : base(options)
        {
        }

        // Definición de DbSets para los únicos modelos creados en el proyecto
        public DbSet<Rol> Roles { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<UsuarioRol> UsuarioRoles { get; set; } = null!;
        public DbSet<Materia> Materias { get; set; } = null!;
        public DbSet<Carrera> Carreras { get; set; } = null!;
        public DbSet<Cohorte> Cohortes { get; set; } = null!;
        public DbSet<Asistencia> Asistencias { get; set; } = null!;
        public DbSet<CarreraCohorte> CarreraCohortes { get; set; } = null!;
        public DbSet<CarreraMateria> CarreraMaterias { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapeo explícito y desactivación de autoincremento para PKs manuales (ya que no tienen IDENTITY en el SQL)
            modelBuilder.Entity<Rol>().Property(r => r.RoId).ValueGeneratedNever();
            modelBuilder.Entity<Usuario>().Property(u => u.UsId).ValueGeneratedNever();
            modelBuilder.Entity<UsuarioRol>().Property(ur => ur.UsRoId).ValueGeneratedNever();
            modelBuilder.Entity<Materia>().Property(m => m.MaId).ValueGeneratedNever();
            modelBuilder.Entity<Carrera>().Property(c => c.CaId).ValueGeneratedNever();
            modelBuilder.Entity<Cohorte>().Property(co => co.CoId).ValueGeneratedNever();
            modelBuilder.Entity<Asistencia>().Property(a => a.AsId).ValueGeneratedNever();
            modelBuilder.Entity<CarreraCohorte>().Property(cc => cc.CaCoId).ValueGeneratedNever();
            modelBuilder.Entity<CarreraMateria>().Property(cm => cm.CaMaId).ValueGeneratedNever();

            // Configurar DNI único de la tabla USUARIOS
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.UsDni)
                .IsUnique();

            // Configuración de las Relaciones y Claves Foráneas

            // Relación USUARIOS_ROLES -> USUARIOS y ROLES
            modelBuilder.Entity<UsuarioRol>()
                .HasOne(ur => ur.Usuario)
                .WithMany(u => u.UsuarioRoles)
                .HasForeignKey(ur => ur.UsId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UsuarioRol>()
                .HasOne(ur => ur.Rol)
                .WithMany(r => r.UsuarioRoles)
                .HasForeignKey(ur => ur.RoId)
                .OnDelete(DeleteBehavior.Restrict);

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

            // Relación CARRERAS_MATERIAS -> CARRERAS y MATERIAS
            modelBuilder.Entity<CarreraMateria>()
                .HasOne(cm => cm.Carrera)
                .WithMany(c => c.CarreraMaterias)
                .HasForeignKey(cm => cm.CaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CarreraMateria>()
                .HasOne(cm => cm.Materia)
                .WithMany(m => m.CarreraMaterias)
                .HasForeignKey(cm => cm.MaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación USUARIOS -> CARRERAS_COHORTES (solo Alumnos)
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.CarreraCohorte)
                .WithMany(cc => cc.Usuarios)
                .HasForeignKey(u => u.CaCoId)
                .OnDelete(DeleteBehavior.SetNull);

            // Relación M:N USUARIOS (Profesores) <-> CARRERAS_MATERIAS
            modelBuilder.Entity<Usuario>()
                .HasMany(u => u.CarreraMaterias)
                .WithMany(m => m.Usuarios)
                .UsingEntity(j => j.ToTable("UsuarioCarreraMateria"));

            // Relación ASISTENCIAS -> USUARIOS (Alumno) y MATERIAS
            modelBuilder.Entity<Asistencia>()
                .HasOne(a => a.Usuario)
                .WithMany(u => u.Asistencias)
                .HasForeignKey(a => a.UsId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Asistencia>()
                .HasOne(a => a.Materia)
                .WithMany(m => m.Asistencias)
                .HasForeignKey(a => a.MaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

