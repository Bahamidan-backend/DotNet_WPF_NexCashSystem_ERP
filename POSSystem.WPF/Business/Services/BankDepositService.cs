using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace POSSystem.WPF.Business.Services
{
    public class BankDepositService : IBankDepositService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BankDepositService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<BankDeposit>> GetAllDepositsAsync()
        {
            return await _unitOfWork.GetContext().BankDeposits
                .AsNoTracking()
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<BankDeposit?> GetDepositByIdAsync(int id)
        {
            return await _unitOfWork.GetContext().BankDeposits
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<BankDeposit> CreateDepositAsync(BankDeposit deposit)
        {
            if (deposit == null) throw new ArgumentNullException(nameof(deposit));
            if (deposit.Amount <= 0) throw new ArgumentException("مبلغ الإيداع البنكي يجب أن يكون أكبر من الصفر.", nameof(deposit));
            var context = _unitOfWork.GetContext();
            var tracked = context.ChangeTracker.Entries<BankDeposit>().ToList();
            foreach (var entry in tracked)
            {
                entry.State = EntityState.Detached;
            }

            await _unitOfWork.BankDeposits.AddAsync(deposit);
            await _unitOfWork.SaveChangesAsync();
            context.Entry(deposit).State = EntityState.Detached;
            return deposit;
        }

        public async Task UpdateDepositAsync(BankDeposit deposit)
        {
            if (deposit == null) throw new ArgumentNullException(nameof(deposit));
            if (deposit.Amount <= 0) throw new ArgumentException("مبلغ الإيداع البنكي يجب أن يكون أكبر من الصفر.", nameof(deposit));
            var context = _unitOfWork.GetContext();
            var existingTracked = context.ChangeTracker.Entries<BankDeposit>()
                .FirstOrDefault(e => e.Entity.Id == deposit.Id);

            if (existingTracked != null && !ReferenceEquals(existingTracked.Entity, deposit))
            {
                existingTracked.State = EntityState.Detached;
            }

            _unitOfWork.BankDeposits.Update(deposit);
            await _unitOfWork.SaveChangesAsync();
            context.Entry(deposit).State = EntityState.Detached;
        }

        public async Task DeleteDepositAsync(int id)
        {
            await _unitOfWork.GetContext().BankDeposits
                .Where(d => d.Id == id)
                .ExecuteDeleteAsync();
            _unitOfWork.ClearEntityTracking();
        }

        public async Task<IEnumerable<BankDeposit>> GetDepositsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var endExclusive = endDate.Date.AddDays(1);

            return await _unitOfWork.GetContext().BankDeposits
                .AsNoTracking()
                .Where(d => d.DepositDate >= start && d.DepositDate < endExclusive)
                .OrderByDescending(d => d.DepositDate)
                .ToListAsync();
        }
    }
}
