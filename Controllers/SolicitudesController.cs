using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Controllers;

[Authorize]
public class SolicitudesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public SolicitudesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> MisSolicitudes(
        EstadoSolicitud? estado,
        decimal? montoMin,
        decimal? montoMax,
        DateTime? fechaInicio,
        DateTime? fechaFin)
    {
        if (montoMin < 0 || montoMax < 0)
        {
            ModelState.AddModelError("", "No se aceptan montos negativos.");
        }

        if (fechaInicio.HasValue && fechaFin.HasValue && fechaInicio > fechaFin)
        {
            ModelState.AddModelError("", "La fecha de inicio no puede ser mayor que la fecha fin.");
        }

        var userId = _userManager.GetUserId(User);

        var query = _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .Where(s => s.Cliente.UsuarioId == userId);

        if (ModelState.IsValid)
        {
            if (estado.HasValue)
                query = query.Where(s => s.Estado == estado.Value);

            if (montoMin.HasValue)
                query = query.Where(s => s.MontoSolicitado >= montoMin.Value);

            if (montoMax.HasValue)
                query = query.Where(s => s.MontoSolicitado <= montoMax.Value);

            if (fechaInicio.HasValue)
                query = query.Where(s => s.FechaSolicitud >= fechaInicio.Value);

            if (fechaFin.HasValue)
                query = query.Where(s => s.FechaSolicitud <= fechaFin.Value);
        }

        return View(await query.ToListAsync());
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var userId = _userManager.GetUserId(User);

        var solicitud = await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .FirstOrDefaultAsync(s => s.Id == id && s.Cliente.UsuarioId == userId);

        if (solicitud == null)
            return NotFound();

        return View(solicitud);
    }
}