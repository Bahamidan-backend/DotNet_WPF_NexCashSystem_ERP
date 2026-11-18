using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;
using POSSystem.WPF.Services.Logging;

namespace POSSystem.WPF.Business.Services
{
    public class HeldOrderService : IHeldOrderService
    {
        private readonly IUnitOfWork _unitOfWork;

        public HeldOrderService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<HeldOrder>> GetAllHeldOrdersAsync()
        {
            try
            {
                await using var db = new POSDbContext();
                return await db.HeldOrders
                    .AsNoTracking()
                    .OrderByDescending(o => o.HoldTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Error fetching held orders from database", ex);
                return Enumerable.Empty<HeldOrder>();
            }
        }

        public async Task<IEnumerable<HeldOrder>> GetHeldOrdersByShiftAsync(int shiftId)
        {
            try
            {
                await using var db = new POSDbContext();
                return await db.HeldOrders
                    .AsNoTracking()
                    .Where(o => o.ShiftId == shiftId)
                    .OrderByDescending(o => o.HoldTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Error fetching held orders for shift #{shiftId}", ex);
                return Enumerable.Empty<HeldOrder>();
            }
        }

        public async Task<HeldOrder?> GetHeldOrderByIdAsync(Guid id)
        {
            try
            {
                await using var db = new POSDbContext();
                return await db.HeldOrders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Error fetching held order #{id}", ex);
                return null;
            }
        }

        public async Task<HeldOrder> SaveHeldOrderAsync(HeldOrder heldOrder)
        {
            try
            {
                heldOrder.SerializeItems();

                await using var db = new POSDbContext();
                var existing = await db.HeldOrders.FirstOrDefaultAsync(o => o.Id == heldOrder.Id);
                if (existing != null)
                {
                    existing.CustomerName = heldOrder.CustomerName;
                    existing.CustomerPhone = heldOrder.CustomerPhone;
                    existing.OrderType = heldOrder.OrderType;
                    existing.DiscountAmount = heldOrder.DiscountAmount;
                    existing.DiscountCode = heldOrder.DiscountCode;
                    existing.HasDiscount = heldOrder.HasDiscount;
                    existing.TotalAmount = heldOrder.TotalAmount;
                    existing.DeliveryAddress = heldOrder.DeliveryAddress;
                    existing.DeliveryDriver = heldOrder.DeliveryDriver;
                    existing.DeliveryFee = heldOrder.DeliveryFee;
                    existing.ShiftId = heldOrder.ShiftId;
                    existing.UserId = heldOrder.UserId;
                    existing.ItemsJson = heldOrder.ItemsJson;
                    existing.HoldTime = heldOrder.HoldTime;

                    db.HeldOrders.Update(existing);
                }
                else
                {
                    await db.HeldOrders.AddAsync(new HeldOrder
                    {
                        Id = heldOrder.Id,
                        CustomerName = heldOrder.CustomerName,
                        CustomerPhone = heldOrder.CustomerPhone,
                        OrderType = heldOrder.OrderType,
                        DiscountAmount = heldOrder.DiscountAmount,
                        DiscountCode = heldOrder.DiscountCode,
                        HasDiscount = heldOrder.HasDiscount,
                        TotalAmount = heldOrder.TotalAmount,
                        DeliveryAddress = heldOrder.DeliveryAddress,
                        DeliveryDriver = heldOrder.DeliveryDriver,
                        DeliveryFee = heldOrder.DeliveryFee,
                        ShiftId = heldOrder.ShiftId,
                        UserId = heldOrder.UserId,
                        ItemsJson = heldOrder.ItemsJson,
                        HoldTime = heldOrder.HoldTime
                    });
                }

                await db.SaveChangesAsync();
                AppLogger.LogInfo($"[HELD ORDER] Successfully saved held order #{heldOrder.Id} for customer '{heldOrder.CustomerName}' (Amount: {heldOrder.TotalAmount:N0})");
                return heldOrder;
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Error saving held order #{heldOrder.Id}", ex);
                throw;
            }
        }

        public async Task<bool> DeleteHeldOrderAsync(Guid id)
        {
            try
            {
                await using var db = new POSDbContext();
                int deleted = await db.HeldOrders
                    .Where(o => o.Id == id)
                    .ExecuteDeleteAsync();

                if (deleted > 0)
                {
                    AppLogger.LogInfo($"[HELD ORDER] Successfully removed held order #{id}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Error deleting held order #{id}", ex);
                return false;
            }
        }

        public async Task<int> ClearAllHeldOrdersAsync()
        {
            try
            {
                await using var db = new POSDbContext();
                int count = await db.HeldOrders.ExecuteDeleteAsync();
                AppLogger.LogInfo($"[HELD ORDER] Cleared all {count} held orders");
                return count;
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Error clearing all held orders", ex);
                return 0;
            }
        }
    }
}
