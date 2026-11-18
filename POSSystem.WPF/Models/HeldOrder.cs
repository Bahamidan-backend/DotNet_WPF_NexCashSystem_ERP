using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace POSSystem.WPF.Models
{
    public class HeldOrder
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime HoldTime { get; set; } = DateTime.Now;

        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }

        public string? OrderType { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? DiscountCode { get; set; }
        public bool HasDiscount { get; set; }

        public decimal TotalAmount { get; set; }

        // Delivery Info Snapshot
        public string? DeliveryAddress { get; set; }
        public string? DeliveryDriver { get; set; }
        public decimal DeliveryFee { get; set; }

        // Associated Shift / User Context
        public int? ShiftId { get; set; }
        public int? UserId { get; set; }

        // Persistent JSON snapshot of cart items
        public string? ItemsJson { get; set; }

        // In-memory items list for UI and ViewModels (Ignored by EF Core)
        [NotMapped]
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();

        /// <summary>
        /// Serializes the in-memory Items list into a persistent JSON snapshot.
        /// </summary>
        public void SerializeItems()
        {
            if (Items == null || Items.Count == 0)
            {
                ItemsJson = "[]";
                return;
            }

            var snapshots = Items.Select(item => new HeldOrderItemSnapshot
            {
                ProductId = item.ProductId,
                ProductName = item.Product?.Name ?? item.Name,
                AddonId = item.AddonId,
                AddonName = item.Addon?.Name,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                Size = item.Size,
                Notes = item.Notes,
                TotalPrice = item.TotalPrice,
                AddonItems = (item.addonAndOrderItems ?? Enumerable.Empty<AddonAndOrderItem>())
                    .Select(a => new HeldOrderAddonSnapshot
                    {
                        AddonId = a.AddonId,
                        AddonName = a.Addon?.Name,
                        Price = a.Addon?.Price ?? 0
                    }).ToList()
            }).ToList();

            ItemsJson = JsonSerializer.Serialize(snapshots);
        }

        /// <summary>
        /// Deserializes the persistent JSON snapshot into the in-memory Items list,
        /// re-attaching product and addon references from catalog if available.
        /// </summary>
        public void DeserializeItems(IEnumerable<Product>? allProducts = null, IEnumerable<Addon>? allAddons = null)
        {
            Items.Clear();
            if (string.IsNullOrWhiteSpace(ItemsJson)) return;

            try
            {
                var snapshots = JsonSerializer.Deserialize<List<HeldOrderItemSnapshot>>(ItemsJson);
                if (snapshots == null) return;

                foreach (var snap in snapshots)
                {
                    Product? matchedProduct = null;
                    if (snap.ProductId.HasValue && allProducts != null)
                    {
                        matchedProduct = allProducts.FirstOrDefault(p => p.Id == snap.ProductId.Value);
                    }

                    if (matchedProduct == null && snap.ProductId.HasValue)
                    {
                        matchedProduct = new Product
                        {
                            Id = snap.ProductId.Value,
                            Name = snap.ProductName ?? "صنف غير محدد",
                            BasePrice = snap.UnitPrice
                        };
                    }

                    Addon? matchedAddon = null;
                    if (snap.AddonId.HasValue && allAddons != null)
                    {
                        matchedAddon = allAddons.FirstOrDefault(a => a.Id == snap.AddonId.Value);
                    }

                    var orderItem = new OrderItem
                    {
                        ProductId = snap.ProductId,
                        Product = matchedProduct,
                        AddonId = snap.AddonId,
                        Addon = matchedAddon,
                        UnitPrice = snap.UnitPrice,
                        Quantity = snap.Quantity,
                        Size = snap.Size ?? string.Empty,
                        Notes = snap.Notes,
                        TotalPrice = snap.TotalPrice,
                        CreatedAt = DateTime.Now
                    };

                    if (snap.AddonItems != null && snap.AddonItems.Count > 0)
                    {
                        foreach (var addonSnap in snap.AddonItems)
                        {
                            Addon? aObj = allAddons?.FirstOrDefault(x => x.Id == addonSnap.AddonId);
                            orderItem.addonAndOrderItems.Add(new AddonAndOrderItem
                            {
                                AddonId = addonSnap.AddonId,
                                Addon = aObj ?? new Addon { Id = addonSnap.AddonId, Name = addonSnap.AddonName ?? "", Price = addonSnap.Price }
                            });
                        }
                    }

                    orderItem.InitializeTotalPrice();
                    Items.Add(orderItem);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HeldOrder] Failed to deserialize items: {ex.Message}");
            }
        }
    }

    public class HeldOrderItemSnapshot
    {
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public int? AddonId { get; set; }
        public string? AddonName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string? Size { get; set; }
        public string? Notes { get; set; }
        public decimal TotalPrice { get; set; }
        public List<HeldOrderAddonSnapshot> AddonItems { get; set; } = new();
    }

    public class HeldOrderAddonSnapshot
    {
        public int AddonId { get; set; }
        public string? AddonName { get; set; }
        public decimal Price { get; set; }
    }
}
