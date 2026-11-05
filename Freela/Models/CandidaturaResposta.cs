
namespace Freela.Models
{
    // Respostas utilizadas pelas APIs de candidaturas.

    // =====================================
    // VISÃO DO FREELANCER
    // =====================================

    public class MinhaCandidaturaResposta
    {
        public int Id { get; set; }

        public ProjetoResposta Projeto { get; set; } = new();

        public string MensagemApresentacao { get; set; } = "";

        public decimal ValorProposto { get; set; }

        public int PrazoDias { get; set; }

        public StatusCandidatura Status { get; set; }

        public DateTime DataCandidatura { get; set; }

        public DateTime? DataResposta { get; set; }

        // Contato da empresa somente depois do Match.
        public string? EmailEmpresa { get; set; }
        public string? TelefoneEmpresa { get; set; }

        public static MinhaCandidaturaResposta De(Candidatura c)
        {
            var match = c.Status == StatusCandidatura.Aceita;

            return new MinhaCandidaturaResposta
            {
                Id = c.Id,

                Projeto = ProjetoResposta.De(c.Projeto!),

                MensagemApresentacao = c.MensagemApresentacao,

                ValorProposto = c.ValorProposto,

                PrazoDias = c.PrazoDias,

                Status = c.Status,

                DataCandidatura = c.DataCandidatura,

                DataResposta = c.DataResposta,

                EmailEmpresa = match
                    ? c.Projeto?.Cliente?.Usuario?.Email
                    : null,

                TelefoneEmpresa = match
                    ? c.Projeto?.Cliente?.Telefone
                    : null
            };
        }
    }

    // =====================================
    // VISÃO DA EMPRESA
    // Candidatos de uma vaga
    // =====================================

    public class CandidatoResposta
    {
        public int CandidaturaId { get; set; }

        public int ProjetoId { get; set; }

        public int FreelancerId { get; set; }

        public string Nome { get; set; } = "";

        public string Iniciais { get; set; } = "";

        // Biografia pública do freelancer.
        public string? Bio { get; set; }

        // Novo campo: foto de perfil.
        public string? FotoPerfil { get; set; }

        public string? LinkPortfolio { get; set; }

        public string MensagemApresentacao { get; set; } = "";

        public decimal ValorProposto { get; set; }

        public int PrazoDias { get; set; }

        public StatusCandidatura Status { get; set; }

        public DateTime DataCandidatura { get; set; }

        // Contatos somente depois do Match.
        public string? Email { get; set; }

        public string? Telefone { get; set; }

        // Converte uma candidatura em uma resposta para a empresa.
        public static CandidatoResposta De(Candidatura c)
        {
            var nome = c.Freelancer?.NomeCompleto ?? "Freelancer";

            var match = c.Status == StatusCandidatura.Aceita;

            return new CandidatoResposta
            {
                CandidaturaId = c.Id,

                ProjetoId = c.ProjetoId,

                FreelancerId = c.FreelancerId,

                Nome = nome,

                Iniciais = GerarIniciais(nome),

                Bio = c.Freelancer?.Bio,

                // Nova informação enviada pela API.
                FotoPerfil = c.Freelancer?.FotoPerfil,

                LinkPortfolio = c.Freelancer?.LinkPortfolio,

                MensagemApresentacao = c.MensagemApresentacao,

                ValorProposto = c.ValorProposto,

                PrazoDias = c.PrazoDias,

                Status = c.Status,

                DataCandidatura = c.DataCandidatura,

                Email = match
                    ? c.Freelancer?.Usuario?.Email
                    : null,

                Telefone = match
                    ? c.Freelancer?.Telefone
                    : null
            };
        }

        // "João da Silva" -> "JS".
        public static string GerarIniciais(string nome)
        {
            var partes = nome.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries
            )
            .Where(p => p.Length > 2)
            .ToList();

            if (partes.Count == 0)
                return nome.Length > 0
                    ? nome[..1].ToUpper()
                    : "?";

            if (partes.Count == 1)
                return partes[0][..1].ToUpper();

            return (
                partes[0][..1] +
                partes[^1][..1]
            ).ToUpper();
        }
    }
}
