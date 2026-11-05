using System.ComponentModel.DataAnnotations;

namespace Freela.Models
{
    // Campos do formulário de login (não é uma tabela do banco)
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Informe seu e-mail.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Informe sua senha.")]
        public string Senha { get; set; } = "";
    }
}
