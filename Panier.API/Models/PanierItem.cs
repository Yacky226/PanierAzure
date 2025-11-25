using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Panier.API.Models
{
    [NotMapped] // idem: non persisté
    public class PanierItem
    {
        public int Id { get; set; }
        public Produit? Produit { get; set; } 

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        public decimal SousTotal => (Produit?.Prix ?? 0m) * Quantity;
    }
}
