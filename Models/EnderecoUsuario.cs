namespace ScarFood.Models
{
    public class EnderecoUsuario
    {
        public string UserEmail { get; set; } = string.Empty;
        public string CEP { get; set; } = string.Empty;
        public string Logradouro { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Complemento { get; set; } = string.Empty;
    }
}