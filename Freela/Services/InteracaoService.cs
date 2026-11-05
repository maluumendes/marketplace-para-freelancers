using Freela.Data;
using Freela.Models;
using Microsoft.EntityFrameworkCore;

namespace Freela.Services
{
    // REGRA DE NEGÓCIO das interações
    //
    // Fica aqui, e não no controller, porque é usada por mais de um controller
    // (CandidaturasController e InteressesController). Assim a regra do MATCH
    // existe em um lugar só.
    //
    // modelo de match: INTERESSE MÚTUO (estilo Tinder).
    //   - O freelancer demonstra interesse ENVIANDO A CANDIDATURA (♡ no card da vaga).
    //   - A empresa demonstra interesse ACEITANDO o candidato (♡ no card do candidato).
    //   - Os dois interesses ficam registrados na tabela Interesses.
    //   - Existindo os dois lados -> o sistema cria o Match.
    // Na tela atual a empresa só decide sobre quem já se candidatou, então na
    // prática o match acontece no "aceitar". Mas a regra já aceita a ordem
    // inversa (empresa demonstra interesse primeiro, via POST /api/interesses):
    // quando o freelancer se candidatar, o match sai na hora.
    //
    // IMPORTANTE: os métodos NÃO chamam SaveChangesAsync. Quem chama (o controller)
    // salva tudo de uma vez no final — uma única transação: ou grava tudo, ou nada.
    public class InteracaoService
    {
        private readonly AppDbContext _db;

        public InteracaoService(AppDbContext db)
        {
            _db = db;
        }

        // Cria ou atualiza a decisão de UM lado sobre o par (vaga, freelancer)
        public async Task RegistrarInteresseAsync(int projetoId, int freelancerId,
                                                  OrigemInteresse origem, TipoInteresse tipo)
        {
            var interesse = await BuscarInteresseAsync(projetoId, freelancerId, origem);

            if (interesse == null)
            {
                interesse = new Interesse
                {
                    ProjetoId = projetoId,
                    FreelancerId = freelancerId,
                    Origem = origem
                };
                _db.Interesses.Add(interesse);
            }

            interesse.Tipo = tipo;
            interesse.DataRegistro = DateTime.Now;
        }

        // Apaga a decisão de um lado (ex.: freelancer retirou a candidatura)
        public async Task RemoverInteresseAsync(int projetoId, int freelancerId, OrigemInteresse origem)
        {
            var interesse = await BuscarInteresseAsync(projetoId, freelancerId, origem);

            if (interesse != null)
                _db.Interesses.Remove(interesse);
        }

        // A empresa aceita (♡) ou recusa (×) uma candidatura PENDENTE.
        // Devolve o Match criado, ou null quando foi recusa.
        public async Task<Match?> DecidirCandidaturaAsync(Candidatura candidatura, bool aceitar)
        {
            if (candidatura.Status != StatusCandidatura.Pendente)
                throw new InvalidOperationException("Esta candidatura já foi respondida.");

            if (!aceitar)
            {
                await RegistrarInteresseAsync(candidatura.ProjetoId, candidatura.FreelancerId,
                                              OrigemInteresse.Cliente, TipoInteresse.Rejeicao);

                candidatura.Status = StatusCandidatura.Rejeitada;
                candidatura.DataResposta = DateTime.Now;
                return null;
            }

            await RegistrarInteresseAsync(candidatura.ProjetoId, candidatura.FreelancerId,
                                          OrigemInteresse.Cliente, TipoInteresse.Interesse);

            return await VerificarMatchAsync(candidatura)
                ?? throw new InvalidOperationException(
                    "O interesse do freelancer não foi encontrado; o match não pôde ser registrado.");
        }

        // O CORAÇÃO DA REGRA: há interesse dos dois lados? Então é MATCH.
        // Devolve o Match novo, ou null se ainda falta um dos lados.
        public async Task<Match?> VerificarMatchAsync(Candidatura candidatura)
        {
            // Candidatura recusada ou retirada não vira match
            if (candidatura.Status == StatusCandidatura.Rejeitada ||
                candidatura.Status == StatusCandidatura.Retirada)
                return null;

            var doFreelancer = await BuscarInteresseAsync(candidatura.ProjetoId, candidatura.FreelancerId,
                                                          OrigemInteresse.Freelancer);
            var doCliente = await BuscarInteresseAsync(candidatura.ProjetoId, candidatura.FreelancerId,
                                                       OrigemInteresse.Cliente);

            var interesseMutuo = doFreelancer?.Tipo == TipoInteresse.Interesse
                              && doCliente?.Tipo == TipoInteresse.Interesse;

            if (!interesseMutuo)
                return null;

            // Já existe? Não duplica.
            var jaExiste = _db.Matches.Local.Any(m => m.ProjetoId == candidatura.ProjetoId &&
                                                      m.FreelancerId == candidatura.FreelancerId)
                        || await _db.Matches.AnyAsync(m => m.ProjetoId == candidatura.ProjetoId &&
                                                           m.FreelancerId == candidatura.FreelancerId);
            if (jaExiste)
                return null;

            var agora = DateTime.Now;

            candidatura.Status = StatusCandidatura.Aceita;
            candidatura.DataResposta = agora;

            var match = new Match
            {
                ProjetoId = candidatura.ProjetoId,
                FreelancerId = candidatura.FreelancerId,
                Candidatura = candidatura,   // pela navegação: funciona mesmo se a candidatura for nova (Id ainda 0)
                DataMatch = agora
            };

            _db.Matches.Add(match);
            return match;
        }

        // Procura primeiro no que já está carregado na memória (Local) —
        // inclusive o que acabou de ser adicionado e ainda não foi salvo —
        // e só depois no banco.
        private async Task<Interesse?> BuscarInteresseAsync(int projetoId, int freelancerId, OrigemInteresse origem)
        {
            var local = _db.Interesses.Local.FirstOrDefault(i =>
                i.ProjetoId == projetoId && i.FreelancerId == freelancerId && i.Origem == origem &&
                _db.Entry(i).State != EntityState.Deleted);

            if (local != null) return local;

            var doBanco = await _db.Interesses.FirstOrDefaultAsync(i =>
                i.ProjetoId == projetoId && i.FreelancerId == freelancerId && i.Origem == origem);

            // Se foi marcado para exclusão nesta mesma operação, conta como inexistente
            return doBanco != null && _db.Entry(doBanco).State == EntityState.Deleted ? null : doBanco;
        }
    }
}