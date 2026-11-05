using System.ComponentModel.DataAnnotations;

namespace Freela.Models
{
    // Recebe somente os campos que a empresa pode preencher no formulário.
    public class ProjetoViewModel
    {
        [Required(ErrorMessage = "Informe o título."), StringLength(100, MinimumLength = 5)]   // Titulo é VARCHAR(100) no banco do grupo
        public string Titulo { get; set; } = "";

        [Required(ErrorMessage = "Informe a descrição."), StringLength(10000, MinimumLength = 20)]
        public string Descricao { get; set; } = "";

        [Required(ErrorMessage = "Informe a categoria."), StringLength(50)]   // Nome da tabela Categorias
        public string Categoria { get; set; } = "";

        [Required(ErrorMessage = "Informe as habilidades."), StringLength(1000)]
        public string Habilidades { get; set; } = "";

        // Limites como número (e não texto): com texto, o .NET lê "0.01" no idioma do Windows
        // e em português (vírgula decimal) isso quebra a validação com erro 500.
        [Range(0.01, 999999999.99, ErrorMessage = "Informe um orçamento mínimo válido.")]
        public decimal OrcamentoMinimo { get; set; }

        [Range(0.01, 999999999.99, ErrorMessage = "Informe um orçamento máximo válido.")]
        public decimal OrcamentoMaximo { get; set; }

        [Range(1, 3650, ErrorMessage = "O prazo deve ser de 1 a 3650 dias.")]
        public int PrazoDias { get; set; }

        [Required]
        public string TipoPagamento { get; set; } = "Valor fixo";

        [Required]
        public string Experiencia { get; set; } = "Iniciante";
    }
}