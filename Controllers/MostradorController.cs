// Controllers/MostradoresController.cs
using Microsoft.AspNetCore.Mvc;
using PracticoOrmMongo.Models;
using PracticoOrmMongo.Services;

namespace PracticoOrmMongo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MostradoresController : ControllerBase
{
    private readonly MostradorService _service;

    public MostradoresController(MostradorService service) =>
        _service = service;

    [HttpGet]
    public async Task<ActionResult<List<Mostrador>>> GetAll() =>
        Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<Mostrador>> GetById(string id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("porPuntoDeVenta/{puntoDeVentaId}")]
    public async Task<ActionResult<List<Mostrador>>> GetByPuntoDeVenta(string puntoDeVentaId) =>
        Ok(await _service.GetByPuntoDeVentaAsync(puntoDeVentaId));

    [HttpPost]
    public async Task<IActionResult> Create(Mostrador mostrador)
    {
        await _service.CreateAsync(mostrador);
        return CreatedAtAction(nameof(GetById), new { id = mostrador.Id }, mostrador);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, Mostrador updated)
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