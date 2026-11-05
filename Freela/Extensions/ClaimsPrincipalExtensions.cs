using System.Security.Claims;

namespace Freela.Extensions
{
    // Atalho para ler o Id do usuário logado (gravado no cookie pelo ContaController).
    // Uso nos controllers: var usuarioId = User.ObterUsuarioId();
    public static class ClaimsPrincipalExtensions
    {
        public static int ObterUsuarioId(this ClaimsPrincipal usuario)
        {
            var valor = usuario.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(valor, out var id)
                ? id
                : throw new InvalidOperationException("Usuário não autenticado.");
        }
    }
}