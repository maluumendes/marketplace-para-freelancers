using Freela.Data;
using Freela.Extensions;
using Freela.Models;
using Freela.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freela.Controllers
{
    // API das candidaturas (Integrante 3)
    //
    //  Verbo   Rota                                   Quem         O que faz
    //  POST    /api/candidaturas                      FREELANCER   envia candidatura (= interesse do freelancer)
    //  GET     /api/candidaturas/minhas               FREELANCER   lista as minhas, com status
    //  DELETE  /api/candidaturas/{id}                 FREELANCER   retira a candidatura (só se Pendente)
    //  GET     /api/candidaturas/{id}                 os dois      detalhe (só quem participa)
    //  GET     /api/candidaturas/projeto/{projetoId}  CLIENTE      candidatos de uma vaga minha
    //  PUT     /api/candidaturas/{id}/decisao         CLIENTE      aceita (MATCH) ou recusa
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]   // todas as rotas exigem login
    public class CandidaturasController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly InteracaoService _interacoes;

        public CandidaturasController(AppDbContext db, InteracaoService interacoes)
        {
            _db = db;
            _interacoes = interacoes;
        }

        // ---------- FREELANCER ----------

        // POST: /api/candidaturas
        [HttpPost]
        [Authorize(Roles = "FREELANCER")]
        [ValidateAntiForgeryToken]   // mesmo padrão das rotas do Integrante 2 (token no header X-CSRF-TOKEN)
        public async Task<IActionResult> Criar(CandidaturaViewModel dto)
        {
            // [ApiController] já devolve 400 se as anotações do ViewModel não forem atendidas
            var freelancerId = User.ObterUsuarioId();

            var projeto = await _db.Projetos.FindAsync(dto.ProjetoId);
            if (projeto == null)
                return NotFound(new { mensagem = "Vaga não encontrada." });

            // Vaga encerrada pela empresa (Integrante 2) não recebe candidaturas
            if (projeto.Status == StatusProjeto.Encerrado)
                return Conflict(new { mensagem = "Esta vaga foi encerrada pela empresa." });

            if (!await _db.Freelancers.AnyAsync(f => f.Id == freelancerId))
                return BadRequest(new { mensagem = "Seu perfil de freelancer não foi encontrado." });

            // Impede candidatura duplicada
            var candidatura = await _db.Candidaturas
                .FirstOrDefaultAsync(c => c.ProjetoId == dto.ProjetoId && c.FreelancerId == freelancerId);

            if (candidatura != null && candidatura.Status != StatusCandidatura.Retirada)
                return Conflict(new { mensagem = "Você já se candidatou a esta vaga." });

            if (candidatura == null)
            {
                candidatura = new Candidatura
                {
                    ProjetoId = dto.ProjetoId,
                    FreelancerId = freelancerId
                };
                _db.Candidaturas.Add(candidatura);
            }

            // Nova candidatura, ou reenvio de uma que tinha sido retirada:
            // reaproveita a mesma linha (o índice único não deixa criar outra)
            candidatura.MensagemApresentacao = dto.MensagemApresentacao.Trim();
            candidatura.ValorProposto = dto.ValorProposto;
            candidatura.PrazoDias = dto.PrazoDias;
            candidatura.Status = StatusCandidatura.Pendente;
            candidatura.DataCandidatura = DateTime.Now;
            candidatura.DataResposta = null;

            // A candidatura É o interesse do freelancer na vaga
            await _interacoes.RegistrarInteresseAsync(dto.ProjetoId, freelancerId,
                                                      OrigemInteresse.Freelancer, TipoInteresse.Interesse);

            // Se a empresa já tinha demonstrado interesse antes, o match sai agora
            var match = await _interacoes.VerificarMatchAsync(candidatura);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Dois envios ao mesmo tempo: o índice único do banco barra o segundo
                return Conflict(new { mensagem = "Você já se candidatou a esta vaga." });
            }

            return CreatedAtAction(nameof(Obter), new { id = candidatura.Id }, new
            {
                mensagem = match != null ? "Candidatura enviada e já deu MATCH!" : "Candidatura enviada!",
                candidaturaId = candidatura.Id,
                status = candidatura.Status,
                match = match != null
            });
        }

        // GET: /api/candidaturas/minhas
        [HttpGet("minhas")]
        [Authorize(Roles = "FREELANCER")]
        public async Task<IActionResult> Minhas()
        {
            var freelancerId = User.ObterUsuarioId();

            var candidaturas = await _db.Candidaturas
                .Include(c => c.Projeto!).ThenInclude(p => p.Cliente!).ThenInclude(cl => cl.Usuario)
                .Include(c => c.Projeto!).ThenInclude(p => p.Categoria)
                .Include(c => c.Projeto!).ThenInclude(p => p.Habilidades)
                .Where(c => c.FreelancerId == freelancerId && c.Status != StatusCandidatura.Retirada)
                .OrderByDescending(c => c.DataCandidatura)
                .ToListAsync();

            return Ok(candidaturas.Select(MinhaCandidaturaResposta.De));
        }

        // DELETE: /api/candidaturas/{id}
        // Retirar = desistir. Não apaga a linha: muda o status para "Retirada"
        // (fica o histórico e o freelancer pode se candidatar de novo depois).
        [HttpDelete("{id}")]
        [Authorize(Roles = "FREELANCER")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Retirar(int id)
        {
            var freelancerId = User.ObterUsuarioId();

            var candidatura = await _db.Candidaturas.FindAsync(id);
            if (candidatura == null)
                return NotFound(new { mensagem = "Candidatura não encontrada." });

            if (candidatura.FreelancerId != freelancerId)
                return Forbid();   // a candidatura é de outra pessoa

            if (candidatura.Status != StatusCandidatura.Pendente)
                return BadRequest(new { mensagem = "Só é possível retirar candidaturas que ainda estão pendentes." });

            candidatura.Status = StatusCandidatura.Retirada;
            candidatura.DataResposta = DateTime.Now;

            // Sem candidatura, não há mais interesse do freelancer: a vaga volta para a lista dele
            await _interacoes.RemoverInteresseAsync(candidatura.ProjetoId, freelancerId, OrigemInteresse.Freelancer);

            await _db.SaveChangesAsync();

            return Ok(new { mensagem = "Candidatura retirada." });
        }

        // ---------- OS DOIS PERFIS ----------

        // GET: /api/candidaturas/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Obter(int id)
        {
            var usuarioId = User.ObterUsuarioId();

            var candidatura = await _db.Candidaturas
                .Include(c => c.Projeto!).ThenInclude(p => p.Cliente!).ThenInclude(cl => cl.Usuario)
                .Include(c => c.Projeto!).ThenInclude(p => p.Categoria)
                .Include(c => c.Projeto!).ThenInclude(p => p.Habilidades)
                .Include(c => c.Freelancer!).ThenInclude(f => f.Usuario)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (candidatura == null)
                return NotFound(new { mensagem = "Candidatura não encontrada." });

            // Só o freelancer dono ou a empresa dona da vaga podem ver
            if (candidatura.FreelancerId == usuarioId)
                return Ok(MinhaCandidaturaResposta.De(candidatura));

            if (candidatura.Projeto!.ClienteId == usuarioId)
                return Ok(CandidatoResposta.De(candidatura));

            return Forbid();
        }

        // ---------- CLIENTE (empresa) ----------

        // GET: /api/candidaturas/projeto/{projetoId}?status=Pendente
        [HttpGet("projeto/{projetoId}")]
        [Authorize(Roles = "CLIENTE")]
        public async Task<IActionResult> DoProjeto(int projetoId, [FromQuery] StatusCandidatura? status)
        {
            var clienteId = User.ObterUsuarioId();

            var projeto = await _db.Projetos.FindAsync(projetoId);
            if (projeto == null)
                return NotFound(new { mensagem = "Vaga não encontrada." });

            if (projeto.ClienteId != clienteId)
                return Forbid();   // só a empresa dona da vaga vê os candidatos

            var consulta = _db.Candidaturas
                .Include(c => c.Freelancer!).ThenInclude(f => f.Usuario)
                .Where(c => c.ProjetoId == projetoId && c.Status != StatusCandidatura.Retirada);

            if (status.HasValue)
                consulta = consulta.Where(c => c.Status == status.Value);

            var candidaturas = await consulta
                .OrderBy(c => c.DataCandidatura)   // quem chegou primeiro aparece primeiro
                .ToListAsync();

            return Ok(candidaturas.Select(CandidatoResposta.De));
        }

        // PUT: /api/candidaturas/{id}/decisao   body: { "aceitar": true }
        [HttpPut("{id}/decisao")]
        [Authorize(Roles = "CLIENTE")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decidir(int id, DecisaoCandidaturaViewModel dto)
        {
            var clienteId = User.ObterUsuarioId();

            var candidatura = await _db.Candidaturas
                .Include(c => c.Projeto)
                .Include(c => c.Freelancer)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (candidatura == null)
                return NotFound(new { mensagem = "Candidatura não encontrada." });

            if (candidatura.Projeto!.ClienteId != clienteId)
                return Forbid();   // só a empresa dona da vaga decide

            if (candidatura.Status != StatusCandidatura.Pendente)
                return Conflict(new { mensagem = "Esta candidatura já foi respondida." });

            Match? match;
            try
            {
                match = await _interacoes.DecidirCandidaturaAsync(candidatura, dto.Aceitar!.Value);
            }
            catch (InvalidOperationException erro)
            {
                return Conflict(new { mensagem = erro.Message });
            }

            await _db.SaveChangesAsync();

            var nome = candidatura.Freelancer?.NomeCompleto ?? "Candidato";

            return Ok(new
            {
                mensagem = match != null ? $"Match com {nome}!" : $"{nome} foi recusado(a).",
                status = candidatura.Status,
                match = match != null,
                matchId = match?.Id
            });
        }
    }
}