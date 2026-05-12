using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ScarFood.Models;
using ScarFood.Services; // ADICIONADO: Serviço do MongoDB
using MongoDB.Driver;    // ADICIONADO: Driver do NoSQL
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Globalization;
using System;

namespace ScarFood.Controllers
{
    public class CarrinhoController : Controller
    {
        // ADICIONADO: Injeção do Banco de Dados NoSQL
        private readonly DatabaseService _dbService;

        public CarrinhoController(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        private List<ItemCarrinho> ObterCarrinhoDaSessao()
        {
            var json = HttpContext.Session.GetString("Carrinho");
            return json == null ? new List<ItemCarrinho>() : JsonSerializer.Deserialize<List<ItemCarrinho>>(json) ?? new List<ItemCarrinho>();
        }

        private void SalvarCarrinhoNaSessao(List<ItemCarrinho> carrinho)
        {
            HttpContext.Session.SetString("Carrinho", JsonSerializer.Serialize(carrinho));
        }

        public IActionResult Index()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Autenticacao");

            var carrinho = ObterCarrinhoDaSessao();
            var subtotal = carrinho.Sum(i => i.PrecoTotal);

            ViewBag.Total = subtotal;
            ViewBag.Subtotal = subtotal;
            ViewBag.EnderecosSalvos = ObterEnderecosSalvos(email);

            return View(carrinho);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult Adicionar(string id, string nome, string preco, int quantidade, string observacao)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return Json(new { success = false, message = "Faça login primeiro" });

            decimal precoConvertido = 0;
            if (!decimal.TryParse(preco.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out precoConvertido))
            {
                return Json(new { success = false, message = "Erro no formato do preço" });
            }

            var carrinho = ObterCarrinhoDaSessao();

            string obsLimpa = (string.IsNullOrWhiteSpace(observacao) || observacao.ToLower() == "null") ? "" : observacao;

            carrinho.Add(new ItemCarrinho
            {
                Id = id,
                Nome = nome,
                PrecoUnitario = precoConvertido / (quantidade > 0 ? quantidade : 1),
                Quantidade = quantidade,
                Observacao = obsLimpa
            });

            SalvarCarrinhoNaSessao(carrinho);

            return Json(new
            {
                success = true,
                itens = carrinho,
                total = carrinho.Sum(i => i.PrecoTotal)
            });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult EditarItemAjax(int index, int quantidade, string observacao, string precoFinalStr)
        {
            var carrinho = ObterCarrinhoDaSessao();
            if (index >= 0 && index < carrinho.Count)
            {
                carrinho[index].Quantidade = quantidade > 0 ? quantidade : 1;
                carrinho[index].Observacao = (string.IsNullOrWhiteSpace(observacao) || observacao == "null") ? "" : observacao;

                if (!string.IsNullOrEmpty(precoFinalStr))
                {
                    if (decimal.TryParse(precoFinalStr.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal precoConvertido))
                    {
                        carrinho[index].PrecoUnitario = precoConvertido / carrinho[index].Quantidade;
                    }
                }

                SalvarCarrinhoNaSessao(carrinho);
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult RemoverEnderecoSalvo(string cep, string numero)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return Json(new { success = false });

            var safeCep = cep?.Replace("-", "").Trim() ?? "";
            var safeNum = numero?.Trim() ?? "";

            var resultado = _dbService.Enderecos.DeleteOne(e =>
                e.UserEmail == email &&
                e.CEP.Replace("-", "").Trim() == safeCep &&
                e.Numero.Trim() == safeNum);

            if (resultado.DeletedCount > 0)
            {
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        public IActionResult Remover(int index)
        {
            var carrinho = ObterCarrinhoDaSessao();
            if (index >= 0 && index < carrinho.Count) carrinho.RemoveAt(index);
            SalvarCarrinhoNaSessao(carrinho);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult RemoverAjax(int index)
        {
            var carrinho = ObterCarrinhoDaSessao();
            if (index >= 0 && index < carrinho.Count)
            {
                carrinho.RemoveAt(index);
                SalvarCarrinhoNaSessao(carrinho);
                return Json(new { success = true, total = carrinho.Sum(i => i.PrecoTotal) });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult LimparCarrinhoAjax()
        {
            HttpContext.Session.Remove("Carrinho");
            return Json(new { success = true });
        }

        public IActionResult Finalizar()
        {
            // PROTEÇÃO: Garantir que o usuário está logado antes de ver a tela de sucesso
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Autenticacao");

            HttpContext.Session.Remove("Carrinho");
            return View("PedidoSucesso");
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            // PROTEÇÃO: Garantir que não dê para acessar o checkout sem logar
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Autenticacao");

            return View();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult RepetirPedido(string pedidoId) 
        {
            // 1. PROTEÇÃO: O usuário DEVE estar logado
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Autenticacao");

            // 2. PROTEÇÃO (IDOR): Busca o pedido garantindo que ele pertence ao e-mail logado
            var pedidoAntigo = _dbService.Pedidos.Find(p => p.Id == pedidoId && p.UserEmail == email).FirstOrDefault();

            if (pedidoAntigo != null && pedidoAntigo.Itens != null)
            {
                var carrinho = ObterCarrinhoDaSessao();
                foreach (var item in pedidoAntigo.Itens)
                {
                    carrinho.Add(new ItemCarrinho
                    {
                        Id = item.Id,
                        Nome = item.Nome,
                        PrecoUnitario = item.PrecoUnitario,
                        Quantidade = item.Quantidade,
                        Observacao = item.Observacao
                    });
                }
                SalvarCarrinhoNaSessao(carrinho);
                return RedirectToAction("Index", "Carrinho");
            }
            return RedirectToAction("Pedidos", "Home");
        }

        [HttpPost]
        public IActionResult FinalizarPedido(string tipoEntrega, string pagamento, string total)
        {
            // PROTEÇÃO: Agora barra a requisição se não tiver logado (removido o "convidado")
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Autenticacao");

            var carrinho = ObterCarrinhoDaSessao();

            if (carrinho.Count > 0)
            {
                var novoPedido = new Pedido
                {
                    UserEmail = email,
                    Data = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                    TipoEntrega = tipoEntrega ?? "Delivery",
                    FormaPagamento = pagamento ?? "Não informado",
                    Total = total ?? "0,00",
                    Status = "AGUARDANDO",
                    Itens = carrinho
                };

                _dbService.Pedidos.InsertOne(novoPedido);
                HttpContext.Session.Remove("Carrinho");
            }

            return RedirectToAction("Pedidos", "Home");
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult SalvarEnderecoNovo(string cep, string logradouro, string numero, string bairro, string complemento)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return Json(new { success = false });

            var jaExiste = _dbService.Enderecos.Find(e =>
                e.UserEmail == email &&
                e.CEP == cep &&
                e.Numero == numero).Any();

            if (!jaExiste)
            {
                var novoEndereco = new EnderecoUsuario
                {
                    UserEmail = email,
                    CEP = cep,
                    Logradouro = logradouro,
                    Numero = numero,
                    Bairro = bairro,
                    Complemento = complemento ?? string.Empty
                };

                _dbService.Enderecos.InsertOne(novoEndereco);
                return Json(new { success = true, message = "Endereço novo salvo!" });
            }

            return Json(new { success = true, message = "Endereço já estava salvo." });
        }

        private List<EnderecoUsuario> ObterEnderecosSalvos(string email)
        {
            return _dbService.Enderecos.Find(e => e.UserEmail == email).ToList();
        }

        [HttpGet]
        [IgnoreAntiforgeryToken]
        public IActionResult ObterCarrinhoAjax()
        {
            var carrinho = ObterCarrinhoDaSessao();
            return Json(new
            {
                itens = carrinho,
                total = carrinho.Sum(i => i.PrecoTotal)
            });
        }
    }

    namespace ScarFood.Models
    {
        public class ItemCarrinho
        {
            public string Id { get; set; } 
            public string Nome { get; set; } = string.Empty;
            public decimal PrecoUnitario { get; set; }
            public int Quantidade { get; set; }
            public string Observacao { get; set; } = string.Empty;
            public decimal PrecoTotal => PrecoUnitario * Quantidade;
        }

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
}