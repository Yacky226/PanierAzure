using Panier.API.Services;
using Panier.API.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Panier.API.Endpoints;

public static class PanierEndpoints
{
    public static void MapPanierEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/panier");

        // GET /api/panier/{userId}
        group.MapGet("/{userId}", async (string userId, IPanierService service) =>
        {
            try
            {
                var panier = await service.GetPanierByUserIdAsync(userId);
                return Results.Ok(panier);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message, statusCode: 500);
            }
        });

        // POST /api/panier/{userId}/items
        group.MapPost("/{userId}/items",
            async (
            string userId,
            [FromBody] ProduitDTORequest request,
            IPanierService service) =>
            {
                try
                {
                    var panier = await service.AddItemToPanierAsync(userId, request);
                    return Results.Ok(panier);
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.NotFound(ex.Message);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message, statusCode: 500);
                }
            });

        // PUT /api/panier/{userId}/items/{produitId}
        group.MapPut("/{userId}/items/{produitId:int}",
            async (
            string userId,
            int produitId,
            [FromBody] UpdateQuantityRequest request,
            IPanierService service) =>
            {
                try
                {
                    var panier = await service.UpdateItemQuantityAsync(userId, produitId, request.Quantity);
                    return Results.Ok(panier);
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.NotFound(ex.Message);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message, statusCode: 500);
                }
            });

        // DELETE /api/panier/{userId}/items/{produitId}
        group.MapDelete("/{userId}/items/{produitId:int}",
            async (
            string userId,
            int produitId,
            IPanierService service) =>
            {
                try
                {
                    var panier = await service.RemoveItemFromPanierAsync(userId, produitId);
                    return Results.Ok(panier);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message, statusCode: 500);
                }
            });

        // DELETE /api/panier/{userId}
        group.MapDelete("/{userId}",
            async (
            string userId,
            IPanierService service) =>
            {
                try
                {
                    await service.ClearPanierAsync(userId);
                    return Results.NoContent();
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message, statusCode: 500);
                }
            });
    }
}