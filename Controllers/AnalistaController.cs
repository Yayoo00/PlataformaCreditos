using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Controllers;

[Authorize(Roles = "Analista")]
public class AnalistaController : Controller
{
    private readonly ApplicationDbContext _context;

    public AnalistaController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var solicitudes = await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .Where(s => s.Estado == EstadoSolicitud.Pendiente)
            .ToListAsync();

        return View(solicitudes);
    }

    [HttpPost]
    public async Task<IActionResult> Aprobar(int id)
    {
        var solicitud = await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud == null) return NotFound();

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
            return BadRequest("Ya fue procesada");

        if (solicitud.MontoSolicitado > solicitud.Cliente.IngresosMensuales * 5)
            return BadRequest("Supera 5x ingresos");

        solicitud.Estado = EstadoSolicitud.Aprobado;

        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Rechazar(int id, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            return BadRequest("Motivo obligatorio");

        var solicitud = await _context.SolicitudesCredito.FindAsync(id);

        if (solicitud == null) return NotFound();

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
            return BadRequest("Ya fue procesada");

        solicitud.Estado = EstadoSolicitud.Rechazado;
        solicitud.MotivoRechazo = motivo;

        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }
}