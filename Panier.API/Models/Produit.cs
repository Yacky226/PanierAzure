using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Panier.API.Models
{
    public class Produit
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Nom { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Prix { get; set; }
    }
}
