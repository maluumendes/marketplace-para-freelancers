using System.ComponentModel.DataAnnotations;

namespace Freela.Models
{
    // Campos do formulário de cadastro (não é uma tabela do banco)
    public class CadastroViewModel
    {
        [Required(ErrorMessage = "Escolha o tipo de conta.")]
        public string TipoUsuario { get; set; } = "";   // "CLIENTE" ou "FREELANCER"

        [Required(ErrorMessage = "Informe seu nome.")]
        [StringLength(150, ErrorMessage = "Use no máximo 150 caracteres.")]
        public string Nome { get; set; } = "";          // nome completo ou razão social

        [Required(ErrorMessage = "Informe o documento.")]
        public string Documento { get; set; } = "";     // CPF (freelancer) ou CPF/CNPJ (cliente)

        [Required(ErrorMessage = "Informe seu e-mail.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [StringLength(150, ErrorMessage = "Use no máximo 150 caracteres.")]
        public string Email { get; set; } = "";

        [StringLength(20, ErrorMessage = "Use no máximo 20 caracteres.")]
        public string? Telefone { get; set; }           // opcional

        [Required(ErrorMessage = "Crie uma senha.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
        public string Senha { get; set; } = "";

        [Required(ErrorMessage = "Repita a senha.")]
        [Compare("Senha", ErrorMessage = "As senhas não conferem.")]
        public string ConfirmarSenha { get; set; } = "";
    }
}
