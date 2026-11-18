using POSSystem.WPF.Models;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Shered.Proberty;
using System.Linq;
using System.Windows;
using System;
using System.Threading.Tasks;
using POSSystem.WPF.StaticFunction;
using static POSSystem.WPF.Models.Category;

namespace POSSystem.WPF.ViewModels.Cashier
{
    public partial class CashierViewModel
    {
        /// <summary>
        /// تحميل أصناف الفاتورة الملغاة إلى سلة الكاشير كطلب جديد قابل للتعديل (Void & Reopen for Edit)
        /// </summary>
        public async Task LoadOrderIntoCartForEditAsync(Order sourceOrder)
        {
            if (sourceOrder == null) return;

            await LoadProductsAsync();

            foreach (var existing in cashierProberty.OrderItems.ToList())
            {
                if (existing.Product != null)
                {
                    existing.Product.IsInOrder = false;
                    existing.Product.Quantity = 0;
                    existing.Product.VirtualSizeName = null;
                }
            }
            cashierProberty.OrderItems.Clear();

            cashierProberty.OrderType = !string.IsNullOrWhiteSpace(sourceOrder.OrderType) ? sourceOrder.OrderType : "dinein";

            if (sourceOrder.OrderItems != null)
            {
                foreach (var srcItem in sourceOrder.OrderItems)
                {
                    Product? matchedProduct = null;
                    if (srcItem.AddonId.HasValue)
                    {
                        matchedProduct = _allProducts.FirstOrDefault(p => p.IsAddon && p.Id == srcItem.AddonId.Value);
                    }
                    else if (srcItem.ProductId.HasValue)
                    {
                        matchedProduct = _allProducts.FirstOrDefault(p => !p.IsAddon && p.Id == srcItem.ProductId.Value);
                    }

                    var newItem = new OrderItem
                    {
                        Id = 0,
                        OrderId = 0,
                        ProductId = srcItem.ProductId,
                        AddonId = srcItem.AddonId,
                        Product = matchedProduct ?? srcItem.Product,
                        Addon = srcItem.Addon,
                        Size = srcItem.Size ?? string.Empty,
                        UnitPrice = srcItem.UnitPrice,
                        Quantity = srcItem.Quantity,
                        Notes = srcItem.Notes ?? string.Empty
                    };
                    newItem.InitializeTotalPrice();
                    cashierProberty.OrderItems.Add(newItem);

                    if (matchedProduct != null)
                    {
                        matchedProduct.IsInOrder = true;
                        matchedProduct.Quantity += srcItem.Quantity;
                        if (!string.IsNullOrWhiteSpace(srcItem.Size))
                            matchedProduct.VirtualSizeName = NormalizeSizeName(srcItem.Size);
                    }
                }
            }

            await SequnceNumber();
            FilterProducts();
            OnPropertyChanged(nameof(cashierProberty.SubTotal));
            OnPropertyChanged(nameof(cashierProberty.CountProduct));
            OnPropertyChanged(nameof(cashierProberty.TotalAmount));
        }

        private void SyncProductCardState(int productId, bool isAddon = false)
        {
            if (isAddon)
            {
                int addonQty = cashierProberty.OrderItems.Where(i => i.AddonId == productId).Sum(i => i.Quantity);
                foreach (var p in _allProducts.Where(x => x.IsAddon && x.Id == productId))
                {
                    p.Quantity = addonQty;
                    p.IsInOrder = addonQty > 0;
                }
                if (Products != null)
                {
                    foreach (var p in Products.Where(x => x.IsAddon && x.Id == productId))
                    {
                        p.Quantity = addonQty;
                        p.IsInOrder = addonQty > 0;
                    }
                }
                return;
            }

            var matchingItems = cashierProberty.OrderItems.Where(i => i.ProductId == productId).ToList();
            int totalUnits = matchingItems.Sum(i => i.Quantity);
            string? activeSize = matchingItems.LastOrDefault()?.Size;

            var masterProd = _allProducts.FirstOrDefault(x => !x.IsAddon && x.Id == productId);
            if (masterProd != null)
            {
                masterProd.Quantity = totalUnits;
                masterProd.IsInOrder = totalUnits > 0;
                if (totalUnits == 0)
                {
                    masterProd.VirtualSizeName = null;
                }
                else if (string.IsNullOrWhiteSpace(masterProd.VirtualSizeName) ||
                         !matchingItems.Any(i => SizesMatch(i.Size, masterProd.VirtualSizeName)))
                {
                    masterProd.VirtualSizeName = activeSize;
                }
            }

            if (Products != null)
            {
                foreach (var card in Products.Where(x => !x.IsAddon && x.Id == productId))
                {
                    if (card.ParentProduct != null && !string.IsNullOrWhiteSpace(card.VirtualSizeName))
                    {
                        // Separate cards per size mode
                        int sizeQty = matchingItems.Where(i => SizesMatch(i.Size, card.VirtualSizeName)).Sum(i => i.Quantity);
                        card.Quantity = sizeQty;
                        card.IsInOrder = sizeQty > 0;
                    }
                    else
                    {
                        // Merged card mode (Note 6): show active size quantity or total item count (>= 1 when in order)
                        if (!string.IsNullOrWhiteSpace(card.VirtualSizeName))
                        {
                            var activeItem = matchingItems.FirstOrDefault(i => SizesMatch(i.Size, card.VirtualSizeName));
                            if (activeItem != null)
                            {
                                card.Quantity = activeItem.Quantity;
                                card.IsInOrder = true;
                            }
                            else if (matchingItems.Any())
                            {
                                var fallback = matchingItems.Last();
                                card.VirtualSizeName = fallback.Size;
                                card.Quantity = fallback.Quantity;
                                card.IsInOrder = true;
                            }
                            else
                            {
                                card.VirtualSizeName = null;
                                card.Quantity = 0;
                                card.IsInOrder = false;
                            }
                        }
                        else
                        {
                            card.Quantity = totalUnits;
                            card.IsInOrder = totalUnits > 0;
                            if (totalUnits > 0 && !string.IsNullOrWhiteSpace(activeSize))
                                card.VirtualSizeName = activeSize;
                        }
                    }
                }
            }
        }

        public void AddProductToOrder(Product product)
        {
            if (product == null) return;
            if (!HasActiveShift)
            {
                MessageBox.Show("يجب بدء مناوبة قبل إضافة منتجات للطلب.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!product.IsAddon && product.ProductSizes != null && product.ProductSizes.Any())
            {
                string chosen = !string.IsNullOrWhiteSpace(product.VirtualSizeName)
                    ? product.VirtualSizeName
                    : GetDefaultSizeForProduct(product);
                AddProductWithSizeToOrder(product, chosen);
                return;
            }

            string chosenSize = product.IsAddon ? string.Empty : GetDefaultSizeForProduct(product);
            OrderItem? existingItem = product.IsAddon
                ? cashierProberty.OrderItems.FirstOrDefault(item => item.AddonId == product.Id)
                : FindExistingOrderItem(product.Id, chosenSize);

            decimal sizeMult = GetSizeMultiplier(chosenSize);
            decimal totalPortionsUsed = product.IsAddon
                ? (existingItem?.Quantity ?? 0)
                : cashierProberty.OrderItems.Where(item => item.ProductId == product.Id).Sum(item => GetSizeMultiplier(item.Size) * item.Quantity);

            if (!product.IsUntrackedStock && totalPortionsUsed + sizeMult > product.TotalValue)
            {
                MessageBox.Show($"عذراً، الكمية المطلوبة من '{product.Name}' غير متوفرة حالياً في المخزون.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (existingItem == null)
            {
                var productSize = product.ProductSizes?.FirstOrDefault(s => SizesMatch(s.SizeName, chosenSize));
                decimal unitPrice = productSize?.Price ?? product.Price;
                var orderItem = new OrderItem { UnitPrice = unitPrice, Quantity = 1, Notes = string.Empty };
                if (product.IsAddon)
                {
                    orderItem.AddonId = product.Id;
                    orderItem.Product = product;
                    orderItem.Size = string.Empty;
                }
                else
                {
                    orderItem.ProductId = product.Id;
                    orderItem.Product = product.ParentProduct ?? product;
                    orderItem.Size = chosenSize;
                }

                orderItem.InitializeTotalPrice();
                cashierProberty.OrderItems.Add(orderItem);
            }
            else
            {
                if (!product.IsAddon) existingItem.Size = chosenSize;
                existingItem.Quantity++;
            }

            if (!product.IsAddon && !string.IsNullOrWhiteSpace(chosenSize))
            {
                product.VirtualSizeName = chosenSize;
            }

            SyncProductCardState(product.Id, product.IsAddon);
            OnPropertyChanged(nameof(cashierProberty.SubTotal));
            OnPropertyChanged(nameof(cashierProberty.CountProduct));
            OnPropertyChanged(nameof(cashierProberty.TotalAmount));
        }

        public void AddProductWithSizeToOrder(Product product, string sizeName)
        {
            if (product == null) return;
            if (!HasActiveShift)
            {
                MessageBox.Show("يجب بدء مناوبة قبل إضافة منتجات للطلب.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!product.IsUntrackedStock && product.TotalValue <= 0)
            {
                MessageBox.Show($"عذراً، الصنف '{product.Name}' غير متوفر حالياً في المخزون.", "تنبيه - نفاد الكمية", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string normalizedSize = NormalizeSizeName(sizeName);
            decimal sizeMultiplier = GetSizeMultiplier(normalizedSize);

            decimal totalPortionsUsed = cashierProberty.OrderItems
                .Where(item => item.ProductId == product.Id)
                .Sum(item => GetSizeMultiplier(item.Size) * item.Quantity);

            if (!product.IsUntrackedStock && totalPortionsUsed + sizeMultiplier > product.TotalValue)
            {
                MessageBox.Show("عذراً، الكمية المطلوبة غير متوفرة حالياً في المخزون.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var realProduct = product.ParentProduct ?? _allProducts.FirstOrDefault(p => !p.IsAddon && p.Id == product.Id) ?? product;
            var productSize = realProduct.ProductSizes?.FirstOrDefault(s => SizesMatch(s.SizeName, normalizedSize))
                              ?? product.ProductSizes?.FirstOrDefault(s => SizesMatch(s.SizeName, normalizedSize));
            decimal unitPrice = productSize?.Price ?? product.Price;
            OrderItem? existingItem = FindExistingOrderItem(product.Id, normalizedSize);

            // Track the active selected size on the card so + and - control this exact size (Note 6)
            product.VirtualSizeName = normalizedSize;
            realProduct.VirtualSizeName = normalizedSize;

            if (existingItem == null)
            {
                var orderItem = new OrderItem
                {
                    ProductId = realProduct.Id,
                    Product = realProduct,
                    UnitPrice = unitPrice,
                    Quantity = 1,
                    Size = normalizedSize,
                    Notes = string.Empty
                };
                orderItem.InitializeTotalPrice();
                cashierProberty.OrderItems.Add(orderItem);
            }
            else
            {
                existingItem.Size = normalizedSize;
                existingItem.Quantity++;
                existingItem.InitializeTotalPrice();
            }

            SyncProductCardState(realProduct.Id, false);
            OnPropertyChanged(nameof(cashierProberty.SubTotal));
            OnPropertyChanged(nameof(cashierProberty.CountProduct));
            OnPropertyChanged(nameof(cashierProberty.TotalAmount));
        }

        private decimal GetSizeMultiplier(string size)
        {
            size = NormalizeSizeName(size);
            if (string.IsNullOrEmpty(size)) return 1m;
            if (size == "نص نفر" || size == "نص حبة" || size == "نص ذبيحة") return 0.5m;
            if (size == "نفر ونص" || size == "حبة ونص" || size == "ذبيحة ونص") return 1.5m;
            if (size == "ربع نفر" || size == "ربع حبة" || size == "ربع ذبيحة") return 0.25m;
            if (size == "ثمن نفر" || size == "ثمن حبة" || size == "ثمن ذبيحة") return 0.125m;
            return 1m;
        }

        private static string NormalizeSizeName(string? size)
        {
            if (string.IsNullOrWhiteSpace(size)) return string.Empty;
            return size switch
            {
                nameof(PortionSize.Single) or "Single" => "نفر",
                nameof(PortionSize.Piece) or "Piece" => "حبة",
                nameof(PortionSize.Large) or "Large" => "كبير",
                _ => size.Trim()
            };
        }

        private static bool SizesMatch(string? a, string? b)
        {
            var normA = NormalizeSizeName(a);
            var normB = NormalizeSizeName(b);
            if (string.IsNullOrWhiteSpace(normA) && string.IsNullOrWhiteSpace(normB)) return true;
            return normA == normB;
        }

        private static string GetDefaultSizeForProduct(Product product)
        {
            if (!string.IsNullOrEmpty(product.VirtualSizeName))
                return NormalizeSizeName(product.VirtualSizeName);

            var fullSize = product.ProductSizes?.FirstOrDefault(s =>
                s.SizeName == "نفر" || s.SizeName == "حبة");
            if (fullSize != null)
                return fullSize.SizeName;

            if (product.ProductSizes?.Any() == true)
                return product.ProductSizes.First().SizeName;

            if (product.Category != null &&
                PortionSizeHelper.DisplayNames.TryGetValue(product.Category.portionSize, out var displayName))
                return displayName;

            return string.Empty;
        }

        private OrderItem? FindExistingOrderItem(int id, string sizeName, bool isAddon = false)
        {
            if (isAddon)
            {
                return cashierProberty.OrderItems.FirstOrDefault(item => item.AddonId == id);
            }
            var normalized = NormalizeSizeName(sizeName);

            var exactMatch = cashierProberty.OrderItems.FirstOrDefault(item => 
                item.ProductId == id && SizesMatch(item.Size, normalized));
            if (exactMatch != null) return exactMatch;

            if (string.IsNullOrWhiteSpace(normalized))
            {
                return cashierProberty.OrderItems.LastOrDefault(item => item.ProductId == id);
            }

            return null;
        }

        public void RemoveProductFromOrder(OrderItem orderItem)
        {
            if (orderItem == null) return;
            int prodId = orderItem.AddonId ?? orderItem.ProductId ?? 0;
            bool isAddon = orderItem.AddonId.HasValue;

            cashierProberty.OrderItems.Remove(orderItem);

            if (prodId > 0)
            {
                SyncProductCardState(prodId, isAddon);
            }

            if (cashierProberty.OrderItems.Count == 0)
            {
                ClearDiscount();
            }

            OnPropertyChanged(nameof(cashierProberty.SubTotal));
            OnPropertyChanged(nameof(cashierProberty.CountProduct));
            OnPropertyChanged(nameof(cashierProberty.TotalAmount));
        }

        public void IncreaseQuantity(OrderItem orderItem)
        {
            if (orderItem == null) return;
            bool isUntracked = orderItem.Product?.IsUntrackedStock == true;
            decimal totalValue = isUntracked ? decimal.MaxValue : (orderItem.Product?.TotalValue ?? decimal.MaxValue);
            int prodId = orderItem.AddonId ?? orderItem.ProductId ?? 0;
            bool isAddon = orderItem.AddonId.HasValue;

            decimal sizeMult = isAddon ? 1m : GetSizeMultiplier(orderItem.Size);
            decimal totalPortionsUsed = isAddon
                ? orderItem.Quantity
                : cashierProberty.OrderItems
                    .Where(item => item.ProductId == prodId)
                    .Sum(item => GetSizeMultiplier(item.Size) * item.Quantity);

            if (totalPortionsUsed + sizeMult > totalValue)
            {
                MessageBox.Show("عذراً، لا يمكن زيادة الكمية لعدم توفر مخزون كافٍ.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            orderItem.Quantity++;
            if (prodId > 0)
            {
                SyncProductCardState(prodId, isAddon);
            }
            OnPropertyChanged(nameof(cashierProberty.SubTotal));
            OnPropertyChanged(nameof(cashierProberty.CountProduct));
            OnPropertyChanged(nameof(cashierProberty.TotalAmount));
        }

        public void DecreaseQuantity(OrderItem orderItem)
        {
            if (orderItem == null) return;
            int prodId = orderItem.AddonId ?? orderItem.ProductId ?? 0;
            bool isAddon = orderItem.AddonId.HasValue;

            if (orderItem.Quantity > 1)
            {
                orderItem.Quantity--;
            }
            else
            {
                cashierProberty.OrderItems.Remove(orderItem);
            }

            if (prodId > 0)
            {
                SyncProductCardState(prodId, isAddon);
            }

            if (cashierProberty.OrderItems.Count == 0)
            {
                ClearDiscount();
            }

            OnPropertyChanged(nameof(cashierProberty.SubTotal));
            OnPropertyChanged(nameof(cashierProberty.CountProduct));
            OnPropertyChanged(nameof(cashierProberty.TotalAmount));
        }

        public void IncreaseProductQuantity(Product product)
        {
            if (product == null) return;

            if (!product.IsAddon)
            {
                // Note 6: Use the active size selected on the card (e.g. نص نفر) or the last added size in cart for this product
                string targetSize = !string.IsNullOrWhiteSpace(product.VirtualSizeName)
                    ? NormalizeSizeName(product.VirtualSizeName)
                    : (cashierProberty.OrderItems.LastOrDefault(i => i.ProductId == product.Id)?.Size ?? GetDefaultSizeForProduct(product));

                if (!string.IsNullOrWhiteSpace(targetSize))
                {
                    AddProductWithSizeToOrder(product, targetSize);
                    return;
                }
            }

            var existingItem = FindExistingOrderItem(product.Id, GetDefaultSizeForProduct(product), product.IsAddon);
            if (existingItem != null)
            {
                IncreaseQuantity(existingItem);
            }
            else
            {
                AddProductToOrder(product);
            }
        }

        public void DecreaseProductQuantity(Product product)
        {
            if (product == null) return;

            OrderItem? existingItem = null;
            if (!product.IsAddon)
            {
                if (!string.IsNullOrWhiteSpace(product.VirtualSizeName))
                {
                    existingItem = FindExistingOrderItem(product.Id, product.VirtualSizeName, false);
                }
                existingItem ??= cashierProberty.OrderItems.LastOrDefault(i => i.ProductId == product.Id);
            }
            else
            {
                existingItem = FindExistingOrderItem(product.Id, string.Empty, true);
            }

            if (existingItem != null)
            {
                DecreaseQuantity(existingItem);
            }
        }

        public void CancelCurrentInvoice()
        {
            foreach (var prod in _allProducts)
            {
                prod.IsInOrder = false;
                prod.Quantity = 0;
                prod.VirtualSizeName = null;
            }
            if (Products != null)
            {
                foreach (var prod in Products)
                {
                    prod.IsInOrder = false;
                    prod.Quantity = 0;
                    if (prod.ParentProduct == null)
                        prod.VirtualSizeName = null;
                }
            }

            cashierProberty.OrderItems.Clear();
            ClearDiscount();
            cashierProberty.DeliveryCustomerName = string.Empty;
            cashierProberty.DeliveryPhone = string.Empty;
            cashierProberty.DeliveryAddress = string.Empty;
            cashierProberty.SelectedDeliveryCustomer = null;
            cashierProberty.DeliveryFee = 0;

            OnPropertyChanged(nameof(cashierProberty.SubTotal));
            OnPropertyChanged(nameof(cashierProberty.CountProduct));
            OnPropertyChanged(nameof(cashierProberty.TotalAmount));
        }

        public void ClearDiscount()
        {
            cashierProberty.SelectedDiscount = null;
            cashierProberty.DiscountCode = string.Empty;
            cashierProberty.DiscountAmount = 0m;
            cashierProberty.HasDiscount = false;
            OnPropertyChanged(nameof(cashierProberty.SubTotal));
            OnPropertyChanged(nameof(cashierProberty.TotalAmount));
        }

        public int SetDirectOrderItemQuantity(OrderItem orderItem, int desiredQty, out bool wasCapped, out int maxAvailable)
        {
            wasCapped = false;
            maxAvailable = 99999;
            if (orderItem == null) return 1;

            int prodId = orderItem.AddonId ?? orderItem.ProductId ?? 0;
            bool isAddon = orderItem.AddonId.HasValue;
            var realProduct = orderItem.Product ?? _allProducts.FirstOrDefault(p => (isAddon ? p.IsAddon : !p.IsAddon) && p.Id == prodId);

            bool isUntracked = realProduct?.IsUntrackedStock == true;
            decimal totalValue = isUntracked ? decimal.MaxValue : (realProduct?.TotalValue ?? decimal.MaxValue);

            decimal sizeMult = isAddon ? 1m : GetSizeMultiplier(orderItem.Size);
            if (sizeMult <= 0) sizeMult = 1m;

            decimal otherPortionsUsed = isAddon
                ? 0m
                : cashierProberty.OrderItems
                    .Where(item => item != orderItem && item.ProductId == prodId)
                    .Sum(item => GetSizeMultiplier(item.Size) * item.Quantity);

            if (!isUntracked && totalValue < decimal.MaxValue)
            {
                decimal remainingForThisItem = Math.Max(0m, totalValue - otherPortionsUsed);
                maxAvailable = (int)Math.Floor(remainingForThisItem / sizeMult);
            }

            if (desiredQty <= 0)
            {
                desiredQty = 1;
            }

            if (!isUntracked && desiredQty > maxAvailable)
            {
                wasCapped = true;
                orderItem.Quantity = Math.Max(1, maxAvailable);
            }
            else
            {
                orderItem.Quantity = desiredQty;
            }

            orderItem.InitializeTotalPrice();

            if (prodId > 0)
            {
                SyncProductCardState(prodId, isAddon);
            }

            if (cashierProberty.HasDiscount)
            {
                ApplyDiscount();
            }

            OnPropertyChanged(nameof(cashierProberty.SubTotal));
            OnPropertyChanged(nameof(cashierProberty.CountProduct));
            OnPropertyChanged(nameof(cashierProberty.TotalAmount));

            return orderItem.Quantity;
        }

        public int SetDirectProductQuantity(Product product, int desiredQty, out bool wasCapped, out int maxAvailable)
        {
            wasCapped = false;
            maxAvailable = 99999;
            if (product == null) return 1;

            OrderItem? existingItem = null;
            if (!product.IsAddon)
            {
                string targetSize = !string.IsNullOrWhiteSpace(product.VirtualSizeName)
                    ? NormalizeSizeName(product.VirtualSizeName)
                    : (cashierProberty.OrderItems.LastOrDefault(i => i.ProductId == product.Id)?.Size ?? GetDefaultSizeForProduct(product));

                if (!string.IsNullOrWhiteSpace(targetSize))
                {
                    existingItem = FindExistingOrderItem(product.Id, targetSize, false);
                }
                existingItem ??= cashierProberty.OrderItems.LastOrDefault(i => i.ProductId == product.Id);
            }
            else
            {
                existingItem = FindExistingOrderItem(product.Id, string.Empty, true);
            }

            if (existingItem == null)
            {
                AddProductToOrder(product);
                existingItem = product.IsAddon
                    ? FindExistingOrderItem(product.Id, string.Empty, true)
                    : cashierProberty.OrderItems.LastOrDefault(i => i.ProductId == product.Id);
            }

            if (existingItem != null)
            {
                return SetDirectOrderItemQuantity(existingItem, desiredQty, out wasCapped, out maxAvailable);
            }

            return product.Quantity;
        }

        public void EditProductQuantityFromCashier(Product product)
        {
            var existingItem = FindExistingOrderItem(product.Id, GetDefaultSizeForProduct(product), product.IsAddon);
            if (existingItem != null)
            {
                if (product.Quantity == 0)
                {
                    cashierProberty.OrderItems.Remove(existingItem);
                }
                else
                {
                    existingItem.Quantity = product.Quantity;
                }
                SyncProductCardState(product.Id, product.IsAddon);
                OnPropertyChanged(nameof(cashierProberty.SubTotal));
                OnPropertyChanged(nameof(cashierProberty.CountProduct));
                OnPropertyChanged(nameof(cashierProberty.TotalAmount));
            }
            else if (product.Quantity > 0)
            {
                AddProductToOrder(product);
            }
        }

        public void ChangeItemSize(OrderItem item, string sizeCategory)
        {
            if (item == null || item.Product == null) return;
            string newSize = sizeCategory switch
            {
                "Full" => item.Product.ProductSizes?.FirstOrDefault(s => s.SizeName.Contains("نفر") || s.SizeName.Contains("حبة") || s.SizeName.Contains("ذبيحة"))?.SizeName ?? "نفر",
                "Half" => item.Product.ProductSizes?.FirstOrDefault(s => s.SizeName.Contains("نص"))?.SizeName ?? "نص نفر",
                _ => item.Size
            };

            if (item.Size == newSize) return;
            var existingItem = FindExistingOrderItem(item.Product.Id, newSize);
            if (existingItem != null && existingItem != item)
            {
                existingItem.Quantity += item.Quantity;
                cashierProberty.OrderItems.Remove(item);
            }
            else
            {
                item.Size = newSize;
                var productSize = item.Product.ProductSizes?.FirstOrDefault(s => s.SizeName == newSize);
                if (productSize != null) item.UnitPrice = productSize.Price;
            }

            SyncProductCardState(item.Product.Id, false);
            OnPropertyChanged(nameof(cashierProberty.SubTotal));
            OnPropertyChanged(nameof(cashierProberty.CountProduct));
            OnPropertyChanged(nameof(cashierProberty.TotalAmount));
        }

        public void DeductProductFromInventory(OrderItem item)
        {
            var productRef = _allProducts.FirstOrDefault(p => (item.AddonId.HasValue ? p.Id == item.AddonId && p.IsAddon : p.Id == item.ProductId && !p.IsAddon));
            if (productRef != null)
            {
                decimal deduction = 0m;
                if (item.AddonId.HasValue || productRef.IsAddon) deduction = item.Quantity * 1m;
                else if (item.Size == "نفر" || item.Size == "حبة" || item.Size == "ذبيحة") deduction = item.Quantity * 1m;
                else if (item.Size == "نص نفر" || item.Size == "نص حبة" || item.Size == "نص ذبيحة") deduction = item.Quantity * 0.5m;
                else if (item.Size == "نفر ونص" || item.Size == "حبة ونص" || item.Size == "ذبيحة ونص") deduction = item.Quantity * 1.5m;
                else if (item.Size == "ربع حبة" || item.Size == "ربع نفر" || item.Size == "ربع ذبيحة") deduction = item.Quantity * 0.25m;
                else if (item.Size == "ثمن حبة" || item.Size == "ثمن نفر" || item.Size == "ثمن ذبيحة") deduction = item.Quantity * 0.125m;
                else if (item.Size == "نفرين" || item.Size == "حبتين") deduction = item.Quantity * 2m;
                else deduction = item.Quantity * 1m;

                if (!productRef.IsUntrackedStock) productRef.TotalValue = Math.Max(0, productRef.TotalValue - deduction);
                if (productRef.PersonNumberFood > 0) productRef.StockQuantity = productRef.TotalValue / productRef.PersonNumberFood;
                productRef.Quantity = 0;
                productRef.IsInOrder = false;
                productRef.VirtualSizeName = null;
            }
        }
    }
}
