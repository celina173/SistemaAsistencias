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

        //public DbSet<UsuarioRol> UsuarioRoles { get; set; } = null!;
        //public DbSet<Login> Logins { get; set; } = null!;
        public DbSet<Materia> Materias { get; set; } = null!;
        public DbSet<Carrera> Carreras { get; set; } = null!;

        public DbSet<Cohorte> Cohortes { get; set; } = null!;
        public DbSet<Asistencia> Asistencias { get; set; } = null!;

        public DbSet<CarreraCohorte> CarreraCohortes { get; set; } = null!;
        public DbSet<CarreraMateria> CarreraMaterias { get; set; } = null!;
        public DbSet<UsuarioRol> UsuarioRoles { get; set; } = null!;
        public DbSet<Inscripciones> Inscripciones { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapeo explícito y desactivación de autoincremento para PKs manuales (ya que no tienen IDENTITY en el SQL)
            modelBuilder.Entity<Rol>().Property(r => r.RoId).ValueGeneratedNever();
            modelBuilder.Entity<Usuario>().Property(u => u.UsId).ValueGeneratedNever();
            //modelBuilder.Entity<UsuarioRol>().Property(ur => ur.UsRoId).ValueGeneratedNever();
            //modelBuilder.Entity<Login>().Property(l => l.LoId).ValueGeneratedNever();
            // Materia, Carrera y Asistencia pasaron a IDENTITY (columna autoincremental en SQL Server)
            //modelBuilder.Entity<Cohorte>().Property(co => co.CoId).ValueGeneratedNever();
            //modelBuilder.Entity<CarreraCohorte>().Property(cc => cc.CaCoId).ValueGeneratedNever();
            modelBuilder.Entity<CarreraMateria>().ToTable("CarreraMateria");
            modelBuilder.Entity<CarreraMateria>().Property(cm => cm.CaMaId).ValueGeneratedOnAdd();
            modelBuilder.Entity<CarreraCohorte>().Property(cc => cc.CaCoId).ValueGeneratedNever();
            modelBuilder.Entity<Cohorte>().Property(co => co.CoId).ValueGeneratedNever();
            modelBuilder.Entity<UsuarioRol>().Property(ur => ur.UsRoId).ValueGeneratedNever();

            // Configurar DNI único de la tabla USUARIOS
            modelBuilder.Entity<Usuario>().HasIndex(u => u.UsDni).IsUnique();

            // Restricciones de unicidad (ticket 4.14): nada más impedía cargar la misma
            // combinación dos veces. Requiere que la base ya esté libre de duplicados.
            // Filtrado (WHERE CaCoId IS NOT NULL): CaCoId es opcional mientras una cátedra
            // no tenga cohorte asignada todavía (ticket 4.12); SQL Server trata NULL como
            // valor comparable en un índice único, así que sin el filtro dos cátedras sin
            // cohorte asignada de la misma materia chocarían entre sí.
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

            //// Relación USUARIOS_ROLES -> USUARIOS y ROLES
            //modelBuilder.Entity<UsuarioRol>()
            //    .HasOne(ur => ur.Usuario)
            //    .WithMany(u => u.UsuarioRoles)
            //    .HasForeignKey(ur => ur.UsId)
            //    .OnDelete(DeleteBehavior.Cascade);

            //modelBuilder.Entity<UsuarioRol>()
            //    .HasOne(ur => ur.Rol)
            //    .WithMany(r => r.UsuarioRoles)
            //    .HasForeignKey(ur => ur.RoId)
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

            // Relación CARRERA_MATERIA -> CARRERA_COHORTE y MATERIAS. Una cátedra (Carrera+Materia)
            // queda atada a una cohorte concreta: "Inglés I" de la cohorte 2025 es una cátedra
            // distinta de "Inglés I" de la cohorte 2026. CaCoId es opcional (SetNull) para no
            // bloquear el borrado de una CarreraCohorte ni forzar a elegir cohorte al crear la
            // cátedra (ticket 4.12 todavía no tiene alta de Cohorte/CarreraCohorte terminada).
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

            // Relación USUARIOS_ROLES -> USUARIOS y ROLES
            modelBuilder.Entity<UsuarioRol>()
                .HasOne(ur => ur.Usuario)
                .WithMany(u => u.UsuarioRoles)
                .HasForeignKey(ur => ur.UsId)
                .OnDelete(DeleteBehavior.Cascade);

            // NO ACTION (no Cascade): Usuario ya cascadea desde Rol (Usuario.RoId es requerido),
            // y UsuarioRol ya cascadea desde Usuario justo arriba. Si esta FK también cascadeara,
            // SQL Server ve dos caminos de cascada distintos desde Roles hasta UsuarioRoles
            // (directo, y vía Usuarios) y rechaza crear el esquema en una base nueva
            // ("multiple cascade paths") — ticket 6.2.
            modelBuilder.Entity<UsuarioRol>()
                .HasOne(ur => ur.Rol)
                .WithMany(r => r.UsuarioRoles)
                .HasForeignKey(ur => ur.RoId)
                .OnDelete(DeleteBehavior.NoAction);

            // Relación ASISTENCIAS -> USUARIOS (Alumno) y MATERIAS
            modelBuilder.Entity<Asistencia>()
                .HasOne(a => a.Usuario)
                .WithMany(u => u.Asistencias)
                .HasForeignKey(a => a.UsId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Asistencia>()
                .HasOne(a => a.Materias)
                .WithMany(m => m.Asistencias)
                .HasForeignKey(a => a.MaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación ASISTENCIAS -> CARRERA_MATERIA. Sin esto, la navegación CarreraMateria de
            // Asistencia quedaba sin configurar y EF Core le creaba su propia columna sombra
            // (CarreraMateriaCaMaId) separada de CaMaId, que el código real nunca usa.
            // NO ACTION (no Cascade): Materias ya cascadea a Asistencias directo por MaId: si
            // esta también cascadeara, SQL Server rechaza el esquema por "multiple cascade
            // paths" (Materias -> Asistencias directo, y Materias -> CarreraMateria ->
            // Asistencias indirecto).
            modelBuilder.Entity<Asistencia>()
                .HasOne(a => a.CarreraMateria)
                .WithMany()
                .HasForeignKey(a => a.CaMaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación INSCRIPCIONES -> USUARIOS y CARRERA_MATERIA. Mismo problema que arriba:
            // sin configurar, EF creaba UsuariosUsId / CarreraMateriaCaMaId como columnas sombra
            // separadas de UsId / CaMaId, siempre en null.
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

            // Muchos a muchos Usuario <-> CarreraMateria (docentes asignados a cátedras),
            // mapeada a la tabla existente UsuarioCarreraMateria (columnas CarreraMateriasCaMaId / UsuariosUsId).
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
