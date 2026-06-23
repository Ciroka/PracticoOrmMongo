// Services/MostradorService.cs
using MongoDB.Driver;
using PracticoOrmMongo.Models;
using PracticoOrmMongo.Settings;
using Microsoft.Extensions.Options;

namespace PracticoOrmMongo.Services;

public class MostradorService
{
    private readonly IMongoCollection<Mostrador> _collection;

    public MostradorService(IMongoClient client, IOptions<MongoDbSettings> settings)
    {
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _collection = db.GetCollection<Mostrador>("mostradores");
    }

    public async Task<List<Mostrador>> GetAllAsync() =>
        await _collection.Find(_ => true).ToListAsync();

    public async Task<Mostrador?> GetByIdAsync(string id) =>
        await _collection.Find(m => m.Id == id).FirstOrDefaultAsync();

    public async Task<List<Mostrador>> GetByPuntoDeVentaAsync(string puntoDeVentaId) =>
        await _collection.Find(m => m.PuntoDeVentaId == puntoDeVentaId).ToListAsync();

    public async Task CreateAsync(Mostrador mostrador) =>
        await _collection.InsertOneAsync(mostrador);

    public async Task UpdateAsync(string id, Mostrador updated) =>
        await _collection.ReplaceOneAsync(m => m.Id == id, updated);

    public async Task DeleteAsync(string id) =>
        await _collection.DeleteOneAsync(m => m.Id == id);
}