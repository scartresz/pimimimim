using System.Collections.Generic;

namespace ScarFood.Models
{
    // A classe do pedido que você já tinha
    public class Pedido
    {
        public int Id { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public string TipoEntrega { get; set; } = string.Empty;
        public string FormaPagamento { get; set; } = string.Empty;
        public string Total { get; set; } = string.Empty;
        public string Status { get; set; } = "AGUARDANDO"; 
        
        public List<ItemCarrinho> Itens { get; set; } = new(); 

        // NOVA LINHA: O histórico de conversas deste pedido
        public List<MensagemChat> Mensagens { get; set; } = new(); 
    }

    // NOVA CLASSE: Para guardar a mensagem, quem enviou, horário e status
    public class MensagemChat
    {
        public string Remetente { get; set; } = "Cliente"; // "Cliente" ou "Loja"
        public string Texto { get; set; } = string.Empty;
        public string DataHora { get; set; } = string.Empty; // Ex: 18:45
        public string StatusVisto { get; set; } = "Enviado"; // "Enviado", "Entregue", "Visto"
    }
}