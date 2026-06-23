using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PracticoOrmMongo.Models;

public class Ingrediente
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;

    [BsonElement("costo")]
    public decimal Costo { get; set; }
}