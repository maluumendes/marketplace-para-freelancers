namespace Freela.Models
{
    // Situação da candidatura. No banco é gravada como TEXTO em MAIÚSCULAS
    // ("PENDENTE", "ACEITA"...), no mesmo padrão do TipoUsuario ("CLIENTE"/"FREELANCER").
    // A conversão é feita no AppDbContext.
    public enum StatusCandidatura
    {
        Pendente,   // enviada, aguardando a empresa
        Aceita,     // a empresa aceitou -> virou MATCH
        Rejeitada,  // a empresa recusou
        Retirada    // o próprio freelancer desistiu antes da resposta
    }

    // A proposta formal do freelancer para uma vaga (mensagem, valor e prazo).
    public class Candidatura
    {
        public int Id { get; set; }

        public int ProjetoId { get; set; }
        public int FreelancerId { get; set; }            // é o mesmo Id do Usuario

        public string MensagemApresentacao { get; set; } = "";
        public decimal ValorProposto { get; set; }
        public int PrazoDias { get; set; }

        public StatusCandidatura Status { get; set; } = StatusCandidatura.Pendente;
        public DateTime DataCandidatura { get; set; }
        public DateTime? DataResposta { get; set; }      // quando a empresa aceitou/recusou

        public Projeto? Projeto { get; set; }
        public Freelancer? Freelancer { get; set; }
    }
}