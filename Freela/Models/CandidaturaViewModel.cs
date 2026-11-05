using System.ComponentModel.DataAnnotations;

namespace Freela.Models
{
    // Campos do modal "Enviar candidatura" (não é uma tabela do banco)
    public class CandidaturaViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Vaga inválida.")]
        public int ProjetoId { get; set; }

        [Required(ErrorMessage = "Escreva uma mensagem de apresentação.")]
        [StringLength(1000, MinimumLength = 10,
            ErrorMessage = "A apresentação deve ter entre 10 e 1000 caracteres.")]
        public string MensagemApresentacao { get; set; } = "";

        // Limites como número (e não texto): com texto, o .NET lê "0.01" no idioma do Windows
        // e em português (vírgula decimal) isso quebra a validação com erro 500.
        [Range(0.01, 999999999.99, ErrorMessage = "Informe um valor proposto maior que zero.")]
        public decimal ValorProposto { get; set; }

        [Range(1, 3650, ErrorMessage = "O prazo deve ser de 1 a 3650 dias.")]
        public int PrazoDias { get; set; }
    }

    // A empresa aceita (true) ou recusa (false) um candidato.
    // bool? + [Required]: se o campo não vier no JSON, a API recusa (400)
    // em vez de entender "false" e rejeitar alguém sem querer.
    public class DecisaoCandidaturaViewModel
    {
        [Required(ErrorMessage = "Informe se o candidato foi aceito.")]
        public bool? Aceitar { get; set; }
    }
}