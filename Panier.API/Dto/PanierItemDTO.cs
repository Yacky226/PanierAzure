namespace Panier.API.Dto
{
    public class PanierItemDTO
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public decimal SousTotal { get; set; }
        public ProduitDTO? Produit { get; set; }
    }
}
