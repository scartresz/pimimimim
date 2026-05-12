using Microsoft.AspNetCore.Mvc;
using ScarFood.Models;
using ScarFood.Services; 
using MongoDB.Driver;    
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System;
using Microsoft.AspNetCore.Http; // ADICIONADO: Necessário para ler a Session

namespace ScarFood.Controllers
{
    public class AdminController : Controller
    {
        private readonly DatabaseService _dbService;

        public AdminController(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        // ==========================================
        // FUNÇÃO AUXILIAR DE SEGURANÇA (Privada)
        // ==========================================
        private bool EhAdmin()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            return email == "admin@scarfood.com";
        }

        // ==========================================
        // 1. FUNÇÕES AUXILIARES E DE LEITURA
        // ==========================================
        private List<Produto> CarregarCardapio()
        {
            return _dbService.Produtos.Find(_ => true).ToList();
        }

        private List<Pedido> CarregarPedidos()
        {
            return _dbService.Pedidos.Find(_ => true).ToList();
        }

        private List<CategoriaItem> CarregarCategorias()
        {
            var categorias = _dbService.Categorias.Find(_ => true).ToList();

            if (categorias.Count == 0)
            {
                var iniciais = new List<CategoriaItem> {
                    new CategoriaItem { Nome = "Lanches", Ordem = 1 },
                    new CategoriaItem { Nome = "Bebidas", Ordem = 2 },
                    new CategoriaItem { Nome = "Pizzas", Ordem = 3 }
                };
                _dbService.Categorias.InsertMany(iniciais);
                return iniciais;
            }
            return categorias;
        }

        // ==========================================
        // 2. TELA PRINCIPAL (PAINEL)
        // ==========================================
        public IActionResult Painel()
        {
            if (!EhAdmin()) return RedirectToAction("Index", "Home");

            var produtos = CarregarCardapio();
            ViewBag.Pedidos = CarregarPedidos().OrderByDescending(p => p.Id).ToList();
            ViewBag.Categorias = CarregarCategorias().OrderBy(c => c.Ordem).ToList();
            return View(produtos);
        }

        // ==========================================
        // 3. GESTÃO DE CATEGORIAS
        // ==========================================
        [HttpPost]
        public IActionResult AdicionarCategoria(string nome)
        {
            if (!EhAdmin()) return RedirectToAction("Index", "Home");

            if (!string.IsNullOrWhiteSpace(nome))
            {
                var categorias = CarregarCategorias();
                if (!categorias.Any(c => c.Nome.ToLower() == nome.Trim().ToLower()))
                {
                    var novaCategoria = new CategoriaItem {
                        Nome = nome.Trim(),
                        Ordem = categorias.Count > 0 ? categorias.Max(x => x.Ordem) + 1 : 1
                    };
                    _dbService.Categorias.InsertOne(novaCategoria);
                }
            }
            return RedirectToAction("Painel");
        }

        [HttpPost]
        public IActionResult MoverCategoria(string nome, int direcao)
        {
            if (!EhAdmin()) return RedirectToAction("Index", "Home");

            var categorias = CarregarCategorias().OrderBy(c => c.Ordem).ToList();
            var index = categorias.FindIndex(c => c.Nome == nome);

            if (index != -1)
            {
                if (direcao == -1 && index > 0)
                {
                    var temp = categorias[index].Ordem;
                    categorias[index].Ordem = categorias[index - 1].Ordem;
                    categorias[index - 1].Ordem = temp;

                    _dbService.Categorias.ReplaceOne(c => c.Id == categorias[index].Id, categorias[index]);
                    _dbService.Categorias.ReplaceOne(c => c.Id == categorias[index - 1].Id, categorias[index - 1]);
                }
                else if (direcao == 1 && index < categorias.Count - 1)
                {
                    var temp = categorias[index].Ordem;
                    categorias[index].Ordem = categorias[index + 1].Ordem;
                    categorias[index + 1].Ordem = temp;

                    _dbService.Categorias.ReplaceOne(c => c.Id == categorias[index].Id, categorias[index]);
                    _dbService.Categorias.ReplaceOne(c => c.Id == categorias[index + 1].Id, categorias[index + 1]);
                }
            }
            return RedirectToAction("Painel");
        }

        [HttpPost]
        public IActionResult ExcluirCategoria(string nome)
        {
            if (!EhAdmin()) return RedirectToAction("Index", "Home");

            _dbService.Categorias.DeleteOne(c => c.Nome == nome);
            return RedirectToAction("Painel");
        }

        // ==========================================
        // 4. GESTÃO DE CARDÁPIO (PRODUTOS)
        // ==========================================
        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> SalvarNovoProduto(string Nome, string Descricao, string Preco, string Categoria, Microsoft.AspNetCore.Http.IFormFile FotoArquivo, string FotoUrl, List<string> IngredientesNomes, List<string> IngredientesPrecos)
        {
            if (!EhAdmin()) return RedirectToAction("Index", "Home");

            decimal precoLimpo = 0;
            if (!string.IsNullOrEmpty(Preco))
            {
                string valorParaParse = Preco.Replace(".", "").Replace(",", ".");
                decimal.TryParse(valorParaParse, NumberStyles.Any, CultureInfo.InvariantCulture, out precoLimpo);
            }

            string fotoFinal = "/img/default.png";
            if (FotoArquivo != null && FotoArquivo.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                var nomeUnico = System.Guid.NewGuid().ToString() + "_" + FotoArquivo.FileName;
                var filePath = Path.Combine(uploadsFolder, nomeUnico);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await FotoArquivo.CopyToAsync(stream);
                }
                fotoFinal = "/img/" + nomeUnico;
            }
            else if (!string.IsNullOrWhiteSpace(FotoUrl))
            {
                fotoFinal = FotoUrl;
            }

            var listaExtras = new List<IngredienteExtra>();
            if (IngredientesNomes != null && IngredientesPrecos != null)
            {
                for (int i = 0; i < IngredientesNomes.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(IngredientesNomes[i]))
                    {
                        decimal pExtra = 0;
                        if (i < IngredientesPrecos.Count && !string.IsNullOrEmpty(IngredientesPrecos[i]))
                        {
                            string vExtra = IngredientesPrecos[i].Replace(".", "").Replace(",", ".");
                            decimal.TryParse(vExtra, NumberStyles.Any, CultureInfo.InvariantCulture, out pExtra);
                        }
                        listaExtras.Add(new IngredienteExtra { Nome = IngredientesNomes[i], Preco = pExtra });
                    }
                }
            }

            var novoProduto = new Produto
            {
                Nome = Nome ?? "Sem Nome",
                Descricao = Descricao ?? "",
                Preco = precoLimpo,
                Categoria = Categoria ?? "Lanches",
                FotoUrl = fotoFinal,
                Vendas = 0,
                Extras = listaExtras
            };

            _dbService.Produtos.InsertOne(novoProduto);
            return RedirectToAction("Painel");
        }

        [HttpPost]
        public IActionResult ExcluirProduto(string id)
        {
            if (!EhAdmin()) return RedirectToAction("Index", "Home");

            _dbService.Produtos.DeleteOne(p => p.Id == id);
            return RedirectToAction("Painel");
        }

        [HttpGet]
        public IActionResult Editar(string id)
        {
            if (!EhAdmin()) return RedirectToAction("Index", "Home");

            var produto = _dbService.Produtos.Find(p => p.Id == id).FirstOrDefault();
            if (produto == null) return RedirectToAction("Painel");
            ViewBag.Categorias = CarregarCategorias().OrderBy(c => c.Ordem).ToList();
            return View(produto);
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> Editar(Produto pEditado, string Preco, Microsoft.AspNetCore.Http.IFormFile FotoNova, string FotoNovaUrl, List<string> IngredientesNomes, List<string> IngredientesPrecos)
        {
            if (!EhAdmin()) return RedirectToAction("Index", "Home");

            var produtoOriginal = _dbService.Produtos.Find(p => p.Id == pEditado.Id).FirstOrDefault();

            if (produtoOriginal != null)
            {
                decimal precoLimpo = 0;
                if (!string.IsNullOrEmpty(Preco))
                {
                    string valorParaParse = Preco.Replace(".", "").Replace(",", ".");
                    decimal.TryParse(valorParaParse, NumberStyles.Any, CultureInfo.InvariantCulture, out precoLimpo);
                }
                pEditado.Preco = precoLimpo;

                if (FotoNova != null && FotoNova.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    var nomeUnico = System.Guid.NewGuid().ToString() + "_" + FotoNova.FileName;
                    var filePath = Path.Combine(uploadsFolder, nomeUnico);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await FotoNova.CopyToAsync(stream);
                    }
                    pEditado.FotoUrl = "/img/" + nomeUnico;
                }
                else if (!string.IsNullOrWhiteSpace(FotoNovaUrl))
                {
                    pEditado.FotoUrl = FotoNovaUrl;
                }
                else
                {
                    pEditado.FotoUrl = produtoOriginal.FotoUrl;
                }

                var listaExtras = new List<IngredienteExtra>();
                if (IngredientesNomes != null && IngredientesPrecos != null)
                {
                    for (int i = 0; i < IngredientesNomes.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(IngredientesNomes[i]))
                        {
                            decimal pExtra = 0;
                            if (i < IngredientesPrecos.Count && !string.IsNullOrEmpty(IngredientesPrecos[i]))
                            {
                                string vExtra = IngredientesPrecos[i].Replace(".", "").Replace(",", ".");
                                decimal.TryParse(vExtra, NumberStyles.Any, CultureInfo.InvariantCulture, out pExtra);
                            }
                            listaExtras.Add(new IngredienteExtra { Nome = IngredientesNomes[i], Preco = pExtra });
                        }
                    }
                }
                pEditado.Extras = listaExtras;
                pEditado.Vendas = produtoOriginal.Vendas;

                _dbService.Produtos.ReplaceOne(p => p.Id == pEditado.Id, pEditado);
            }
            return RedirectToAction("Painel");
        }

        // ==========================================
        // 5. GESTÃO DE PEDIDOS E CHAT
        // ==========================================
        [HttpPost]
        public IActionResult AtualizarStatusPedido(string pedidoId, string status)
        {
            if (!EhAdmin()) return RedirectToAction("Index", "Home");

            var pedido = _dbService.Pedidos.Find(p => p.Id == pedidoId).FirstOrDefault();
            if (pedido != null)
            {
                pedido.Status = status;
                _dbService.Pedidos.ReplaceOne(p => p.Id == pedidoId, pedido);
            }
            return RedirectToAction("Painel");
        }

        [HttpGet]
        public IActionResult ObterChatAdmin(string pedidoId)
        {
            if (!EhAdmin()) return Json(new { success = false });

            var pedido = _dbService.Pedidos.Find(p => p.Id == pedidoId).FirstOrDefault();
            if (pedido == null) return Json(new { success = false });

            bool atualizou = false;
            if (pedido.Mensagens != null)
            {
                foreach (var m in pedido.Mensagens.Where(m => m.Remetente == "Cliente" && m.StatusVisto != "Visto"))
                {
                    m.StatusVisto = "Visto";
                    atualizou = true;
                }
            }

            if (atualizou) _dbService.Pedidos.ReplaceOne(p => p.Id == pedidoId, pedido);

            return Json(new { success = true, mensagens = pedido.Mensagens ?? new List<MensagemChat>() });
        }

        [HttpPost]
        public IActionResult EnviarMensagemLoja(string pedidoId, string texto)
        {
            if (!EhAdmin()) return Json(new { success = false });

            var pedido = _dbService.Pedidos.Find(p => p.Id == pedidoId).FirstOrDefault();

            if (pedido != null && !string.IsNullOrWhiteSpace(texto))
            {
                if (pedido.Mensagens == null) pedido.Mensagens = new List<MensagemChat>();

                var novaMsg = new MensagemChat
                {
                    Remetente = "Loja",
                    Texto = texto,
                    DataHora = System.DateTime.Now.ToString("HH:mm"),
                    StatusVisto = "Enviado"
                };

                pedido.Mensagens.Add(novaMsg);
                _dbService.Pedidos.ReplaceOne(p => p.Id == pedidoId, pedido);

                return Json(new { success = true, mensagem = novaMsg });
            }
            return Json(new { success = false });
        }
    }
}