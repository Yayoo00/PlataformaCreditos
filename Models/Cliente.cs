namespace PlataformaCreditos.Models;
using System.ComponentModel.DataAnnotations;

public class Cliente
{
    public int Id { get; set; }

    public string UsuarioId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal IngresosMensuales { get; set; }

    public bool Activo { get; set; }

    public List<SolicitudCredito> Solicitudes { get; set; } = new();
}