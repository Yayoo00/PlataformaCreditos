using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<SolicitudCredito> SolicitudesCredito { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Cliente>()
            .HasCheckConstraint("CK_Cliente_Ingresos", "IngresosMensuales > 0");

        builder.Entity<SolicitudCredito>()
            .HasCheckConstraint("CK_Solicitud_Monto", "MontoSolicitado > 0");

        builder.Entity<Cliente>().HasData(
            new Cliente
            {
                Id = 1,
                UsuarioId = "user1",
                IngresosMensuales = 2000,
                Activo = true
            },
            new Cliente
            {
                Id = 2,
                UsuarioId = "user2",
                IngresosMensuales = 3000,
                Activo = true
            }
        );

        builder.Entity<SolicitudCredito>().HasData(
            new SolicitudCredito
            {
                Id = 1,
                ClienteId = 1,
                MontoSolicitado = 1000,
                FechaSolicitud = new DateTime(2026, 1, 1),
                Estado = EstadoSolicitud.Pendiente
            },
            new SolicitudCredito
            {
                Id = 2,
                ClienteId = 2,
                MontoSolicitado = 500,
                FechaSolicitud = new DateTime(2026, 1, 2),
                Estado = EstadoSolicitud.Aprobado
            }
        );
    }
}