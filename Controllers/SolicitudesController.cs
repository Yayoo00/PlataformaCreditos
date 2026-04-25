using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace PlataformaCreditos.Controllers;

[Authorize]
public class SolicitudesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IDistributedCache _cache;
    public SolicitudesController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IDistributedCache cache)    {
        _context = context;
        _userManager = userManager;
        _cache = cache;
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
        var cacheKey = $"solicitudes_{userId}";

// 🔹 Intentar leer cache
var cacheData = await _cache.GetStringAsync(cacheKey);

if (cacheData != null)
{
    var solicitudesCache = JsonSerializer.Deserialize<List<SolicitudCredito>>(cacheData);
    return View(solicitudesCache);
}

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

        var solicitudes = await query.ToListAsync();

// 🔹 Guardar en cache por 60 segundos
await _cache.SetStringAsync(
    cacheKey,
    JsonSerializer.Serialize(solicitudes),
    new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
    });

return View(solicitudes);
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var userId = _userManager.GetUserId(User);

        var solicitud = await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .FirstOrDefaultAsync(s => s.Id == id && s.Cliente.UsuarioId == userId);

        if (solicitud == null)
            return NotFound();

        HttpContext.Session.SetInt32("UltimaSolicitudId", solicitud.Id);
        HttpContext.Session.SetString("UltimaSolicitudMonto", solicitud.MontoSolicitado.ToString());
        return View(solicitud);
    }
    public IActionResult Crear()
    {
        return View();
    }
    [HttpPost]
    public async Task<IActionResult> Crear(SolicitudCredito solicitud)
    {
    var userId = _userManager.GetUserId(User);

    var cliente = await _context.Clientes
        .FirstOrDefaultAsync();

    if (cliente == null)
    {
        ModelState.AddModelError("", "No existe cliente asociado.");
        return View(solicitud);
    }

    if (!cliente.Activo)
    {
        ModelState.AddModelError("", "El cliente no está activo.");
        return View(solicitud);
    }

    bool tienePendiente = await _context.SolicitudesCredito
        .AnyAsync(s => s.ClienteId == cliente.Id && s.Estado == EstadoSolicitud.Pendiente);

    if (tienePendiente)
    {
        ModelState.AddModelError("", "Ya tienes una solicitud pendiente.");
        return View(solicitud);
    }

    if (solicitud.MontoSolicitado <= 0)
    {
        ModelState.AddModelError("", "El monto solicitado debe ser mayor a cero.");
        return View(solicitud);
    }

    if (solicitud.MontoSolicitado > cliente.IngresosMensuales * 10)
    {
        ModelState.AddModelError("", "El monto solicitado no puede superar 10 veces los ingresos mensuales.");
        return View(solicitud);
    }

    solicitud.ClienteId = cliente.Id;
    solicitud.Estado = EstadoSolicitud.Pendiente;
    solicitud.FechaSolicitud = DateTime.Now;

    _context.SolicitudesCredito.Add(solicitud);
    await _context.SaveChangesAsync();
    await _cache.RemoveAsync($"solicitudes_{userId}");

    TempData["Mensaje"] = "Solicitud registrada correctamente.";
    return RedirectToAction("MisSolicitudes");
    }
}