using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ScarFood.Models
{
    public class Usuario
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)] // Adicione esta linha
        public string? Id { get; set; }
        
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string? SenhaAtual { get; set; }
        public string? Cpf { get; set; }
        public string? Telefone { get; set; }
        public string? Cep { get; set; }
        public string? Logradouro { get; set; }
        public string? Numero { get; set; }
        public string? Complemento { get; set; }
    }
}