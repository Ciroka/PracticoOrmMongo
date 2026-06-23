// Services/RecetaService.cs
using MongoDB.Driver;
using PracticoOrmMongo.Models;
using PracticoOrmMongo.Settings;
using Microsoft.Extensions.Options;

namespace PracticoOrmMongo.Services;

public class RecetaService
{
    private readonly IMongoCollection<Receta> _collection;

    public RecetaService(IMongoClient client, IOptions<MongoDbSettings> settings)
    {
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _collection = db.GetCollection<Receta>("recetas");
    }

    public async Task<List<Receta>> GetAllAsync() =>
        await _collection.Find(_ => true).ToListAsync();

    public async Task<Receta?> GetByIdAsync(string id) =>
        await _collection.Find(r => r.Id == id).FirstOrDefaultAsync();

    public async Task CreateAsync(Receta receta) =>
        await _collection.InsertOneAsync(receta);

    public async Task UpdateAsync(string id, Receta updated) =>
        await _collection.ReplaceOneAsync(r => r.Id == id, updated);

    public async Task DeleteAsync(string id) =>
        await _collection.DeleteOneAsync(r => r.Id == id);

    // Agregar un detalle a la receta
    public async Task AddDetalleAsync(string recetaId, DetalleReceta detalle)
    {
        var update = Builders<Receta>.Update.Push(r => r.Detalles, detalle);
        await _collection.UpdateOneAsync(r => r.Id == recetaId, update);
    }

    // Reemplazar todos los detalles (útil para edición completa)
    public async Task UpdateDetallesAsync(string recetaId, List<DetalleReceta> detalles)
    {
        var update = Builders<Receta>.Update.Set(r => r.Detalles, detalles);
        await _collection.UpdateOneAsync(r => r.Id == recetaId, update);
    }
}