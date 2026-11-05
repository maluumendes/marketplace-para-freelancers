using System.ComponentModel.DataAnnotations;

namespace Freela.Models
{
    // Registro de interesse/rejeição ("arrastar o card"). Não é uma tabela do banco.
    // - FREELANCER: envia ProjetoId e Tipo = "Rejeicao" (passar a vaga).
    //   O interesse positivo do freelancer é a própria candidatura.
    // - CLIENTE: envia ProjetoId, FreelancerId e Tipo ("Interesse" ou "Rejeicao").
    public class InteresseViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Vaga inválida.")]
        public int ProjetoId { get; set; }

        public int? FreelancerId { get; set; }

        [Required(ErrorMessage = "Informe o tipo: Interesse ou Rejeicao.")]
        public TipoInteresse? Tipo { get; set; }
    }
}