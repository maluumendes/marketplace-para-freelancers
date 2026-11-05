
namespace Freela.Models
{
    // Resposta enviada pela API de matches.
    public class MatchResposta
    {
        public int Id { get; set; }

        public DateTime DataMatch { get; set; }

        public int ProjetoId { get; set; }

        public string TituloProjeto { get; set; } = "";

        // Perfil público da empresa.
        public string Empresa { get; set; } = "";
        public string? FotoEmpresa { get; set; }
        public string? BioEmpresa { get; set; }

        public int FreelancerId { get; set; }

        // Perfil público do freelancer.
        public string NomeFreelancer { get; set; } = "";
        public string? FotoFreelancer { get; set; }
        public string? BioFreelancer { get; set; }

        public decimal ValorProposto { get; set; }

        public int PrazoDias { get; set; }

        // Contato da outra parte somente após o Match.
        public string? EmailContato { get; set; }

        public string? TelefoneContato { get; set; }
    }
}
