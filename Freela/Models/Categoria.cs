namespace Freela.Models
{
    // Tabela Categorias do banco do grupo (Nome VARCHAR(50) UNIQUE).
    // As opções do formulário "Publicar projeto" são cadastradas pelo Database/freela.sql.
    public class Categoria
    {
        public int Id { get; set; }
        public string Nome { get; set; } = "";
    }
}