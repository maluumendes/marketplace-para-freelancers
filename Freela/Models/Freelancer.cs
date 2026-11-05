
namespace Freela.Models
{
    public class Freelancer
    {
        // Mesmo ID da conta de usuário.
        public int Id { get; set; }

        // Dados do freelancer.
        public string NomeCompleto { get; set; } = "";
        public string Cpf { get; set; } = "";

        // Informações editáveis do perfil.
        public string? Telefone { get; set; }
        public string? Bio { get; set; }
        public string? LinkPortfolio { get; set; }

        // Novo campo: caminho da foto de perfil.
        public string? FotoPerfil { get; set; }

        // Relacionamento com a conta de usuário.
        public Usuario? Usuario { get; set; }
    }
}
