
using Freela.Data;
using Freela.Extensions;
using Freela.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freela.Controllers
{
    // API de matches.
    //
    // GET /api/matches
    // Lista os matches do usuário autenticado.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MatchesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public MatchesController(AppDbContext db)
        {
            _db = db;
        }

        // GET: /api/matches
        [HttpGet]
        public async Task<IActionResult> Meus()
        {
            // Identifica o usuário autenticado.
            var usuarioId = User.ObterUsuarioId();

            var ehFreelancer = User.IsInRole("FREELANCER");

            var ehCliente = User.IsInRole("CLIENTE");

            // Impede acesso de contas sem um dos dois perfis.
            if (!ehFreelancer && !ehCliente)
                return Forbid();

            var consulta = _db.Matches.AsQueryable();

            // Cada perfil consulta somente seus próprios matches.
            consulta = ehFreelancer
                ? consulta.Where(m => m.FreelancerId == usuarioId)
                : consulta.Where(m => m.Projeto!.ClienteId == usuarioId);

            // Monta a resposta diretamente na consulta do banco.
            var matches = await consulta
                .OrderByDescending(m => m.DataMatch)
                .Select(m => new MatchResposta
                {
                    Id = m.Id,

                    DataMatch = m.DataMatch,

                    ProjetoId = m.ProjetoId,

                    TituloProjeto = m.Projeto!.Titulo,

                    // Dados públicos da empresa.
                    Empresa = m.Projeto.Cliente!.NomeOuRazaoSocial,

                    FotoEmpresa = m.Projeto.Cliente.FotoPerfil,

                    BioEmpresa = m.Projeto.Cliente.Bio,

                    // Dados públicos do freelancer.
                    FreelancerId = m.FreelancerId,

                    NomeFreelancer = m.Freelancer!.NomeCompleto,

                    FotoFreelancer = m.Freelancer.FotoPerfil,

                    BioFreelancer = m.Freelancer.Bio,

                    ValorProposto = m.Candidatura!.ValorProposto,

                    PrazoDias = m.Candidatura.PrazoDias,

                    // O freelancer vê o contato da empresa.
                    // A empresa vê o contato do freelancer.
                    EmailContato = ehFreelancer
                        ? m.Projeto.Cliente!.Usuario!.Email
                        : m.Freelancer.Usuario!.Email,

                    TelefoneContato = ehFreelancer
                        ? m.Projeto.Cliente!.Telefone
                        : m.Freelancer.Telefone
                })
                .ToListAsync();

            return Ok(matches);
        }
    }
}
