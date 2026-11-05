namespace Freela.Models
{
    // Quem tomou a decisão
    public enum OrigemInteresse
    {
        Freelancer,   // o freelancer sobre a vaga
        Cliente       // a empresa sobre o freelancer, naquela vaga
    }

    // O que foi decidido (o "arrastar para a direita/esquerda")
    public enum TipoInteresse
    {
        Interesse,    // ♡ direita
        Rejeicao      // × esquerda
    }

    // Registra a decisão de CADA LADO sobre o par (vaga, freelancer).
    // Cada lado tem no máximo um registro por par (índice único no AppDbContext);
    // se mudar de ideia, o registro é atualizado, não duplicado.
    // MATCH = existe Interesse do Freelancer E Interesse do Cliente para o mesmo par.
    public class Interesse
    {
        public int Id { get; set; }

        public int ProjetoId { get; set; }
        public int FreelancerId { get; set; }

        public OrigemInteresse Origem { get; set; }
        public TipoInteresse Tipo { get; set; }
        public DateTime DataRegistro { get; set; }

        public Projeto? Projeto { get; set; }
        public Freelancer? Freelancer { get; set; }
    }
}