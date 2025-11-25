namespace Panier.API.Dto
{
    public class ProduitDTO
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public decimal Prix { get; set; }
    }
}
