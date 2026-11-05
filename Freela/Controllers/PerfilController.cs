using Freela.Data;
using Freela.Extensions;
using Freela.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freela.Controllers
{
    [Authorize]
    public class PerfilController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _ambiente;

        private const long TamanhoMaximoFoto = 3 * 1024 * 1024;

        public PerfilController(
            AppDbContext db,
            IWebHostEnvironment ambiente)
        {
            _db = db;
            _ambiente = ambiente;
        }

        // =====================================
        // ABRIR A TELA DO PERFIL
        // GET: /Perfil
        // =====================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int usuarioId = User.ObterUsuarioId();

            // Perfil da empresa.
            if (User.IsInRole("CLIENTE"))
            {
                var cliente = await _db.Clientes
                    .Include(c => c.Usuario)
                    .FirstOrDefaultAsync(c => c.Id == usuarioId);

                if (cliente == null)
                    return NotFound("Perfil da empresa não encontrado.");

                var model = new PerfilViewModel
                {
                    TipoUsuario = "CLIENTE",
                    Nome = cliente.NomeOuRazaoSocial,
                    Email = cliente.Usuario?.Email ?? "",
                    Bio = cliente.Bio,
                    Telefone = cliente.Telefone,
                    FotoPerfilAtual = cliente.FotoPerfil
                };

                return View(model);
            }

            // Perfil do freelancer.
            if (User.IsInRole("FREELANCER"))
            {
                var freelancer = await _db.Freelancers
                    .Include(f => f.Usuario)
                    .FirstOrDefaultAsync(f => f.Id == usuarioId);

                if (freelancer == null)
                    return NotFound("Perfil do freelancer não encontrado.");

                var model = new PerfilViewModel
                {
                    TipoUsuario = "FREELANCER",
                    Nome = freelancer.NomeCompleto,
                    Email = freelancer.Usuario?.Email ?? "",
                    Bio = freelancer.Bio,
                    Telefone = freelancer.Telefone,
                    LinkPortfolio = freelancer.LinkPortfolio,
                    FotoPerfilAtual = freelancer.FotoPerfil
                };

                return View(model);
            }

            return Forbid();
        }

        // =====================================
        // SALVAR ALTERAÇÕES DO PERFIL
        // POST: /Perfil/Salvar
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(PerfilViewModel model)
        {
            int usuarioId = User.ObterUsuarioId();

            Cliente? cliente = null;
            Freelancer? freelancer = null;

            // Busca o perfil correspondente à conta autenticada.
            if (User.IsInRole("CLIENTE"))
            {
                cliente = await _db.Clientes
                    .Include(c => c.Usuario)
                    .FirstOrDefaultAsync(c => c.Id == usuarioId);
            }
            else if (User.IsInRole("FREELANCER"))
            {
                freelancer = await _db.Freelancers
                    .Include(f => f.Usuario)
                    .FirstOrDefaultAsync(f => f.Id == usuarioId);
            }
            else
            {
                return Forbid();
            }

            if (cliente == null && freelancer == null)
                return NotFound("Perfil não encontrado.");

            // Recupera os dados que aparecem na parte superior da tela.
            if (cliente != null)
            {
                model.TipoUsuario = "CLIENTE";
                model.Nome = cliente.NomeOuRazaoSocial;
                model.Email = cliente.Usuario?.Email ?? "";
                model.FotoPerfilAtual = cliente.FotoPerfil;
            }
            else if (freelancer != null)
            {
                model.TipoUsuario = "FREELANCER";
                model.Nome = freelancer.NomeCompleto;
                model.Email = freelancer.Usuario?.Email ?? "";
                model.FotoPerfilAtual = freelancer.FotoPerfil;
            }

            if (!ModelState.IsValid)
                return View("Index", model);

            string? novaFoto = null;

            // Se o usuário escolheu uma foto, valida e salva o arquivo.
            if (model.Foto != null && model.Foto.Length > 0)
            {
                if (model.Foto.Length > TamanhoMaximoFoto)
                {
                    ModelState.AddModelError(
                        nameof(model.Foto),
                        "A foto deve ter no máximo 3 MB."
                    );

                    return View("Index", model);
                }

                string extensao = Path
                    .GetExtension(model.Foto.FileName)
                    .ToLowerInvariant();

                if (extensao != ".jpg" &&
                    extensao != ".jpeg" &&
                    extensao != ".png" &&
                    extensao != ".webp")
                {
                    ModelState.AddModelError(
                        nameof(model.Foto),
                        "Escolha uma imagem JPG, JPEG, PNG ou WEBP."
                    );

                    return View("Index", model);
                }

                // Confere o conteúdo real do arquivo.
                if (!await ImagemValidaAsync(model.Foto, extensao))
                {
                    ModelState.AddModelError(
                        nameof(model.Foto),
                        "O arquivo selecionado não é uma imagem válida."
                    );

                    return View("Index", model);
                }

                novaFoto = await SalvarFotoAsync(
                    model.Foto,
                    extensao
                );
            }

            // Atualiza os dados da empresa.
            if (cliente != null)
            {
                cliente.Bio = LimparTexto(model.Bio);
                cliente.Telefone = LimparTexto(model.Telefone);

                if (novaFoto != null)
                    cliente.FotoPerfil = novaFoto;
            }

            // Atualiza os dados do freelancer.
            if (freelancer != null)
            {
                freelancer.Bio = LimparTexto(model.Bio);
                freelancer.Telefone = LimparTexto(model.Telefone);
                freelancer.LinkPortfolio = LimparTexto(model.LinkPortfolio);

                if (novaFoto != null)
                    freelancer.FotoPerfil = novaFoto;
            }

            await _db.SaveChangesAsync();

            TempData["Sucesso"] = "Perfil atualizado com sucesso!";

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // SALVAR A FOTO NA PASTA WWWROOT
        // =====================================

        private async Task<string> SalvarFotoAsync(
            IFormFile foto,
            string extensao)
        {
            string pasta = Path.Combine(
                _ambiente.WebRootPath,
                "uploads",
                "perfis"
            );

            Directory.CreateDirectory(pasta);

            // Nome aleatório: evita sobrescrever fotos de outros usuários.
            string nomeArquivo = Guid.NewGuid().ToString("N") + extensao;

            string caminhoCompleto = Path.Combine(
                pasta,
                nomeArquivo
            );

            await using (var arquivo = new FileStream(
                caminhoCompleto,
                FileMode.CreateNew))
            {
                await foto.CopyToAsync(arquivo);
            }

            // Este caminho será salvo no MySQL.
            return "/uploads/perfis/" + nomeArquivo;
        }

        // =====================================
        // CONFERIR O TIPO REAL DA IMAGEM
        // =====================================

        private static async Task<bool> ImagemValidaAsync(
            IFormFile foto,
            string extensao)
        {
            byte[] assinatura = new byte[12];

            await using var stream = foto.OpenReadStream();

            int quantidade = await stream.ReadAsync(
                assinatura.AsMemory(0, assinatura.Length)
            );

            return extensao switch
            {
                ".jpg" or ".jpeg" =>
                    quantidade >= 3 &&
                    assinatura[0] == 0xFF &&
                    assinatura[1] == 0xD8 &&
                    assinatura[2] == 0xFF,

                ".png" =>
                    quantidade >= 8 &&
                    assinatura.Take(8).SequenceEqual(
                        new byte[]
                        {
                            0x89, 0x50, 0x4E, 0x47,
                            0x0D, 0x0A, 0x1A, 0x0A
                        }
                    ),

                ".webp" =>
                    quantidade >= 12 &&
                    System.Text.Encoding.ASCII.GetString(
                        assinatura, 0, 4) == "RIFF" &&
                    System.Text.Encoding.ASCII.GetString(
                        assinatura, 8, 4) == "WEBP",

                _ => false
            };
        }

        // =====================================
        // LIMPAR CAMPOS DE TEXTO
        // =====================================

        private static string? LimparTexto(string? texto)
        {
            return string.IsNullOrWhiteSpace(texto)
                ? null
                : texto.Trim();
        }
    }
}