namespace Freela.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string Senha { get; set; } = "";          // guardar o HASH, nunca a senha pura
        public string TipoUsuario { get; set; } = "";    // "CLIENTE" ou "FREELANCER"
        public DateTime DataCadastro { get; set; }

        // Ligação 1:1 (só um dos dois existe, conforme o TipoUsuario)
        public Cliente? Cliente { get; set; }
        public Freelancer? Freelancer { get; set; }
    }
}