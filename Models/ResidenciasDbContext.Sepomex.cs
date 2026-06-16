using Microsoft.EntityFrameworkCore;

namespace WebApiVinculacionProyectosV2.Models
{
    public partial class ResidenciasDbContext
    {
        public virtual DbSet<SepomexEstado> SepomexEstados { get; set; } = null!;
        public virtual DbSet<SepomexMunicipio> SepomexMunicipios { get; set; } = null!;
        public virtual DbSet<SepomexColonia> SepomexColonias { get; set; } = null!;

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            modelBuilder.HasCharSet("utf8mb4").UseCollation("utf8mb4_spanish_ci");
            modelBuilder.Entity<SepomexEstado>(e =>
            {
                e.ToTable("SepomexEstados");
                e.HasKey(x => x.EstadoId);
                e.Property(x => x.EstadoId).HasMaxLength(2).IsRequired();
                e.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
                e.Property(x => x.Abreviatura).HasMaxLength(10);
                e.Property(x => x.Rango1).HasMaxLength(5).IsRequired();
                e.Property(x => x.Rango2).HasMaxLength(5).IsRequired();
            });

            modelBuilder.Entity<SepomexMunicipio>(e =>
            {
                e.ToTable("SepomexMunicipios");
                e.HasKey(x => new { x.EstadoId, x.MunicipioId });
                e.Property(x => x.EstadoId).HasMaxLength(2).IsRequired();
                e.Property(x => x.MunicipioId).HasMaxLength(3).IsRequired();
                e.Property(x => x.Nombre).HasMaxLength(160).IsRequired();
                e.Property(x => x.Rango1).HasMaxLength(5).IsRequired();
                e.Property(x => x.Rango2).HasMaxLength(5).IsRequired();
                e.HasIndex(x => x.EstadoId);
            });

            modelBuilder.Entity<SepomexColonia>(e =>
            {
                e.ToTable("SepomexColonias");
                e.HasKey(x => x.ColoniaId);
                e.Property(x => x.ColoniaId).HasMaxLength(10).IsRequired();
                e.Property(x => x.EstadoId).HasMaxLength(2).IsRequired();
                e.Property(x => x.MunicipioId).HasMaxLength(3).IsRequired();
                e.Property(x => x.Cp).HasMaxLength(5).IsRequired();
                e.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
                e.Property(x => x.Cr).HasMaxLength(5);
                e.Property(x => x.FechaAct).IsRequired();

                e.HasIndex(x => x.Cp);
                e.HasIndex(x => new { x.EstadoId, x.MunicipioId });
            });
        }
    }
}