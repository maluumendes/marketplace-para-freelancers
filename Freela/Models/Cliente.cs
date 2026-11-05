
namespace Freela.Models
{
    public class Cliente
    {
        // Mesmo ID da conta de usuário.
        public int Id { get; set; }

        // Dados da empresa.
        public string NomeOuRazaoSocial { get; set; } = "";
        public string CpfOuCnpj { get; set; } = "";

        // Informações editáveis do perfil.
        public string? Telefone { get; set; }
        public string? Bio { get; set; }

        // Novo campo: caminho da foto ou logo da empresa.
        public string? FotoPerfil { get; set; }

        // Relacionamento com a conta de usuário.
        public Usuario? Usuario { get; set; }
    }
}
