using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PracticoOrmMongo.Models;

public class DetalleReceta
{
    // No necesita [BsonId] porque no es una colección propia
    [BsonElement("cantidad")]
    public decimal Cantidad { get; set; }

    [BsonElement("ingrediente")]
    public Ingrediente Ingrediente { get; set; } = null!;
}