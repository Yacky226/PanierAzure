namespace Panier.API.Dto
{
    public class PanierDTO
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public List<PanierItemDTO> Items { get; set; } = new();
        public decimal Total => Items?.Sum(i => i.SousTotal) ?? 0m;
    }
}
