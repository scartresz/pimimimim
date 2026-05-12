using Microsoft.AspNetCore.Mvc;
using ScarFood.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Globalization;
using System;

namespace ScarFood.Controllers
{
    public class AdminController : Controller
    {
        // CAMINHOS DOS ARQUIVOS
        private readonly string _caminhoProdutos = Path.Combine(Directory.GetCurrentDirectory(), "produtos.json");
        private readonly string _caminhoPedidos = Path.Combine(Directory.GetCurrentDirectory(), "pedidos.json");
        private readonly string _caminhoCategorias = Path.Combine(Directory.GetCurrentDirectory(), "categorias.json");

        // ==========================================
        // 1. FUNÇÕES AUXILIARES E DE LEITURA
        // ==========================================
        private List<Produto> CarregarCardapio()
        {
            if (!System.IO.File.Exists(_caminhoProdutos)) return new List<Produto>();
            var json = System.IO.File.ReadAllText(_caminhoProdutos);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<Produto>>(json, options) ?? new List<Produto>();
        }

        private List<Pedido> CarregarPedidos()
        {
            if (!System.IO.File.Exists(_caminhoPedidos)) return new List<Pedido>();
            var json = System.IO.File.ReadAllText(_caminhoPedidos);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<Pedido>>(json, options) ?? new List<Pedido>();
        }

        private void SalvarPedidos(List<Pedido> pedidos)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText(_caminhoPedidos, JsonSerializer.Serialize(pedidos, options));
        }

        private void SalvarCategoriasNoArquivo(List<CategoriaItem> lista)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText(_caminhoCategorias, JsonSerializer.Serialize(lista, options));
        }

        private List<CategoriaItem> CarregarCategorias()
        {
            if (System.IO.File.Exists(_caminhoCategorias))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(_caminhoCategorias);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var lista = JsonSerializer.Deserialize<List<CategoriaItem>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (lista != null && lista.Count > 0) return lista;
                    }
                }
                catch { }
            }

            var iniciais = new List<CategoriaItem> {
                new CategoriaItem { Nome = "Lanches", Ordem = 1 },
                new CategoriaItem { Nome = "Bebidas", Ordem = 2 },
                new CategoriaItem { Nome = "Pizzas", Ordem = 3 }
            };
            SalvarCategoriasNoArquivo(iniciais);
            return iniciais;
        }

        // ==========================================
        // 2. TELA PRINCIPAL (PAINEL)
        // ==========================================
        public IActionResult Painel()
        {
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
            if (!string.IsNullOrWhiteSpace(nome))
            {
                var categorias = CarregarCategorias();
                if (!categorias.Any(c => c.Nome.ToLower() == nome.Trim().ToLower()))
                {
                    categorias.Add(new CategoriaItem { 
                        Nome = nome.Trim(), 
                        Ordem = categorias.Count > 0 ? categorias.Max(x => x.Ordem) + 1 : 1 
                    });
                    SalvarCategoriasNoArquivo(categorias);
                }
            }
            return RedirectToAction("Painel");
        }

        [HttpPost]
        public IActionResult MoverCategoria(string nome, int direcao)
        {
            var categorias = CarregarCategorias().OrderBy(c => c.Ordem).ToList();
            var index = categorias.FindIndex(c => c.Nome == nome);

            if (index != -1)
            {
                if (direcao == -1 && index > 0) 
                {
                    var temp = categorias[index].Ordem;
                    categorias[index].Ordem = categorias[index - 1].Ordem;
                    categorias[index - 1].Ordem = temp;
                }
                else if (direcao == 1 && index < categorias.Count - 1) 
                {
                    var temp = categorias[index].Ordem;
                    categorias[index].Ordem = categorias[index + 1].Ordem;
                    categorias[index + 1].Ordem = temp;
                }
                SalvarCategoriasNoArquivo(categorias);
            }
            return RedirectToAction("Painel");
        }

        [HttpPost]
        public IActionResult ExcluirCategoria(string nome)
        {
            var categorias = CarregarCategorias();
            var item = categorias.FirstOrDefault(c => c.Nome == nome);
            if (item != null)
            {
                categorias.Remove(item);
                SalvarCategoriasNoArquivo(categorias);
            }
            return RedirectToAction("Painel");
        }

        // ==========================================
        // 4. GESTÃO DE CARDÁPIO (PRODUTOS)
        // ==========================================
        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> SalvarNovoProduto(string Nome, string Descricao, string Preco, string Categoria, Microsoft.AspNetCore.Http.IFormFile FotoArquivo, string FotoUrl, List<string> IngredientesNomes, List<string> IngredientesPrecos)
        {
            var produtos = CarregarCardapio();
            decimal precoLimpo = 0;
            // Reparo: Tratamento de ponto e vírgula para PT-BR
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

            int novoId = produtos.Count > 0 ? produtos.Max(p => p.Id) + 1 : 1;

            var novoProduto = new Produto
            {
                Id = novoId,
                Nome = Nome ?? "Sem Nome",
                Descricao = Descricao ?? "",
                Preco = precoLimpo,
                Categoria = Categoria ?? "Lanches",
                FotoUrl = fotoFinal,
                Vendas = 0,
                Extras = listaExtras
            };

            produtos.Add(novoProduto);
            System.IO.File.WriteAllText(_caminhoProdutos, JsonSerializer.Serialize(produtos, new JsonSerializerOptions { WriteIndented = true }));
            return RedirectToAction("Painel");
        }

        [HttpPost]
        public IActionResult ExcluirProduto(int id)
        {
            var produtos = CarregarCardapio();
            var produtoRemover = produtos.FirstOrDefault(p => p.Id == id);
            if (produtoRemover != null)
            {
                produtos.Remove(produtoRemover);
                System.IO.File.WriteAllText(_caminhoProdutos, JsonSerializer.Serialize(produtos, new JsonSerializerOptions { WriteIndented = true }));
            }
            return RedirectToAction("Painel");
        }

        [HttpGet]
        public IActionResult Editar(int id)
        {
            var produtos = CarregarCardapio();
            var produto = produtos.FirstOrDefault(p => p.Id == id);
            if (produto == null) return RedirectToAction("Painel");
            ViewBag.Categorias = CarregarCategorias().OrderBy(c => c.Ordem).ToList();
            return View(produto);
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> Editar(Produto pEditado, string Preco, Microsoft.AspNetCore.Http.IFormFile FotoNova, string FotoNovaUrl, List<string> IngredientesNomes, List<string> IngredientesPrecos)
        {
            var produtos = CarregarCardapio();
            var index = produtos.FindIndex(p => p.Id == pEditado.Id);

            if (index != -1)
            {
                // REPARO PRINCIPAL: O parâmetro mudou de PrecoStr para Preco para bater com o HTML
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
                    pEditado.FotoUrl = produtos[index].FotoUrl;
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
                pEditado.Vendas = produtos[index].Vendas;

                produtos[index] = pEditado;
                System.IO.File.WriteAllText(_caminhoProdutos, JsonSerializer.Serialize(produtos, new JsonSerializerOptions { WriteIndented = true }));
            }
            return RedirectToAction("Painel");
        }

        // ==========================================
        // 5. GESTÃO DE PEDIDOS E CHAT
        // ==========================================
        [HttpPost]
        public IActionResult AtualizarStatusPedido(int pedidoId, string status)
        {
            var pedidos = CarregarPedidos();
            var pedido = pedidos.FirstOrDefault(p => p.Id == pedidoId);
            if (pedido != null)
            {
                pedido.Status = status;
                SalvarPedidos(pedidos);
            }
            return RedirectToAction("Painel");
        }

        [HttpGet]
        public IActionResult ObterChatAdmin(int pedidoId)
        {
            var pedidos = CarregarPedidos();
            var pedido = pedidos.FirstOrDefault(p => p.Id == pedidoId);
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

            if (atualizou) SalvarPedidos(pedidos);

            return Json(new { success = true, mensagens = pedido.Mensagens ?? new List<MensagemChat>() });
        }

        [HttpPost]
        public IActionResult EnviarMensagemLoja(int pedidoId, string texto)
        {
            var pedidos = CarregarPedidos();
            var pedido = pedidos.FirstOrDefault(p => p.Id == pedidoId);

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
                SalvarPedidos(pedidos);

                return Json(new { success = true, mensagem = novaMsg });
            }
            return Json(new { success = false });
        }
    }
}