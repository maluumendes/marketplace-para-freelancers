namespace Freela.Models
{
    // Situação da vaga. No banco: 'ABERTO' / 'ENCERRADO' (MAIÚSCULAS, padrão do TipoUsuario).
    // No JSON da API: "Aberto" / "Encerrado".
    public enum StatusProjeto
    {
        Aberto,
        Encerrado
    }

    // Tabela Projetos (Database/freela.sql), com as colunas do formulário do Integrante 2.
    public class Projeto
    {
        public int Id { get; set; }

        // A empresa é representada pela classe Cliente no projeto do grupo.
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;

        // Categoria vem da tabela Categorias (FK), como no banco do grupo
        public int CategoriaId { get; set; }
        public Categoria Categoria { get; set; } = null!;

        public string Titulo { get; set; } = "";         // VARCHAR(100) no banco
        public string Descricao { get; set; } = "";      

        public decimal OrcamentoMinimo { get; set; }
        public decimal OrcamentoMaximo { get; set; }
        public int PrazoDias { get; set; }
        public string TipoPagamento { get; set; } = "Valor fixo";
        public string Experiencia { get; set; } = "Iniciante";
        public StatusProjeto Status { get; set; } = StatusProjeto.Aberto;

        public DateTime DataCriacao { get; set; }        // o banco preenche (CURRENT_TIMESTAMP)

        // Habilidades pedidas na vaga: N:M com a tabela Habilidades (tabela ProjetoHabilidades)
        public List<Habilidade> Habilidades { get; set; } = new();

        public List<Candidatura> Candidaturas { get; set; } = new();
    }
}
