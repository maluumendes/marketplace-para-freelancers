using System.Security.Claims;
using Freela.Data;
using Freela.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freela.Controllers
{
    // API das vagas, com as duas consultas que as telas de interesse/candidatura precisam.
    //
    //  Verbo  Rota                          Quem        O que faz
    //  GET    /api/projetos                 todos       busca vagas abertas (filtros)  | ?meus=true: vagas da empresa + contagens
    //  GET    /api/projetos/disponiveis     FREELANCER  mesma busca, sem as vagas que ele já avaliou (cards ♡/×)
    //  GET    /api/projetos/{id}            todos       detalhe
    //  POST   /api/projetos                 CLIENTE     publica
    //  PUT    /api/projetos/{id}            CLIENTE     edita (só o dono, só se Aberto)
    //  PATCH  /api/projetos/{id}/encerrar   CLIENTE     encerra (só o dono)
    [ApiController]
    [Route("api/projetos")]
    [Authorize]
    public class ProjetosApiController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ProjetosApiController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Listar(string? busca, string? categoria,
            decimal? minimo, decimal? maximo, string? habilidades, bool meus = false)
        {
            if (!FiltroValido(minimo, maximo))
                return BadRequest(new { mensagem = "Confira os valores do filtro de orçamento." });

            var consulta = ConsultaComDetalhes();

            if (meus)
            {
                if (!User.IsInRole("CLIENTE")) return Forbid();
                int clienteId = ObterUsuarioId();
                consulta = consulta.Where(p => p.ClienteId == clienteId);
            }
            else
            {
                consulta = consulta.Where(p => p.Status == StatusProjeto.Aberto);
            }

            consulta = AplicarFiltros(consulta, busca, categoria, minimo, maximo, habilidades);

            var projetos = await consulta.OrderByDescending(p => p.Id).ToListAsync();
            var resultado = projetos.Select(ProjetoResposta.De).ToList();

            // A empresa vê quantos candidatos estão esperando resposta em cada vaga
            if (meus)
                await PreencherContagensAsync(resultado);

            return Ok(resultado);
        }

        // Cards de "Procurar vagas": vagas abertas em que o freelancer
        // ainda não decidiu nada — sem candidatura ativa (♡) e sem rejeição (×).
        [HttpGet("disponiveis")]
        [Authorize(Roles = "FREELANCER")]
        public async Task<IActionResult> Disponiveis(string? busca, string? categoria,
            decimal? minimo, decimal? maximo, string? habilidades)
        {
            if (!FiltroValido(minimo, maximo))
                return BadRequest(new { mensagem = "Confira os valores do filtro de orçamento." });

            int freelancerId = ObterUsuarioId();

            var consulta = ConsultaComDetalhes()
                .Where(p => p.Status == StatusProjeto.Aberto)
                .Where(p => !_db.Candidaturas.Any(c => c.ProjetoId == p.Id &&
                                                       c.FreelancerId == freelancerId &&
                                                       c.Status != StatusCandidatura.Retirada))
                .Where(p => !_db.Interesses.Any(i => i.ProjetoId == p.Id &&
                                                     i.FreelancerId == freelancerId &&
                                                     i.Origem == OrigemInteresse.Freelancer));

            consulta = AplicarFiltros(consulta, busca, categoria, minimo, maximo, habilidades);

            var projetos = await consulta.OrderByDescending(p => p.Id).ToListAsync();
            return Ok(projetos.Select(ProjetoResposta.De));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Consultar(int id)
        {
            var projeto = await ConsultaComDetalhes().FirstOrDefaultAsync(p => p.Id == id);

            if (projeto == null) return NotFound(new { mensagem = "Projeto não encontrado." });

            return Ok(ProjetoResposta.De(projeto));
        }

        [HttpPost]
        [Authorize(Roles = "CLIENTE")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cadastrar(ProjetoViewModel dados)
        {
            string erro = ValidarProjeto(dados);
            if (erro != "") return BadRequest(new { mensagem = erro });

            int clienteId = ObterUsuarioId();
            var cliente = await _db.Clientes.FindAsync(clienteId);
            if (cliente == null) return Forbid();

            var categoria = await BuscarCategoriaAsync(dados.Categoria);
            if (categoria == null) return BadRequest(new { mensagem = "Escolha uma categoria válida." });

            var projeto = new Projeto();
            projeto.ClienteId = clienteId;
            projeto.Cliente = cliente;
            projeto.Categoria = categoria;
            PreencherProjeto(projeto, dados);
            projeto.Habilidades = await ObterHabilidadesAsync(dados.Habilidades);

            _db.Projetos.Add(projeto);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Consultar), new { id = projeto.Id }, ProjetoResposta.De(projeto));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "CLIENTE")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, ProjetoViewModel dados)
        {
            var projeto = await _db.Projetos
                .Include(p => p.Habilidades)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (projeto == null) return NotFound(new { mensagem = "Projeto não encontrado." });

            // Não basta esconder o botão: o servidor também confere o dono.
            if (projeto.ClienteId != ObterUsuarioId()) return Forbid();
            if (projeto.Status == StatusProjeto.Encerrado)
                return Conflict(new { mensagem = "O projeto já foi encerrado." });

            string erro = ValidarProjeto(dados);
            if (erro != "") return BadRequest(new { mensagem = erro });

            var categoria = await BuscarCategoriaAsync(dados.Categoria);
            if (categoria == null) return BadRequest(new { mensagem = "Escolha uma categoria válida." });

            projeto.Categoria = categoria;
            PreencherProjeto(projeto, dados);

            // Atualiza as habilidades: tira as que saíram e acrescenta só as novas
            // (remover e adicionar a mesma tentaria gravar a ligação duas vezes)
            var novas = await ObterHabilidadesAsync(dados.Habilidades);
            projeto.Habilidades.RemoveAll(atual => !novas.Contains(atual));
            projeto.Habilidades.AddRange(novas.Where(nova => !projeto.Habilidades.Contains(nova)).ToList());

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("{id:int}/encerrar")]
        [Authorize(Roles = "CLIENTE")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Encerrar(int id)
        {
            var projeto = await _db.Projetos.FindAsync(id);
            if (projeto == null) return NotFound(new { mensagem = "Projeto não encontrado." });
            if (projeto.ClienteId != ObterUsuarioId()) return Forbid();

            projeto.Status = StatusProjeto.Encerrado;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ---------- AUXILIARES ----------

        private int ObterUsuarioId()
        {
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id);
            return id;
        }

        // Projeto já com empresa, categoria e habilidades (o que a resposta da API mostra)
        private IQueryable<Projeto> ConsultaComDetalhes()
        {
            return _db.Projetos
                .Include(p => p.Cliente)
                .Include(p => p.Categoria)
                .Include(p => p.Habilidades);
        }

        private static bool FiltroValido(decimal? minimo, decimal? maximo)
        {
            return !(minimo < 0 || maximo < 0 || (minimo.HasValue && maximo.HasValue && minimo > maximo));
        }

        // Os mesmos filtros servem para a busca e para os cards do freelancer
        private static IQueryable<Projeto> AplicarFiltros(IQueryable<Projeto> consulta, string? busca,
            string? categoria, decimal? minimo, decimal? maximo, string? habilidades)
        {
            if (!string.IsNullOrWhiteSpace(busca))
            {
                busca = busca.Trim();
                // Um campo só procura em título, descrição, empresa e habilidades
                consulta = consulta.Where(p => p.Titulo.Contains(busca)
                                            || p.Descricao.Contains(busca)
                                            || p.Cliente.NomeOuRazaoSocial.Contains(busca)
                                            || p.Habilidades.Any(h => h.Nome.Contains(busca)));
            }

            if (!string.IsNullOrWhiteSpace(categoria))
            {
                categoria = categoria.Trim();
                consulta = consulta.Where(p => p.Categoria.Nome == categoria);
            }

            // Procura projetos cuja faixa de orçamento cruza a faixa pesquisada.
            if (minimo.HasValue)
                consulta = consulta.Where(p => p.OrcamentoMaximo >= minimo.Value);

            if (maximo.HasValue)
                consulta = consulta.Where(p => p.OrcamentoMinimo <= maximo.Value);

            if (!string.IsNullOrWhiteSpace(habilidades))
            {
                foreach (string habilidade in habilidades.Split(','))
                {
                    string termo = habilidade.Trim();
                    if (termo != "")
                        consulta = consulta.Where(p => p.Habilidades.Any(h => h.Nome.Contains(termo)));
                }
            }

            return consulta;
        }

        // Quantas candidaturas pendentes/aceitas cada vaga tem (um GROUP BY no banco)
        private async Task PreencherContagensAsync(List<ProjetoResposta> projetos)
        {
            var ids = projetos.Select(p => p.Id).ToList();

            var contagens = await _db.Candidaturas
                .Where(c => ids.Contains(c.ProjetoId))
                .GroupBy(c => c.ProjetoId)
                .Select(g => new
                {
                    ProjetoId = g.Key,
                    Pendentes = g.Count(c => c.Status == StatusCandidatura.Pendente),
                    Matches = g.Count(c => c.Status == StatusCandidatura.Aceita)
                })
                .ToDictionaryAsync(x => x.ProjetoId);

            foreach (var projeto in projetos)
            {
                var achou = contagens.TryGetValue(projeto.Id, out var contagem);
                projeto.CandidaturasPendentes = achou ? contagem!.Pendentes : 0;
                projeto.TotalMatches = achou ? contagem!.Matches : 0;
            }
        }

        private async Task<Categoria?> BuscarCategoriaAsync(string nome)
        {
            nome = nome.Trim();
            return await _db.Categorias.FirstOrDefaultAsync(c => c.Nome == nome);
        }

        // "React, sql, React" -> habilidades da tabela Habilidades (cria as que ainda não existem)
        private async Task<List<Habilidade>> ObterHabilidadesAsync(string texto)
        {
            var nomes = texto.Split(',')
                .Select(n => n.Trim())
                .Where(n => n != "")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // O MySQL compara sem diferenciar maiúsculas: "react" encontra "React"
            var existentes = await _db.Habilidades.Where(h => nomes.Contains(h.Nome)).ToListAsync();

            var resultado = new List<Habilidade>();
            foreach (string nome in nomes)
            {
                var habilidade = existentes.FirstOrDefault(h =>
                    string.Equals(h.Nome, nome, StringComparison.OrdinalIgnoreCase));

                if (habilidade == null)
                {
                    habilidade = new Habilidade { Nome = nome };
                    _db.Habilidades.Add(habilidade);
                    existentes.Add(habilidade);
                }

                resultado.Add(habilidade);
            }

            return resultado;
        }

        private string ValidarProjeto(ProjetoViewModel dados)
        {
            if (dados.Titulo.Trim().Length < 5 || dados.Descricao.Trim().Length < 20)
                return "Use pelo menos 5 caracteres no título e 20 na descrição.";

            if (dados.OrcamentoMaximo < dados.OrcamentoMinimo)
                return "O orçamento máximo não pode ser menor que o mínimo.";

            var habilidades = dados.Habilidades.Split(',').Select(h => h.Trim()).Where(h => h != "").ToList();

            if (habilidades.Count == 0)
                return "Informe pelo menos uma habilidade.";

            if (habilidades.Any(h => h.Length > 50))   // Nome da tabela Habilidades é VARCHAR(50)
                return "Cada habilidade pode ter no máximo 50 caracteres.";

            if (dados.TipoPagamento != "Valor fixo" && dados.TipoPagamento != "Por hora" && dados.TipoPagamento != "Por entrega")
                return "Escolha um tipo de pagamento válido.";

            if (dados.Experiencia != "Iniciante" && dados.Experiencia != "Intermediário" && dados.Experiencia != "Avançado")
                return "Escolha um nível de experiência válido.";

            return "";
        }

        private void PreencherProjeto(Projeto projeto, ProjetoViewModel dados)
        {
            projeto.Titulo = dados.Titulo.Trim();
            projeto.Descricao = dados.Descricao.Trim();
            projeto.OrcamentoMinimo = dados.OrcamentoMinimo;
            projeto.OrcamentoMaximo = dados.OrcamentoMaximo;
            projeto.PrazoDias = dados.PrazoDias;
            projeto.TipoPagamento = dados.TipoPagamento;
            projeto.Experiencia = dados.Experiencia;
        }
    }
}