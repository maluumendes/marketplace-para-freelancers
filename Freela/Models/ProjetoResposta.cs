
namespace Freela.Models
{
    // Informações públicas de uma vaga e da empresa responsável.
    // Não expõe senha, CPF/CNPJ ou e-mail da empresa.
    public class ProjetoResposta
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }

        // Informações da empresa.
        public string Empresa { get; set; } = "";
        public string? EmpresaBio { get; set; }
        public string? EmpresaFotoPerfil { get; set; }

        // Informações da vaga.
        public string Titulo { get; set; } = "";
        public string Descricao { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string Habilidades { get; set; } = "";

        public decimal OrcamentoMinimo { get; set; }
        public decimal OrcamentoMaximo { get; set; }
        public int PrazoDias { get; set; }

        public string TipoPagamento { get; set; } = "";
        public string Experiencia { get; set; } = "";

        public StatusProjeto Status { get; set; }
        public DateTime DataCriacao { get; set; }

        // Informações utilizadas no painel da empresa.
        public int? CandidaturasPendentes { get; set; }
        public int? TotalMatches { get; set; }

        // Converte uma entidade Projeto na resposta enviada pela API.
        public static ProjetoResposta De(Projeto projeto)
        {
            return new ProjetoResposta
            {
                Id = projeto.Id,
                ClienteId = projeto.ClienteId,

                Empresa = projeto.Cliente?.NomeOuRazaoSocial ?? "",

                // Novos dados públicos do perfil da empresa.
                EmpresaBio = projeto.Cliente?.Bio,
                EmpresaFotoPerfil = projeto.Cliente?.FotoPerfil,

                Titulo = projeto.Titulo,
                Descricao = projeto.Descricao,

                Categoria = projeto.Categoria?.Nome ?? "",

                Habilidades = string.Join(
                    ", ",
                    projeto.Habilidades
                        .Select(h => h.Nome)
                        .OrderBy(n => n)
                ),

                OrcamentoMinimo = projeto.OrcamentoMinimo,
                OrcamentoMaximo = projeto.OrcamentoMaximo,
                PrazoDias = projeto.PrazoDias,

                TipoPagamento = projeto.TipoPagamento,
                Experiencia = projeto.Experiencia,

                Status = projeto.Status,
                DataCriacao = projeto.DataCriacao
            };
        }
    }
}
