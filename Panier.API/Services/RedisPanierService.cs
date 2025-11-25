using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Panier.API.Dto;
using Panier.API.Models;
using StackExchange.Redis;

namespace Panier.API.Services
{
    public class RedisPanierService : IPanierService
    {
        // Connexion à la base de données Redis
        private readonly IDatabase _redis;
        // Options de sérialisation JSON
        private readonly JsonSerializerOptions _jsonOptions;
        // Logger pour les erreurs et informations
        private readonly ILogger<RedisPanierService> _logger;

        public RedisPanierService(IConnectionMultiplexer mux, ILogger<RedisPanierService> logger)
        {
            // Initialiser la connexion Redis
            _redis = mux?.GetDatabase() ?? throw new ArgumentNullException(nameof(mux));
            // Initialiser le logger
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Configurer les options de sérialisation JSON
            _jsonOptions = new JsonSerializerOptions
            {
                //Convertir les noms de propriétés en camelCase
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        // Générer la clé Redis pour un utilisateur
        private static string KeyFor(string userId) => $"panier:{userId}";

        // Récupérer le panier depuis Redis
        private async Task<Models.Panier?> GetPanierModelFromRedisAsync(string userId)
        {
            try
            {
                //recuperer le panier
                var raw = await _redis.StringGetAsync(KeyFor(userId)).ConfigureAwait(false);
                // Si pas de panier, retourner null
                if (raw.IsNullOrEmpty) return null;
                // Désérialiser et retourner le modèle Panier
                return JsonSerializer.Deserialize<Models.Panier>(raw.ToString(), _jsonOptions);
            }
            catch (Exception ex)
            {
                // Loguer l'erreur et retourner null
                _logger.LogError(ex, "Erreur lecture Redis pour user {UserId}", userId);
                return null;
            }
        }

        private async Task SavePanierModelToRedisAsync(Models.Panier panier)
        {
            try
            {
                // Sérialiser le panier en JSON
                var raw = JsonSerializer.Serialize(panier, _jsonOptions);
                // Sauvegarder dans Redis avec une expiration de 30 jours
                var expiry = TimeSpan.FromDays(30);
                // sauvegarder
                await _redis.StringSetAsync(KeyFor(panier.UserId), raw, expiry).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Loguer l'erreur
                _logger.LogError(ex, "Erreur écriture Redis pour user {UserId}", panier.UserId);
                throw;
            }
        }

        // Implémentation des méthodes de l'interface IPanierService
        public async Task<PanierDTO> GetPanierByUserIdAsync(string userId)
        {
            // Valider l'input
            if (string.IsNullOrEmpty(userId)) throw new ArgumentException("UserId requis", nameof(userId));
            // Récupérer le modèle Panier depuis Redis
            var model = await GetPanierModelFromRedisAsync(userId);
            // Si pas de panier, en créer un vide
            if (model == null) model = new Models.Panier { UserId = userId, Items = new List<PanierItem>() };
            return MapToDTO(model);
        }

        // Ajouter un Produit au panier
        public async Task<PanierDTO> AddItemToPanierAsync(string userId, ProduitDTORequest itemRequest)
        {
            // Valider l'input
            if (string.IsNullOrEmpty(userId)) throw new ArgumentException("UserId requis", nameof(userId));
            if (itemRequest.Quantity <= 0) throw new ArgumentException("La quantité doit être > 0", nameof(itemRequest.Quantity));
            // Récupérer le panier depuis Redis ou en créer un nouveau
            var panier = await GetPanierModelFromRedisAsync(userId) ?? new Models.Panier { UserId = userId, Items = new List<PanierItem>() };

            // Créer  du produit à partir du DTO
            var produitSnapshot = new Produit
            {
                Id = itemRequest.Id,
                Nom = itemRequest.Nom,
                Prix = itemRequest.Prix,
            };

            // Vérifier si l'item existe déjà dans le panier
            var existing = panier.Items.FirstOrDefault(i => i.Produit.Id == itemRequest.Id);
            // Calculer la nouvelle quantité totale
            var newTotalQuantity = (existing?.Quantity ?? 0) + itemRequest.Quantity;

            // Mettre à jour ou ajouter l'item
            if (existing != null)
            {
                existing.Quantity = newTotalQuantity;
                // Mettre à jour le snapshot du produit 
                existing.Produit = produitSnapshot;
            }
            else
            {
                // Ajouter un nouvel item
                panier.Items.Add(new PanierItem
                {
                    Id = GenerateItemId(panier),
                    Quantity = itemRequest.Quantity,
                    Produit = produitSnapshot
                });
            }

            // Sauvegarder le panier mis à jour dans Redis
            await SavePanierModelToRedisAsync(panier);
            return MapToDTO(panier);
        }

        // Mettre à jour la quantité d'un item dans le panier
        public async Task<PanierDTO> UpdateItemQuantityAsync(string userId, int produitId, int quantity)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentException("UserId requis", nameof(userId));
            if (quantity < 0) throw new ArgumentException("La quantité ne peut pas être négative", nameof(quantity));

            // Récupérer le panier depuis Redis
            var panier = await GetPanierModelFromRedisAsync(userId) ?? throw new Exception("Panier non trouvé");
            var item = panier.Items.FirstOrDefault(i => i.Produit.Id == produitId);
            if (item == null) throw new KeyNotFoundException("Item non trouvé");

            if (quantity == 0)
            {
                panier.Items.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }

            // Si plus d'items, on supprime la clé Redis, sinon on sauvegarde
            if (!panier.Items.Any())
                await _redis.KeyDeleteAsync(KeyFor(userId));
            else
                await SavePanierModelToRedisAsync(panier);

            return MapToDTO(panier);
        }

        // Supprimer un item du panier
        public async Task<PanierDTO> RemoveItemFromPanierAsync(string userId, int produitId)
        {
            // Valider l'input
            if (string.IsNullOrEmpty(userId)) throw new ArgumentException("UserId requis", nameof(userId));
            // Récupérer le panier depuis Redis
            var panier = await GetPanierModelFromRedisAsync(userId) ?? new Models.Panier { UserId = userId, Items = new List<PanierItem>() };
            var item = panier.Items.FirstOrDefault(i => i.Produit.Id == produitId);
            if (item != null)
            {
                // Supprimer l'item 
                panier.Items.Remove(item);
                // Si plus d'items, supprimer la clé Redis, sinon sauvegarder le panier mis à jour  
                if (!panier.Items.Any())
                    await _redis.KeyDeleteAsync(KeyFor(userId));
                else
                    await SavePanierModelToRedisAsync(panier);
            }

            return MapToDTO(panier);
        }

        // Vider le panier
        public async Task ClearPanierAsync(string userId)
        {
            // Valider l'input
            if (string.IsNullOrEmpty(userId)) throw new ArgumentException("UserId requis", nameof(userId));
            // Supprimer la clé Redis
            await _redis.KeyDeleteAsync(KeyFor(userId));
        }

        // Mapper le modèle Panier vers le DTO PanierDTO
        private PanierDTO MapToDTO(Models.Panier? panier)
        {
            if (panier == null) return new PanierDTO { Id = 0, UserId = string.Empty, Items = new List<PanierItemDTO>() };

            return new PanierDTO
            {
                Id = panier.Id,
                UserId = panier.UserId,
                Items = panier.Items.Select(i => new PanierItemDTO
                {
                    Id = i.Id,
                    Quantity = i.Quantity,
                    SousTotal = (i.Produit?.Prix ?? 0m) * i.Quantity,
                    Produit = i.Produit != null ? new ProduitDTO
                    {
                        Id = i.Produit.Id,
                        Nom = i.Produit.Nom,
                        Prix = i.Produit.Prix
                    } : null
                }).ToList()
            };
        }

        // Générer un nouvel Id pour un item dans le panier 
        private int GenerateItemId(Models.Panier panier)
        {
            if (panier.Items == null || !panier.Items.Any()) return 1;
            return panier.Items.Max(i => i.Id) + 1;
        }
    }
}