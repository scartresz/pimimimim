using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators; 
using System.Collections.Generic;

namespace ScarFood.Models
{
    [BsonIgnoreExtraElements]
    public class Pedido
    {
        // Esta configuração aceita o ID "1" antigo e gera novos IDs automaticamente!
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))] 
        [BsonRepresentation(BsonType.String)]
        public string? Id { get; set; }
        
        public string UserEmail { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public string TipoEntrega { get; set; } = string.Empty;
        public string FormaPagamento { get; set; } = string.Empty;
        public string Total { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        
        public List<ItemCarrinho> Itens { get; set; } = new();
        public List<MensagemChat> Mensagens { get; set; } = new();
    }

    // Apenas a classe MensagemChat fica aqui, pois o ItemCarrinho já tem a própria casa dele
    [BsonIgnoreExtraElements]
    public class MensagemChat
    {
        public string Remetente { get; set; } = string.Empty;
        public string Texto { get; set; } = string.Empty;
        public string DataHora { get; set; } = string.Empty;
        public string StatusVisto { get; set; } = string.Empty;
    }
}