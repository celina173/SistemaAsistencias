using Microsoft.EntityFrameworkCore;
using ISFDyT124.Models;

namespace ISFDyT124.Data
{
    /// <summary>
    /// Contexto unificado de la base de datos del sistema de asistencias.
    /// Reemplaza a InstitutoDbContext y SiAsContext.
    /// </summary>
    public class AsistenciaContext : DbContext
    {
        public AsistenciaContext(DbContextOptions<AsistenciaContext> options) : base(options)
        {
        }

        // ── DbSets (tablas) ──────────────────────────────────────────────────
        public DbSet<Carrera> Carreras { get; set; } = null!;
        public DbSet<Materia> Materias { get; set; } = null!;
        public DbSet<CarreraMateria> CarreraMaterias { get; set; } = null!;
        public DbSet<Cohorte> Cohortes { get; set; } = null!;
        public DbSet<CarreraCohorte> CarreraCohortes { get; set; } = null!;
        public DbSet<Rol> Roles { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Mapeo de nombres de tablas ───────────────────────────────────
            modelBuilder.Entity<Carrera>().ToTable("CARRERAS");
            modelBuilder.Entity<Materia>().ToTable("MATERIAS");
            modelBuilder.Entity<CarreraMateria>().ToTable("CARRERAS_MATERIAS");
            modelBuilder.Entity<Cohorte>().ToTable("COHORTES");
            modelBuilder.Entity<CarreraCohorte>().ToTable("CARRERAS_COHORTES");
            modelBuilder.Entity<Rol>().ToTable("ROLES");
            modelBuilder.Entity<Usuario>().ToTable("USUARIOS");

            // ── Relación CARRERAS_MATERIAS → CARRERAS y MATERIAS ────────────
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

            // ── Relación CARRERAS_COHORTES → CARRERAS y COHORTES ────────────
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

            // ── Relación USUARIOS → ROLES ────────────────────────────────────
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Rol)
                .WithMany(r => r.Usuarios)
                .HasForeignKey(u => u.RoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}