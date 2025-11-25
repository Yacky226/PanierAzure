using Panier.API.Dto;
namespace Panier.API.Services
{
    public interface IPanierService
    {
        Task<PanierDTO> GetPanierByUserIdAsync(string userId);
        Task<PanierDTO> AddItemToPanierAsync(string userId, ProduitDTORequest itemRequest);
        Task<PanierDTO> UpdateItemQuantityAsync(string userId, int produitId, int quantity);
        Task<PanierDTO> RemoveItemFromPanierAsync(string userId, int produitId);
        Task ClearPanierAsync(string userId);
    }
}
