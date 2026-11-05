using Freela.Data;
using Freela.Extensions;
using Freela.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freela.Controllers
{
    [ApiController]
    [Route("api/painel")]
    [Authorize]
    public class PainelController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PainelController(AppDbContext db)
        {
            _db = db;
        }

        // GET: /api/painel/resumo
        // Retorna o resumo de acordo com o tipo de usuário logado.
        [HttpGet("resumo")]
        public async Task<IActionResult> Resumo()
        {
            // Pega o ID da conta autenticada.
            // Não recebemos o ID pela URL para impedir que um usuário
            // consulte o resumo de outra pessoa.
            int usuarioId = User.ObterUsuarioId();

            // =====================================
            // PAINEL DO FREELANCER
            // =====================================

            if (User.IsInRole("FREELANCER"))
            {
                // Busca somente as candidaturas deste freelancer.
                var candidaturas = _db.Candidaturas
                    .AsNoTracking()
                    .Where(c => c.FreelancerId == usuarioId);

                // Não contamos as retiradas como candidaturas ativas.
                int totalAtivas = await candidaturas
                    .CountAsync(c => c.Status != StatusCandidatura.Retirada);

                int pendentes = await candidaturas
                    .CountAsync(c => c.Status == StatusCandidatura.Pendente);

                int aceitas = await candidaturas
                    .CountAsync(c => c.Status == StatusCandidatura.Aceita);

                int rejeitadas = await candidaturas
                    .CountAsync(c => c.Status == StatusCandidatura.Rejeitada);

                int retiradas = await candidaturas
                    .CountAsync(c => c.Status == StatusCandidatura.Retirada);

                // Busca somente os matches deste freelancer.
                int matches = await _db.Matches
                    .AsNoTracking()
                    .CountAsync(m => m.FreelancerId == usuarioId);

                return Ok(new
                {
                    tipoUsuario = "FREELANCER",
                    candidaturasAtivas = totalAtivas,
                    pendentes,
                    aceitas,
                    rejeitadas,
                    retiradas,
                    matches
                });
            }

            // =====================================
            // PAINEL DA EMPRESA
            // =====================================

            if (User.IsInRole("CLIENTE"))
            {
                // Busca somente os projetos publicados
                // pela empresa que está autenticada.
                var projetos = _db.Projetos
                    .AsNoTracking()
                    .Where(p => p.ClienteId == usuarioId);

                int projetosPublicados = await projetos.CountAsync();

                int projetosAbertos = await projetos
                    .CountAsync(p => p.Status == StatusProjeto.Aberto);

                int projetosEncerrados = await projetos
                    .CountAsync(p => p.Status == StatusProjeto.Encerrado);

                // Busca candidaturas recebidas nos projetos da empresa.
                var candidaturas = _db.Candidaturas
                    .AsNoTracking()
                    .Where(c => c.Projeto!.ClienteId == usuarioId);

                int candidaturasRecebidas = await candidaturas
                    .CountAsync(c => c.Status != StatusCandidatura.Retirada);

                int candidaturasPendentes = await candidaturas
                    .CountAsync(c => c.Status == StatusCandidatura.Pendente);

                int candidaturasAceitas = await candidaturas
                    .CountAsync(c => c.Status == StatusCandidatura.Aceita);

                int candidaturasRejeitadas = await candidaturas
                    .CountAsync(c => c.Status == StatusCandidatura.Rejeitada);

                // Busca somente os matches dos projetos desta empresa.
                int matches = await _db.Matches
                    .AsNoTracking()
                    .CountAsync(m => m.Projeto!.ClienteId == usuarioId);

                return Ok(new
                {
                    tipoUsuario = "CLIENTE",
                    projetosPublicados,
                    projetosAbertos,
                    projetosEncerrados,
                    candidaturasRecebidas,
                    candidaturasPendentes,
                    candidaturasAceitas,
                    candidaturasRejeitadas,
                    matches
                });
            }

            // Se não possuir um dos dois perfis, não permite o acesso.
            return Forbid();
        }
    }
}