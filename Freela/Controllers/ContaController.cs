using System.Security.Claims;
using Freela.Data;
using Freela.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freela.Controllers
{
    public class ContaController : Controller
    {
        private readonly AppDbContext _db;

        public ContaController(AppDbContext db)
        {
            _db = db;
        }

        // ---------- LOGIN ----------

        [HttpGet]
        public IActionResult Login()
        {
            // Quem já está logado não precisa ver a tela de login
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim().ToLowerInvariant();

            // Busca o usuário e já traz o perfil (Cliente ou Freelancer) para pegar o nome
            var usuario = await _db.Usuarios
                .Include(u => u.Cliente)
                .Include(u => u.Freelancer)
                .FirstOrDefaultAsync(u => u.Email == email);

            // A mesma mensagem para "e-mail não existe" e "senha errada":
            // assim ninguém descobre quais e-mails estão cadastrados.
            if (usuario == null || !BCrypt.Net.BCrypt.Verify(model.Senha, usuario.Senha))
            {
                ModelState.AddModelError("", "E-mail ou senha incorretos.");
                return View(model);
            }

            await EntrarAsync(usuario);
            return RedirectToAction("Index", "Home");
        }

        // ---------- CADASTRO ----------

        [HttpGet]
        public IActionResult Cadastro()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new CadastroViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cadastro(CadastroViewModel model)
        {
            // 1) Regras simples dos campos (as anotações do CadastroViewModel)
            if (!ModelState.IsValid)
                return View(model);

            // 2) Regras que dependem do banco ou do tipo de conta
            if (model.TipoUsuario != "CLIENTE" && model.TipoUsuario != "FREELANCER")
                ModelState.AddModelError(nameof(model.TipoUsuario), "Escolha o tipo de conta.");

            // Guarda só os números do CPF/CNPJ
            var documento = new string(model.Documento.Where(char.IsDigit).ToArray());

            if (model.TipoUsuario == "FREELANCER" && documento.Length != 11)
                ModelState.AddModelError(nameof(model.Documento), "Informe um CPF com 11 números.");

            if (model.TipoUsuario == "CLIENTE" && documento.Length != 11 && documento.Length != 14)
                ModelState.AddModelError(nameof(model.Documento), "Informe um CPF (11 números) ou CNPJ (14 números).");

            var email = model.Email.Trim().ToLowerInvariant();

            if (await _db.Usuarios.AnyAsync(u => u.Email == email))
                ModelState.AddModelError(nameof(model.Email), "Este e-mail já está cadastrado.");

            if (ModelState.IsValid)
            {
                bool documentoJaExiste = model.TipoUsuario == "CLIENTE"
                    ? await _db.Clientes.AnyAsync(c => c.CpfOuCnpj == documento)
                    : await _db.Freelancers.AnyAsync(f => f.Cpf == documento);

                if (documentoJaExiste)
                    ModelState.AddModelError(nameof(model.Documento), "Este documento já está cadastrado.");
            }

            if (!ModelState.IsValid)
                return View(model);

            // 3) Cria o usuário. A senha é guardada como HASH, nunca em texto puro.
            var usuario = new Usuario
            {
                Email = email,
                Senha = BCrypt.Net.BCrypt.HashPassword(model.Senha),
                TipoUsuario = model.TipoUsuario
            };

            // 4) Cria o perfil da tabela correspondente ao tipo de conta.
            //    O EF Core grava o Usuario primeiro e copia o Id dele para o perfil.
            if (model.TipoUsuario == "CLIENTE")
            {
                usuario.Cliente = new Cliente
                {
                    NomeOuRazaoSocial = model.Nome.Trim(),
                    CpfOuCnpj = documento,
                    Telefone = model.Telefone
                };
            }
            else
            {
                usuario.Freelancer = new Freelancer
                {
                    NomeCompleto = model.Nome.Trim(),
                    Cpf = documento,
                    Telefone = model.Telefone
                };
            }

            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();

            // 5) Já deixa o usuário logado
            await EntrarAsync(usuario);
            return RedirectToAction("Index", "Home");
        }

        // ---------- SAIR ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sair()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // ---------- ACESSO NEGADO ----------
        // Aparece quando o usuário está logado, mas tenta abrir uma página do outro tipo de conta

        public IActionResult AcessoNegado()
        {
            return View();
        }

        // ---------- AUXILIAR ----------
        // Cria o cookie de login. Os "claims" são os dados que ficam guardados nele.

        private async Task EntrarAsync(Usuario usuario)
        {
            var nomeCompleto = usuario.Cliente?.NomeOuRazaoSocial
                            ?? usuario.Freelancer?.NomeCompleto
                            ?? usuario.Email;

            var primeiroNome = nomeCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
                            ?? nomeCompleto;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, primeiroNome),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.TipoUsuario)   // "CLIENTE" ou "FREELANCER"
            };

            var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identidade));
        }
    }
}
