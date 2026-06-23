using Microsoft.AspNetCore.Mvc;
using PracticoOrmMongo.Models;
using PracticoOrmMongo.Services;

namespace PracticoOrmMongo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PuntosDeVentaController : ControllerBase
{
    private readonly PuntoDeVentaService _service;

    public PuntosDeVentaController(PuntoDeVentaService service) =>
        _service = service;

    [HttpGet]
    public async Task<ActionResult<List<PuntoDeVenta>>> GetAll() =>
        Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<PuntoDeVenta>> GetById(string id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(PuntoDeVenta puntoDeVenta)
    {
        await _service.CreateAsync(puntoDeVenta);
        return CreatedAtAction(nameof(GetById), new { id = puntoDeVenta.Id }, puntoDeVenta);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, PuntoDeVenta updated)
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
}