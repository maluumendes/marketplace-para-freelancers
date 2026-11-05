namespace Freela.Models
{
    // Nasce quando os dois lados demonstram interesse no mesmo par (vaga, freelancer).
    // Fica em tabela própria para:
    //  - guardar a data exata do match;
    //  - listar "meus matches" (dos dois lados) com uma consulta simples;
    //  - liberar os contatos (e-mail/telefone) só depois do match.
    public class Match
    {
        public int Id { get; set; }

        public int ProjetoId { get; set; }
        public int FreelancerId { get; set; }
        public int CandidaturaId { get; set; }

        public DateTime DataMatch { get; set; }

        public Projeto? Projeto { get; set; }
        public Freelancer? Freelancer { get; set; }
        public Candidatura? Candidatura { get; set; }
    }
}