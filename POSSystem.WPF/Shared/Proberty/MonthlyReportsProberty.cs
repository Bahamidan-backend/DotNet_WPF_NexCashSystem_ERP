using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Shered.Proberty
{
    public class MonthlyReportsProberty:BaseModel
    {
        private bool _isLoading = false;


        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // Employee Report
        public int TotalEmployees { get;  set; }
        public decimal TotalSalaries { get;  set; }
        public decimal TotalAdvances { get;  set; }

        // Sales Report
        public decimal TotalSales { get;  set; }
        public int SalesCount { get;      set; }
        public string? TopProduct { get;  set; }

        // Customer Report
        public decimal TotalDebts { get;   set; }
        public int DebtCount { get;        set; }
        public int RegularCustomers { get; set; }

        // Expense Report
        public decimal TotalExpenses { get;  set; }
        public Dictionary<string, decimal> ExpensesByCategory { get;  set; } = new();

        // Financial Report
        public decimal TotalRevenue { get; set; }
        public decimal TotalCosts { get; set; }
        public decimal NetProfit { get;  set; }

    }
}
