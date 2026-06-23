// Controllers/VentasController.cs
using Microsoft.AspNetCore.Mvc;
using PracticoOrmMongo.Models;
using PracticoOrmMongo.Services;

namespace PracticoOrmMongo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VentasController : ControllerBase
{
    private readonly VentaService _service;

    public VentasController(VentaService service) =>
        _service = service;

    [HttpGet]
    public async Task<ActionResult<List<Venta>>> GetAll() =>
        Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<Venta>> GetById(string id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("porMostrador/{mostradorId}")]
    public async Task<ActionResult<List<Venta>>> GetByMostrador(string mostradorId) =>
        Ok(await _service.GetByMostradorAsync(mostradorId));

    [HttpPost]
    public async Task<IActionResult> Create(Venta venta)
    {
        await _service.CreateAsync(venta);
        return CreatedAtAction(nameof(GetById), new { id = venta.Id }, venta);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, Venta updated)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing is null) return NotFound();
        await _service.UpdateAsync(id, updated);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing is null) return NotFound();
        await _service.DeleteAsync(id);
        return NoContent();
    }

    // Endpoints para manejar los detalles embebidos
    [HttpPost("{id}/detalles")]
    public async Task<IActionResult> AddDetalle(string id, DetalleVenta detalle)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing is null) return NotFound();
        await _service.AddDetalleAsync(id, detalle);
        return NoContent();
    }

    [HttpPut("{id}/detalles")]
    public async Task<IActionResult> UpdateDetalles(string id, List<DetalleVenta> detalles)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing is null) return NotFound();
        await _service.UpdateDetallesAsync(id, detalles);
        return NoContent();
    }
}