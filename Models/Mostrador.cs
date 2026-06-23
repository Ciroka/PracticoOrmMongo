using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PracticoOrmMongo.Models;

public class Mostrador
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("puntoDeVentaId")]
    public string PuntoDeVentaId { get; set; } = null!;
}
