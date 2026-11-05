using Freela.Data;
using Freela.Extensions;
using Freela.Models;
using Freela.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freela.Controllers
{
    // API de interesses (o "arrastar o card" que não é candidatura)
    //
    //  Verbo   Rota                             Quem        O que faz
    //  POST    /api/interesses                  os dois     registra interesse/rejeição
    //  DELETE  /api/interesses/descartadas      FREELANCER  "desfaz" as vagas descartadas (voltam para a lista)
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InteressesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly InteracaoService _interacoes;

        public InteressesController(AppDbContext db, InteracaoService interacoes)
        {
            _db = db;
            _interacoes = interacoes;
        }

        // POST: /api/interesses
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(InteresseViewModel dto)
        {
            var projeto = await _db.Projetos.FindAsync(dto.ProjetoId);
            if (projeto == null)
                return NotFound(new { mensagem = "Vaga não encontrada." });

            if (User.IsInRole("FREELANCER"))
                return await RegistrarDoFreelancer(dto);

            if (User.IsInRole("CLIENTE"))
                return await RegistrarDoCliente(dto, projeto);

            return Forbid();
        }

        // Freelancer: aqui só "passa" a vaga (×).
        // O interesse positivo (♡) é feito enviando a candidatura.
        private async Task<IActionResult> RegistrarDoFreelancer(InteresseViewModel dto)
        {
            var freelancerId = User.ObterUsuarioId();

            if (dto.Tipo == TipoInteresse.Interesse)
                return BadRequest(new { mensagem = "Para demonstrar interesse, envie uma candidatura para a vaga." });

            var temCandidaturaAtiva = await _db.Candidaturas.AnyAsync(c =>
                c.ProjetoId == dto.ProjetoId && c.FreelancerId == freelancerId &&
                c.Status != StatusCandidatura.Retirada);

            if (temCandidaturaAtiva)
                return Conflict(new { mensagem = "Você já se candidatou. Para desistir, retire a candidatura." });

            await _interacoes.RegistrarInteresseAsync(dto.ProjetoId, freelancerId,
                                                      OrigemInteresse.Freelancer, TipoInteresse.Rejeicao);
            await _db.SaveChangesAsync();

            return Ok(new { mensagem = "Vaga descartada." });
        }

        // Empresa: interesse ou rejeição sobre um freelancer, em uma vaga dela.
        // - Se ele já se candidatou (Pendente): vale como aceitar/recusar a candidatura.
        // - Se ainda não: o interesse fica guardado e o match sai quando ele se candidatar.
        private async Task<IActionResult> RegistrarDoCliente(InteresseViewModel dto, Projeto projeto)
        {
            var clienteId = User.ObterUsuarioId();

            if (projeto.ClienteId != clienteId)
                return Forbid();

            if (dto.FreelancerId is not int freelancerId)
                return BadRequest(new { mensagem = "Informe o freelancer." });

            if (!await _db.Freelancers.AnyAsync(f => f.Id == freelancerId))
                return NotFound(new { mensagem = "Freelancer não encontrado." });

            var candidatura = await _db.Candidaturas.FirstOrDefaultAsync(c =>
                c.ProjetoId == projeto.Id && c.FreelancerId == freelancerId);

            var aceitar = dto.Tipo == TipoInteresse.Interesse;

            if (candidatura != null && candidatura.Status == StatusCandidatura.Pendente)
            {
                Match? match;
                try
                {
                    match = await _interacoes.DecidirCandidaturaAsync(candidatura, aceitar);
                }
                catch (InvalidOperationException erro)
                {
                    return Conflict(new { mensagem = erro.Message });
                }

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    mensagem = match != null ? "Match registrado!" : "Candidato recusado.",
                    match = match != null
                });
            }

            if (candidatura != null && candidatura.Status != StatusCandidatura.Retirada)
                return Conflict(new { mensagem = "Esta candidatura já foi respondida." });

            await _interacoes.RegistrarInteresseAsync(projeto.Id, freelancerId, OrigemInteresse.Cliente,
                                                      aceitar ? TipoInteresse.Interesse : TipoInteresse.Rejeicao);
            await _db.SaveChangesAsync();

            return Ok(new { mensagem = "Interesse registrado.", match = false });
        }

        // DELETE: /api/interesses/descartadas
        // Botão "Ver vagas descartadas novamente" da tela Procurar vagas
        [HttpDelete("descartadas")]
        [Authorize(Roles = "FREELANCER")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LimparDescartadas()
        {
            var freelancerId = User.ObterUsuarioId();

            var removidas = await _db.Interesses
                .Where(i => i.FreelancerId == freelancerId &&
                            i.Origem == OrigemInteresse.Freelancer &&
                            i.Tipo == TipoInteresse.Rejeicao)
                .ExecuteDeleteAsync();

            return Ok(new { mensagem = $"{removidas} vaga(s) voltaram para a sua lista." });
        }
    }
}