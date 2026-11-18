using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using POSSystem.WPF.Models;
using POSSystem.WPF.Views;
using POSSystem.WPF.Business.Services;

namespace POSSystem.WPF.ViewModels.AnimalBatches
{
    public partial class AddAnimalBatchViewModel
    {
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        private async void Save()
        {
            bool startNewTransaction = !_unitOfWork.HasActiveTransaction;
            if (startNewTransaction)
            {
                await _unitOfWork.BeginTransactionAsync();
            }

            try
            {
                if (SelectedWarehouse == null)
                {
                    MessageBox.Show("يرجى اختيار المستودع", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    if (startNewTransaction) await _unitOfWork.RollbackTransactionAsync();
                    return;
                }

                if (QuantityInput <= 0)
                {
                    MessageBox.Show("يرجى إدخال الكمية بشكل صحيح", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    if (startNewTransaction) await _unitOfWork.RollbackTransactionAsync();
                    return;
                }

                if (SelectedProduct != null)
                {
                    if (SelectedProduct.StockQuantity < 1)
                    {
                        MessageBox.Show($"الكمية المتوفرة من المنتج '{SelectedProduct.Name}' ({SelectedProduct.StockQuantity}) لا تكفي لخصم العدد", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        if (startNewTransaction) await _unitOfWork.RollbackTransactionAsync();
                        return;
                    }

                    SelectedProduct.StockQuantity -= 1;
                    SelectedProduct.UpdatedAt = DateTime.Now;
                    _unitOfWork.warehouseFromProduct.UpdateWarehouseFromProducts(SelectedProduct);
                }

                decimal finalTotalWeight = 0;
                int finalHeadCount = 0;
                decimal finalBackWeight = 0;
                decimal finalLiverWeight = 0;

                switch (SelectedComponentId)
                {
                    case 4: 
                    case 5: 
                        finalTotalWeight = QuantityInput;
                        break;
                    case 1: 
                        finalHeadCount = (int)QuantityInput;
                        break;
                    case 3: 
                        finalLiverWeight = QuantityInput;
                        break;
                    case 2: 
                        finalBackWeight = QuantityInput;
                        break;
                }

                var animalType = (AnimalType)(SelectedAnimalTypeIndex + 1);
                string animalTypeName = animalType == AnimalType.Sheep ? "خروف" : animalType == AnimalType.Cow ? "بقرة" : "جمل";
                
                var batch = new AnimalBatch
                {
                    Name = $"أجزاء {animalTypeName} - {DateTime.Now:yyyy-MM-dd HH:mm}",
                    IsActive = true,
                    Type = animalType,
                    IsWeightMode = IsWeightMode,
                    TotalWeight = finalTotalWeight,
                    RemainingWeight = finalTotalWeight,
                    HeadCount = finalHeadCount,
                    BackWeight = finalBackWeight,
                    LiverWeight = finalLiverWeight,
                    WarehouseId = SelectedWarehouse.Id,
                    SupplierId = SelectedSupplier?.Id,
                    PurchaseCost = SelectedProduct != null ? SelectedProduct.Price : 0,
                    BatchDate = DateTime.Now,
                    Components = new List<BatchComponent>()
                };

                var components = new List<BatchComponent>
                {
                    new BatchComponent { Name = $"رأس {animalTypeName}", IsActive = true, ComponentType = ComponentType.Head, Quantity = finalHeadCount, OriginalQuantity = finalHeadCount, Unit = "حبة" },
                    new BatchComponent { Name = $"ظهر {animalTypeName}", IsActive = true, ComponentType = ComponentType.Back, Quantity = finalBackWeight, OriginalQuantity = finalBackWeight, Unit = "كجم" },
                    new BatchComponent { Name = $"كبدة {animalTypeName}", IsActive = true, ComponentType = ComponentType.Liver, Quantity = finalLiverWeight, OriginalQuantity = finalLiverWeight, Unit = "كجم" },
                    new BatchComponent { Name = $"لحم {animalTypeName}", IsActive = true, ComponentType = ComponentType.Meat, Quantity = finalTotalWeight, OriginalQuantity = finalTotalWeight, Unit = "كجم" }
                };

                foreach (var component in components)
                {
                    batch.Components.Add(component);
                }

                var componentsToCreate = new List<(string Name, decimal Quantity, ComponentType Type)>();
                
                if (finalHeadCount > 0)
                    componentsToCreate.Add(($"رأس {animalTypeName}", finalHeadCount, ComponentType.Head));
                
                if (finalLiverWeight > 0)
                    componentsToCreate.Add(($"كبدة {animalTypeName}", finalLiverWeight, ComponentType.Liver));
                
                if (finalBackWeight > 0)
                    componentsToCreate.Add(($"ظهر {animalTypeName}", finalBackWeight, ComponentType.Back));

                if (finalTotalWeight > 0)
                {
                    string unitLabel = SelectedComponentId == 4 ? "(كجم)" : "(ثمن/جزء)";
                    componentsToCreate.Add(($"لحم {animalTypeName} {unitLabel}", finalTotalWeight, ComponentType.Meat));
                }

                int categoryIdToUse = 1;

                foreach (var comp in componentsToCreate)
                {
                    var product = (await _unitOfWork.Products.FindAsync(p => p.Name == comp.Name)).FirstOrDefault();
                    
                    if (product == null)
                    {
                        product = new Product
                        {
                            Name = comp.Name,
                            Price = 0, 
                            BasePrice = 0, 
                            CategoryId = categoryIdToUse,
                            IsActive = true,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            StockQuantity = 0,
                            TotalValue = 0,
                            IsAnimalComponent = true,
                            AnimalComponentType = comp.Type
                        };
                        await _unitOfWork.Products.AddAsync(product);
                        await _unitOfWork.SaveChangesAsync(); 
                    }

                    var targetWarehouseProduct = (await _unitOfWork.warehouseFromProduct.FindAsync(
                        wfp => wfp.WarehouseId == SelectedWarehouse.Id && wfp.Name == product.Name)).FirstOrDefault();

                    if (targetWarehouseProduct == null)
                    {
                        targetWarehouseProduct = new WarehouseFromProduct
                        {
                            WarehouseId = SelectedWarehouse.Id,
                            Name = product.Name,
                            StockQuantity = comp.Quantity,
                            Price = 0,
                            IsActive = true,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };
                        await _unitOfWork.warehouseFromProduct.AddAsync(targetWarehouseProduct);
                    }
                    else
                    {
                        targetWarehouseProduct.StockQuantity += comp.Quantity;
                        targetWarehouseProduct.UpdatedAt = DateTime.Now;
                        _unitOfWork.warehouseFromProduct.Update(targetWarehouseProduct);
                    }
                }

                _unitOfWork.AnimalBatches.AddBatchWithComponents(batch);
                await _unitOfWork.SaveChangesAsync();
                
                if (startNewTransaction)
                {
                    await _unitOfWork.CommitTransactionAsync();
                }

                MessageBox.Show("تم إضافة الذبيحة بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var adminWindow = Application.Current.Windows.OfType<AdminDashboardWindow>().FirstOrDefault();
                    if (adminWindow != null)
                    {
                        adminWindow.CloseOverlay();
                        
                        if (adminWindow.MainContentArea.Content is AnimalBatchManagementView batchView && 
                            batchView.DataContext is AnimalBatchManagementViewModel batchViewModel)
                        {
                            batchViewModel.LoadBatchesCommand.Execute(null);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                if (startNewTransaction) await _unitOfWork.RollbackTransactionAsync();
                
                string detail = ex.InnerException != null ? $"\nالتفاصيل: {ex.InnerException.Message}" : "";
                MessageBox.Show($"خطأ في إضافة الذبيحة: {ex.Message}{detail}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var adminWindow = Application.Current.Windows.OfType<AdminDashboardWindow>().FirstOrDefault();
                if (adminWindow != null)
                {
                    adminWindow.CloseOverlay();
                }
            });
        }
    }
}
