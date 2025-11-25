namespace Panier.API.Dto
{
    public class ProduitDTORequest
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public decimal Prix { get; set; }
        public int Quantity { get; set; }
    }
}
