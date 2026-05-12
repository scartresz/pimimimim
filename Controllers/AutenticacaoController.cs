using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ScarFood.Models;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;

namespace ScarFood.Controllers
{
    public class AutenticacaoController : Controller
    {
        // Caminho do arquivo onde os dados serão salvos permanentemente
        private readonly string _caminhoArquivo = Path.Combine(Directory.GetCurrentDirectory(), "usuarios.json");

        // Função para carregar os usuários do arquivo
        private List<Usuario> CarregarUsuarios()
        {
            // CORREÇÃO: Usando System.IO.File para evitar conflito com o método File() do Controller
            if (!System.IO.File.Exists(_caminhoArquivo)) return new List<Usuario>();
            var json = System.IO.File.ReadAllText(_caminhoArquivo);
            return JsonSerializer.Deserialize<List<Usuario>>(json) ?? new List<Usuario>();
        }

        // Função para salvar a lista no arquivo
        private void SalvarUsuarios(List<Usuario> usuarios)
        {
            var json = JsonSerializer.Serialize(usuarios, new JsonSerializerOptions { WriteIndented = true });
            // CORREÇÃO: Usando System.IO.File aqui também
            System.IO.File.WriteAllText(_caminhoArquivo, json);
        }

        [HttpGet] public IActionResult Login() => View();
        [HttpGet] public IActionResult Registro() => View();

        [HttpPost]
        public IActionResult Registro(Usuario novoUsuario)
        {
            if (ModelState.IsValid)
            {
                var usuarios = CarregarUsuarios();

                if (usuarios.Any(u => u.Email.ToLower() == novoUsuario.Email.ToLower()))
                {
                    ModelState.AddModelError("Email", "Este e-mail já está cadastrado.");
                    return View(novoUsuario);
                }

                // Gera um ID simples
                novoUsuario.Id = usuarios.Count > 0 ? usuarios.Max(u => u.Id) + 1 : 1;

                usuarios.Add(novoUsuario);
                SalvarUsuarios(usuarios); // Grava no arquivo físico

                return RedirectToAction("Login");
            }
            return View(novoUsuario);
        }

        [HttpPost]
        public IActionResult Login(Usuario usuario)
        {
            if (usuario.Email == "admin@scarfood.com" && usuario.Senha == "admin123")
            {
                HttpContext.Session.SetString("UserEmail", "admin@scarfood.com");
                HttpContext.Session.SetString("UserNome", "Administrador");
                return RedirectToAction("Index", "Home");
            }

            var usuarios = CarregarUsuarios();
            var userValido = usuarios.FirstOrDefault(u => u.Email.ToLower() == usuario.Email.ToLower() && u.Senha == usuario.Senha);

            if (userValido != null)
            {
                HttpContext.Session.SetString("UserEmail", userValido.Email);
                HttpContext.Session.SetString("UserNome", userValido.Nome);
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "E-mail ou senha incorretos.");
            return View(usuario);
        }



        [HttpGet]
        public IActionResult Perfil()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");

            
            if (email == "admin@scarfood.com")
            {
                return View(new Usuario
                {
                    Nome = "Administrador",
                    Email = email,
                    Cpf = "000.000.000-00",
                    Telefone = "(00) 00000-0000"
                });
            }

           
            var usuarios = CarregarUsuarios();
            var usuario = usuarios.FirstOrDefault(u => u.Email == email);

            
            if (usuario == null) return RedirectToAction("Sair");

            return View(usuario);
        }

        [HttpPost]
        public IActionResult AtualizarPerfil(Usuario model)
        {
            var emailSessao = HttpContext.Session.GetString("UserEmail");
            var usuarios = CarregarUsuarios();
            var usuario = usuarios.FirstOrDefault(u => u.Email == emailSessao);

            if (usuario != null)
            {
                if (!string.IsNullOrEmpty(model.Senha))
                {
                    if (model.SenhaAtual != usuario.Senha)
                    {
                        ModelState.AddModelError("SenhaAtual", "Senha atual incorreta.");
                        return View("Perfil", usuario);
                    }
                    usuario.Senha = model.Senha;
                }

                usuario.Nome = model.Nome;
                usuario.Cpf = model.Cpf;
                usuario.Telefone = model.Telefone;
                usuario.Cep = model.Cep;
                usuario.Logradouro = model.Logradouro;
                usuario.Numero = model.Numero;
                usuario.Complemento = model.Complemento;

                SalvarUsuarios(usuarios); // Atualiza o arquivo físico
                HttpContext.Session.SetString("UserNome", usuario.Nome);
                ViewBag.Mensagem = "Perfil atualizado com sucesso!";
                return View("Perfil", usuario);
            }
            return RedirectToAction("Login");
        }

        public IActionResult Sair()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}