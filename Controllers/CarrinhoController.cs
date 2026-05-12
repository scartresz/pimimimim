using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ScarFood.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Globalization;
using System;

namespace ScarFood.Controllers
{
    public class CarrinhoController : Controller
    {


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
        public IActionResult Adicionar(int id, string nome, string preco, int quantidade, string observacao)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return Json(new { success = false, message = "Faça login primeiro" });

            decimal precoConvertido = 0;
            if (!decimal.TryParse(preco.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out precoConvertido))
            {
                return Json(new { success = false, message = "Erro no formato do preço" });
            }

            var carrinho = ObterCarrinhoDaSessao();

            // CORREÇÃO DO NULL: Se for vazio ou a palavra "null", vira texto limpo
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

        // NOVA FUNÇÃO: Para o botão do Lápis
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult EditarItemAjax(int index, int quantidade, string observacao, string precoFinalStr)
        {
            var carrinho = ObterCarrinhoDaSessao();
            if (index >= 0 && index < carrinho.Count)
            {
                carrinho[index].Quantidade = quantidade > 0 ? quantidade : 1;
                carrinho[index].Observacao = (string.IsNullOrWhiteSpace(observacao) || observacao == "null") ? "" : observacao;

                // ADICIONADO: Se o preço mudou por causa de ingredientes extras, atualizamos o valor!
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

            var caminho = Path.Combine(Directory.GetCurrentDirectory(), "enderecos_usuarios.json");
            if (System.IO.File.Exists(caminho))
            {
                var json = System.IO.File.ReadAllText(caminho);
                var enderecos = JsonSerializer.Deserialize<List<EnderecoUsuario>>(json) ?? new List<EnderecoUsuario>();

                // BUG CORRIGIDO: Removendo traços e espaços para garantir que a exclusão funcione
                var safeCep = cep?.Replace("-", "").Trim() ?? "";
                var safeNum = numero?.Trim() ?? "";

                var enderecoParaRemover = enderecos.FirstOrDefault(e =>
                    e.UserEmail == email &&
                    e.CEP.Replace("-", "").Trim() == safeCep &&
                    e.Numero.Trim() == safeNum);

                if (enderecoParaRemover != null)
                {
                    enderecos.Remove(enderecoParaRemover);
                    System.IO.File.WriteAllText(caminho, JsonSerializer.Serialize(enderecos));
                    return Json(new { success = true });
                }
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
            HttpContext.Session.Remove("Carrinho");
            return View("PedidoSucesso");
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            return View();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult RepetirPedido(int pedidoId)
        {
            var caminhoPedidos = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "pedidos.json");
            if (System.IO.File.Exists(caminhoPedidos))
            {
                var json = System.IO.File.ReadAllText(caminhoPedidos);
                var pedidos = System.Text.Json.JsonSerializer.Deserialize<List<Pedido>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Pedido>();
                var pedidoAntigo = pedidos.FirstOrDefault(p => p.Id == pedidoId);

                if (pedidoAntigo != null && pedidoAntigo.Itens != null)
                {
                    var carrinho = ObterCarrinhoDaSessao();
                    // Joga todos os itens antigos pro carrinho novo
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
                    return RedirectToAction("Index", "Carrinho"); // Manda o cliente pro carrinho!
                }
            }
            return RedirectToAction("Pedidos", "Home");
        }

        [HttpPost]
        public IActionResult FinalizarPedido(string tipoEntrega, string pagamento, string total)
        {
            var email = HttpContext.Session.GetString("UserEmail") ?? "convidado@scarfood.com";
            var carrinho = ObterCarrinhoDaSessao();

            if (carrinho.Count > 0)
            {
                var caminhoPedidos = Path.Combine(Directory.GetCurrentDirectory(), "pedidos.json");
                List<PedidoModel> pedidos = new();

                if (System.IO.File.Exists(caminhoPedidos))
                {
                    var jsonExistente = System.IO.File.ReadAllText(caminhoPedidos);
                    if (!string.IsNullOrWhiteSpace(jsonExistente))
                    {
                        pedidos = JsonSerializer.Deserialize<List<PedidoModel>>(jsonExistente) ?? new();
                    }
                }

                var novoPedido = new PedidoModel
                {
                    Id = pedidos.Count + 1,
                    UserEmail = email,
                    Data = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                    TipoEntrega = tipoEntrega ?? "Delivery",
                    FormaPagamento = pagamento ?? "Não informado",
                    Total = total ?? "0,00",
                    Itens = carrinho
                };

                pedidos.Add(novoPedido);
                System.IO.File.WriteAllText(caminhoPedidos, JsonSerializer.Serialize(pedidos));

                HttpContext.Session.Remove("Carrinho");
            }

            return RedirectToAction("Pedidos", "Home");
        }

        // Classes com inicialização vazia para remover os avisos CS8618
        public class PedidoModel
        {
            public int Id { get; set; }
            public string UserEmail { get; set; } = string.Empty;
            public string Data { get; set; } = string.Empty;
            public string TipoEntrega { get; set; } = string.Empty;
            public string FormaPagamento { get; set; } = string.Empty;
            public string Total { get; set; } = string.Empty;
            public List<ItemCarrinho> Itens { get; set; } = new();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult SalvarEnderecoNovo(string cep, string logradouro, string numero, string bairro, string complemento)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return Json(new { success = false });

            var caminho = Path.Combine(Directory.GetCurrentDirectory(), "enderecos_usuarios.json");
            List<EnderecoUsuario> enderecos = new();

            if (System.IO.File.Exists(caminho))
            {
                var json = System.IO.File.ReadAllText(caminho);
                enderecos = JsonSerializer.Deserialize<List<EnderecoUsuario>>(json) ?? new();
            }

            bool jaExiste = enderecos.Any(e =>
                e.UserEmail == email &&
                e.CEP == cep &&
                e.Numero == numero);

            if (!jaExiste)
            {
                enderecos.Add(new EnderecoUsuario
                {
                    UserEmail = email,
                    CEP = cep,
                    Logradouro = logradouro,
                    Numero = numero,
                    Bairro = bairro,
                    Complemento = complemento ?? string.Empty
                });

                System.IO.File.WriteAllText(caminho, JsonSerializer.Serialize(enderecos));
                return Json(new { success = true, message = "Endereço novo salvo!" });
            }

            return Json(new { success = true, message = "Endereço já estava salvo." });
        }

        private List<EnderecoUsuario> ObterEnderecosSalvos(string email)
        {
            var caminho = Path.Combine(Directory.GetCurrentDirectory(), "enderecos_usuarios.json");
            if (!System.IO.File.Exists(caminho)) return new();

            var json = System.IO.File.ReadAllText(caminho);
            var todos = JsonSerializer.Deserialize<List<EnderecoUsuario>>(json) ?? new();
            return todos.Where(e => e.UserEmail == email).ToList();
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
            public int ProdutoId { get; set; }
            public string Nome { get; set; } = string.Empty;
            public decimal PrecoUnitario { get; set; }
            public int Quantidade { get; set; }
            public string Observacao { get; set; } = string.Empty;
            public decimal PrecoTotal => PrecoUnitario * Quantidade;
        }

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

}