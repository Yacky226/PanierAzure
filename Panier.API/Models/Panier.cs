using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Panier.API.Models
{
    [NotMapped] 
    public class Panier
    {
        public int Id { get; set; } 
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<PanierItem> Items { get; set; } = new List<PanierItem>();
    }
}
