using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ScarFood.Models
{
    [BsonIgnoreExtraElements] // <-- ISSO RESOLVE A TELA PRETA DO CARRINHO
    public class EnderecoUsuario
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        public string UserEmail { get; set; } = string.Empty;
        public string CEP { get; set; } = string.Empty;
        public string Logradouro { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Complemento { get; set; } = string.Empty;
    }
}