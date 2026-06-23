using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PracticoOrmMongo.Models;

namespace PracticoOrmMongo.Models; 

public class PuntoDeVenta
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;
}