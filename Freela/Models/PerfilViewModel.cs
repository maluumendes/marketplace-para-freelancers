using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Freela.Models
{
    public class PerfilViewModel
    {
        // Dados usados para identificar o perfil na tela.
        // Esses campos são preenchidos pelo servidor.
        public string TipoUsuario { get; set; } = "";
        public string Nome { get; set; } = "";
        public string Email { get; set; } = "";

        // Biografia do usuário.
        [StringLength(1000, ErrorMessage = "A biografia pode ter até 1000 caracteres.")]
        public string? Bio { get; set; }

        // Telefone de contato.
        [StringLength(20, ErrorMessage = "O telefone pode ter até 20 caracteres.")]
        public string? Telefone { get; set; }

        // Usado somente pelo freelancer.
        [StringLength(255, ErrorMessage = "O portfólio pode ter até 255 caracteres.")]
        [Url(ErrorMessage = "Informe um link válido para o portfólio.")]
        public string? LinkPortfolio { get; set; }

        // Caminho da foto que já está salva.
        public string? FotoPerfilAtual { get; set; }

        // Arquivo escolhido no formulário.
        public IFormFile? Foto { get; set; }

        // Iniciais utilizadas quando o usuário ainda não tem foto.
        public string Iniciais
        {
            get
            {
                var partes = Nome.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries
                );

                if (partes.Length == 0)
                    return "?";

                if (partes.Length == 1)
                    return partes[0][..1].ToUpper();

                return (
                    partes[0][..1] +
                    partes[^1][..1]
                ).ToUpper();
            }
        }
    }
}