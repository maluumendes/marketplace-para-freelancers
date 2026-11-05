namespace Freela.Models
{
    // Tabela Habilidades do banco do grupo (Nome VARCHAR(50) UNIQUE).
    // Usada pelas vagas (ProjetoHabilidades). FreelancerHabilidades continua com o Integrante 1.
    public class Habilidade
    {
        public int Id { get; set; }
        public string Nome { get; set; } = "";
    }
}