

namespace POSSystem.WPF.Data
{
    public class POSDbContext : DbContext
    {
        public POSDbContext()
        {
        }

        public POSDbContext(DbContextOptions<POSDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Sale> Sales { get; set; } = null!;
        public DbSet<SaleItem> SaleItems { get; set; } = null!;
        public DbSet<Discount> Discounts { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Addon> Addons { get; set; } = null!;
        public DbSet<Warehouse> Warehouses { get; set; } = null!;
        public DbSet<AddonAndOrderItem> addonAndorderItem { get; set; } = null!;
        public DbSet<WarehouseFromProduct>  warehouseFromProducts { get; set; } = null!;
        public DbSet<Recipe>  Recipes { get; set; } = null!;
        public DbSet<ProductSize> ProductSizes { get; set; } = null!;
        public DbSet<EmployeeAdvance> EmployeeAdvances { get; set; } = null!;
        public DbSet<EmployeeSalary> EmployeeSalaries { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<CustomerDebt> CustomerDebts { get; set; } = null!;
        public DbSet<Expense> Expenses { get; set; } = null!;
        public DbSet<ReceiptVoucher> ReceiptVouchers { get; set; } = null!;
        public DbSet<Purchase> Purchases { get; set; } = null!;
        public DbSet<PurchaseItem> PurchaseItems { get; set; } = null!;
        public DbSet<Shift> Shifts { get; set; } = null!;
        public DbSet<CashBoxTransaction> CashBoxTransactions { get; set; } = null!;
        public DbSet<Delivery> Deliveries { get; set; } = null!;
        public DbSet<ReturnOrder> ReturnOrders { get; set; } = null!;
        public DbSet<ReturnOrderItem> ReturnOrderItems { get; set; } = null!;
        
        public DbSet<PurchaseReturn> PurchaseReturns { get; set; } = null!;
        public DbSet<PurchaseReturnItem> PurchaseReturnItems { get; set; } = null!;
        
        public DbSet<Supplier> Suppliers { get; set; } = null!;
        public DbSet<SupplierPayment> SupplierPayments { get; set; } = null!;

        public DbSet<Kitchen> Kitchens { get; set; } = null!;
        public DbSet<AnimalBatch> AnimalBatches { get; set; } = null!;
        public DbSet<BatchComponent> BatchComponents { get; set; } = null!;
        public DbSet<BankDeposit> BankDeposits { get; set; } = null!;
        public DbSet<HeldOrder> HeldOrders { get; set; } = null!;
        public DbSet<StockMovement> StockMovements { get; set; } = null!;
        public DbSet<StockWaste> StockWastes { get; set; } = null!;
        public DbSet<KitchenCookingBatch> KitchenCookingBatches { get; set; } = null!;
        public DbSet<KitchenCookingBatchItem> KitchenCookingBatchItems { get; set; } = null!;
        public DbSet<FinancialAccount> FinancialAccounts { get; set; } = null!;
        public DbSet<FinancialAccountTransaction> FinancialAccountTransactions { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=poss.db;Cache=Shared;Pooling=True;Default Timeout=5;");
            }
        }
        public static string HashPassword(string password)
        {
            return PasswordHasher.HashPassword(password);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>().HasIndex(o => o.OrderNumber).IsUnique();
            modelBuilder.Entity<Order>().HasIndex(o => o.CreatedAt);
            modelBuilder.Entity<Order>().HasIndex(o => o.Status);
            
            modelBuilder.Entity<OrderItem>().HasIndex(oi => oi.ProductId);
            modelBuilder.Entity<OrderItem>().HasIndex(oi => oi.AddonId);
            modelBuilder.Entity<OrderItem>().HasIndex(oi => oi.OrderId);
            
            modelBuilder.Entity<Product>().HasIndex(p => p.CategoryId);
            modelBuilder.Entity<Product>().HasIndex(p => p.Name);
            
            modelBuilder.Entity<Shift>()
                .HasIndex(s => new { s.UserId, s.IsActive })
                .HasFilter("IsActive = 1")
                .IsUnique();

            // ==========================================
            // Additional Performance Indexes (FIX-1)
            // ==========================================

            // CustomerDebts — heavily queried by ShiftId, IsPaid+Date, and CustomerId
            modelBuilder.Entity<CustomerDebt>().HasIndex(d => d.ShiftId);
            modelBuilder.Entity<CustomerDebt>().HasIndex(d => new { d.IsPaid, d.DebtDate });
            modelBuilder.Entity<CustomerDebt>().HasIndex(d => d.CustomerId);

            // CashBoxTransactions — queried by ShiftId and date in every shift report
            modelBuilder.Entity<CashBoxTransaction>().HasIndex(t => t.ShiftId);
            modelBuilder.Entity<CashBoxTransaction>().HasIndex(t => t.TransactionDate);

            // Expenses — queried by UserId and date for expense reports
            modelBuilder.Entity<Expense>().HasIndex(e => e.UserId);
            modelBuilder.Entity<Expense>().HasIndex(e => e.ExpenseDate);

            // AnimalBatches — queried by type and creation date
            modelBuilder.Entity<AnimalBatch>().HasIndex(ab => ab.Type);
            modelBuilder.Entity<AnimalBatch>().HasIndex(ab => ab.CreatedAt);

            // ProductSizes — prevent duplicate (ProductId, SizeName) rows at DB level
            // This eliminates the need for the in-memory DeduplicateSizes() workaround
            modelBuilder.Entity<ProductSize>()
                .HasIndex(ps => new { ps.ProductId, ps.SizeName })
                .IsUnique();

            // ReturnOrders — queried by OriginalOrderId and ReturnDate
            modelBuilder.Entity<ReturnOrder>().HasIndex(r => r.OriginalOrderId);
            modelBuilder.Entity<ReturnOrder>().HasIndex(r => r.ReturnDate);

            // HeldOrders — queried by ShiftId and HoldTime
            modelBuilder.Entity<HeldOrder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Ignore(e => e.Items);
                entity.HasIndex(e => e.HoldTime);
                entity.HasIndex(e => e.ShiftId);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DeliveryFee).HasColumnType("decimal(18,2)");
            });

            // StockMovements — indexed for high performance audit queries
            modelBuilder.Entity<StockMovement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.WarehouseFromProductId);
                entity.HasIndex(e => e.WarehouseId);
                entity.HasIndex(e => e.MovementDate);
                entity.HasIndex(e => e.MovementType);
                entity.Property(e => e.Quantity).HasColumnType("decimal(18,4)");
                entity.Property(e => e.BalanceBefore).HasColumnType("decimal(18,4)");
                entity.Property(e => e.BalanceAfter).HasColumnType("decimal(18,4)");
                entity.Property(e => e.UnitCost).HasColumnType("decimal(18,4)");
                entity.Property(e => e.TotalCost).HasColumnType("decimal(18,4)");
            });

            // StockWastes — indexed for food waste tracking
            modelBuilder.Entity<StockWaste>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.WarehouseFromProductId);
                entity.HasIndex(e => e.WarehouseId);
                entity.HasIndex(e => e.WasteDate);
                entity.Property(e => e.Quantity).HasColumnType("decimal(18,4)");
                entity.Property(e => e.UnitCost).HasColumnType("decimal(18,4)");
                entity.Property(e => e.TotalCost).HasColumnType("decimal(18,4)");
                entity.HasOne(e => e.WarehouseFromProduct)
                    .WithMany()
                    .HasForeignKey(e => e.WarehouseFromProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Recipe — Foreign Key link to WarehouseFromProduct + Fast Lookup Indexes
            modelBuilder.Entity<Recipe>(entity =>
            {
                entity.HasIndex(r => r.ProductId);
                entity.HasIndex(r => r.WarehouseFromProductId);
                entity.HasOne(r => r.WarehouseFromProduct)
                    .WithMany()
                    .HasForeignKey(r => r.WarehouseFromProductId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Additional High-Performance Indexes (Audit Remediation)
            modelBuilder.Entity<CustomerDebt>().HasIndex(d => d.OrderId);
            modelBuilder.Entity<ReceiptVoucher>().HasIndex(v => v.CustomerDebtId);
            modelBuilder.Entity<ReceiptVoucher>().HasIndex(v => v.EmployeeAdvanceId);
            modelBuilder.Entity<EmployeeAdvance>().HasIndex(a => new { a.EmployeeId, a.IsRepaid });
            modelBuilder.Entity<EmployeeSalary>().HasIndex(s => new { s.EmployeeId, s.IsPaid, s.SalaryDate });
            modelBuilder.Entity<ReturnOrderItem>().HasIndex(ri => ri.ReturnOrderId);
            modelBuilder.Entity<ReturnOrderItem>().HasIndex(ri => ri.OriginalOrderItemId);
            modelBuilder.Entity<PurchaseItem>().HasIndex(pi => pi.PurchaseId);
            modelBuilder.Entity<PurchaseItem>().HasIndex(pi => pi.ProductId);
            modelBuilder.Entity<SupplierPayment>().HasIndex(sp => new { sp.SupplierId, sp.PaymentDate });

            // ==========================================

            modelBuilder.Entity<Kitchen>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PrinterName).HasMaxLength(100);
            });

            // Configure relationships
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Kitchen)
                .WithMany(k => k.Categories)
                .HasForeignKey(c => c.KitchenId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Kitchen)
                .WithMany(k => k.Products)
                .HasForeignKey(p => p.KitchenId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    Id = 1,
                    Name = "عام",
                    Description = "تصنيف افتراضي",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1),
                    UpdatedAt = new DateTime(2026, 1, 1)
                }
            );

            modelBuilder.Entity<User>().HasData(

                new User
                {
                    Id=1,
                    Email = "admin@pos.com",
                    Password = HashPassword("admin123"),
                    Name = "Administrator",
                    Role = UserRole.Admin,
                    IsActive = true,
                    MustChangePassword = true,
                    CreatedAt = new DateTime(2026, 1, 1),
                    UpdatedAt = new DateTime(2026, 1, 1)
                },
                new User
                {
                    Id=2,
                    Email = "user@pos.com",
                    Password = HashPassword("user123"),
                    Name = "Cashier User",
                    Role = UserRole.User,
                    IsActive = true,
                    MustChangePassword = true,
                    CreatedAt = new DateTime(2026, 1, 1),
                    UpdatedAt = new DateTime(2026, 1, 1)
                }
            );


            // Configure relationships
            modelBuilder.Entity<Sale>()
                .HasOne(s => s.User)
                .WithMany(u => u.Sales)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SaleItem>()
                .HasOne(si => si.Sale)
                .WithMany(s => s.SaleItems)
                .HasForeignKey(si => si.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SaleItem>()
                .HasOne(si => si.Product)
                .WithMany(p => p.SaleItems)
                .HasForeignKey(si => si.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order relationships
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Delivery)
                .WithOne()
                .HasForeignKey<Order>(o => o.DeliveryID)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Shift)
                .WithMany()
                .HasForeignKey(o => o.ShiftId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Addon)
                .WithMany(a => a.OrderItems)
                .HasForeignKey(oi => oi.AddonId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

          
            modelBuilder.Entity<WarehouseFromProduct>(entity =>
            {
                entity.HasOne(wp => wp.Warehouse)
                    .WithMany(c => c.warehouseFromProducts)
                    .HasForeignKey(wp => wp.WarehouseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.MinimumStockLevel)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(5m);
            });


            modelBuilder.Entity<OrderItem>()
            .HasOne(o => o.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(o => o.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

            // Configure Order entity
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.OrderType).HasMaxLength(20).HasDefaultValue("dinein");
                entity.Property(e => e.SubTotal).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ServiceCharge).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Discount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Total).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxRate).HasColumnType("decimal(18,4)").HasDefaultValue(0.15m);
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,4)").HasDefaultValue(0m);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.HasIndex(e => e.OrderNumber).IsUnique();
            });

            // Configure OrderItem entity
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(500);
            });

            // Configure Product entity
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.BasePrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PersonNumberFood).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CategoryName).HasMaxLength(50);
                entity.Property(e => e.ImagePath).HasMaxLength(200);
            });

            // Configure ProductSize entity
            modelBuilder.Entity<ProductSize>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SizeName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            });

            // Configure ProductSize relationship
            modelBuilder.Entity<ProductSize>()
                .HasOne(ps => ps.Product)
                .WithMany(p => p.ProductSizes)
                .HasForeignKey(ps => ps.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Sale entity
            modelBuilder.Entity<Sale>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.FinalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            });

            // Configure SaleItem entity
            modelBuilder.Entity<SaleItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
            });

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Password).HasMaxLength(255);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.HasIndex(e => e.Email).IsUnique();
            });
            
            // Configure Delivery entity
            modelBuilder.Entity<Delivery>(entity =>
            {
                entity.HasKey(e => e.DeliveryId);
                entity.Property(e => e.DeliveryCustomerName).HasMaxLength(200);
                entity.Property(e => e.DeliveryPhone).HasMaxLength(20);
                entity.Property(e => e.DeliveryAddress).HasMaxLength(500);
                entity.Property(e => e.DeliveryDriverName).HasMaxLength(100);
                entity.Property(e => e.DeliveryFee).HasColumnType("decimal(18,2)");
            });

            // Configure Discount entity
            modelBuilder.Entity<Discount>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountPercentage).HasColumnType("decimal(5,2)");
                entity.HasIndex(e => e.Code).IsUnique();
            });

            // Configure Category entity
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure Addon entity
            modelBuilder.Entity<Addon>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure Addon-Category relationship
            modelBuilder.Entity<Addon>()
                .HasOne(a => a.Category)
                .WithMany(c => c.Addons)
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AddonAndOrderItem>()
                .HasOne(ao => ao.Addon)
                .WithMany(a => a.addonAndOrderItems)
                .HasForeignKey(ao => ao.AddonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Warehouse entity
            modelBuilder.Entity<Warehouse>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Location).HasMaxLength(200);
                entity.Property(e => e.ManagerName).HasMaxLength(100);
                entity.Property(e => e.ContactInfo).HasMaxLength(200);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure EmployeeAdvance entity
            modelBuilder.Entity<EmployeeAdvance>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(500);
            });

            // Configure EmployeeSalary entity
            modelBuilder.Entity<EmployeeSalary>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.BaseSalary).HasColumnType("decimal(18,2)");
                entity.Property(e => e.AdvanceDeduction).HasColumnType("decimal(18,2)");
                entity.Property(e => e.NetSalary).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(500);
            });

            // Configure EmployeeAdvance relationships
            modelBuilder.Entity<EmployeeAdvance>()
                .HasOne(ea => ea.Employee)
                .WithMany(u => u.Advances)
                .HasForeignKey(ea => ea.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure EmployeeSalary relationships
            modelBuilder.Entity<EmployeeSalary>()
                .HasOne(es => es.Employee)
                .WithMany(u => u.Salaries)
                .HasForeignKey(es => es.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

        // Configure User entity - add employee properties
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.BaseSalary).HasColumnType("decimal(18,2)");
            entity.Property(e => e.JobTitle).HasMaxLength(100);
        });

        // Configure Customer entity
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.CreditLimit).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            entity.HasIndex(e => e.PhoneNumber);
            entity.HasIndex(e => e.Name);
        });

        // Configure CustomerDebt entity
        modelBuilder.Entity<CustomerDebt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Notes).HasMaxLength(500);

            // Relationship with Customer
            entity.HasOne(e => e.Customer)
                  .WithMany(c => c.Debts)
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Relationship with Order
            entity.HasOne(e => e.Order)
                  .WithMany()
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Expense entity
            modelBuilder.Entity<Expense>(entity =>
            {
                entity.HasKey(e => e.Id);
                // Ensure DB has a default value for Name to avoid NULL issues
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200).HasDefaultValue("");
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.InvoiceNumber).HasMaxLength(100);
            entity.Property(e => e.SupplierName).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(500);
        });

        // Configure Expense relationships
        modelBuilder.Entity<Expense>()
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure ReceiptVoucher entity
        modelBuilder.Entity<ReceiptVoucher>(entity =>
        {
            entity.HasKey(e => e.Id);
                entity.Property(e => e.VoucherNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(200).HasDefaultValue("");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasIndex(e => e.VoucherNumber).IsUnique();
        });

        // Configure ReceiptVoucher relationships
        modelBuilder.Entity<ReceiptVoucher>()
            .HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ReceiptVoucher>()
            .HasOne(v => v.CustomerDebt)
            .WithMany()
            .HasForeignKey(v => v.CustomerDebtId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ReceiptVoucher>()
            .HasOne(v => v.EmployeeAdvance)
            .WithMany()
            .HasForeignKey(v => v.EmployeeAdvanceId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Purchase entity
        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PurchaseNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SupplierName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.InvoiceNumber).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasIndex(e => e.PurchaseNumber).IsUnique();
        });

        // Configure Purchase relationships
        modelBuilder.Entity<Purchase>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Purchase>()
            .HasOne(p => p.Warehouse)
            .WithMany()
            .HasForeignKey(p => p.WarehouseId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Warehouse-Product relationship (via shadow FK or navigation)
        modelBuilder.Entity<Warehouse>()
            .HasMany(w => w.Products)
            .WithOne()
            .HasForeignKey("WarehouseId")
            .OnDelete(DeleteBehavior.SetNull);

        // Configure PurchaseItem entity
        modelBuilder.Entity<PurchaseItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Notes).HasMaxLength(500);
        });

        // Configure PurchaseItem relationships
        modelBuilder.Entity<PurchaseItem>()
            .HasOne(pi => pi.Purchase)
            .WithMany(p => p.PurchaseItems)
            .HasForeignKey(pi => pi.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseItem>()
            .HasOne(pi => pi.Product)
            .WithMany()
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PurchaseItem>()
            .HasOne(pi => pi.Addon)
            .WithMany()
            .HasForeignKey(pi => pi.AddonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Shift entity
        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ShiftNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StartingCash).HasColumnType("decimal(18,2)");
            entity.Property(e => e.EndingCash).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasIndex(e => e.ShiftNumber).IsUnique();
        });

        // Configure Shift relationships
        modelBuilder.Entity<Shift>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure CashBoxTransaction entity
        modelBuilder.Entity<CashBoxTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(500);
        });

        // Configure CashBoxTransaction relationships
        modelBuilder.Entity<CashBoxTransaction>()
            .HasOne(t => t.Shift)
            .WithMany(s => s.CashBoxTransactions)
            .HasForeignKey(t => t.ShiftId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CashBoxTransaction>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CashBoxTransaction>()
            .HasOne(t => t.EmployeeAdvance)
            .WithMany()
            .HasForeignKey(t => t.EmployeeAdvanceId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Order entity - add PaymentMethod
        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.PaymentMethod).HasConversion<int>();
        });

        // Configure ReturnOrder entity
        modelBuilder.Entity<ReturnOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReturnNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.HasIndex(e => e.ReturnNumber).IsUnique();
        });

        // Configure ReturnOrderItem entity
        modelBuilder.Entity<ReturnOrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ReasonCode).HasMaxLength(100);
        });

        // Configure ReturnOrder relationships
        modelBuilder.Entity<ReturnOrder>()
            .HasOne(ro => ro.OriginalOrder)
            .WithMany(o => o.ReturnOrders)
            .HasForeignKey(ro => ro.OriginalOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReturnOrder>()
            .HasOne(ro => ro.Cashier)
            .WithMany()
            .HasForeignKey(ro => ro.CashierId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure ReturnOrderItem relationships
        modelBuilder.Entity<ReturnOrderItem>()
            .HasOne(roi => roi.ReturnOrder)
            .WithMany(ro => ro.ReturnOrderItems)
            .HasForeignKey(roi => roi.ReturnOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReturnOrderItem>()
            .HasOne(roi => roi.OriginalOrderItem)
            .WithMany(oi => oi.ReturnOrderItems)
            .HasForeignKey(roi => roi.OriginalOrderItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure PurchaseReturn entity
        modelBuilder.Entity<PurchaseReturn>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReturnNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.HasIndex(e => e.ReturnNumber).IsUnique();
        });

        // Configure PurchaseReturnItem entity
        modelBuilder.Entity<PurchaseReturnItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ReasonCode).HasMaxLength(100);
        });

        // Configure PurchaseReturn relationships
        modelBuilder.Entity<PurchaseReturn>()
            .HasOne(pr => pr.OriginalPurchase)
            .WithMany(p => p.PurchaseReturns)
            .HasForeignKey(pr => pr.OriginalPurchaseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseReturn>()
            .HasOne(pr => pr.User)
            .WithMany()
            .HasForeignKey(pr => pr.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure PurchaseReturnItem relationships
        modelBuilder.Entity<PurchaseReturnItem>()
            .HasOne(pri => pri.PurchaseReturn)
            .WithMany(pr => pr.PurchaseReturnItems)
            .HasForeignKey(pri => pri.PurchaseReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseReturnItem>()
            .HasOne(pri => pri.OriginalPurchaseItem)
            .WithMany(pi => pi.PurchaseReturnItems)
            .HasForeignKey(pri => pri.OriginalPurchaseItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Supplier entity
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.VatNumber).HasMaxLength(50);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.TotalBalance).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure SupplierPayment entity
        modelBuilder.Entity<SupplierPayment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        });

        // Supplier - Purchase relationship
        modelBuilder.Entity<Purchase>()
            .HasOne(p => p.Supplier)
            .WithMany(s => s.Purchases)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        // Supplier - SupplierPayment relationship
        modelBuilder.Entity<SupplierPayment>()
            .HasOne(sp => sp.Supplier)
            .WithMany(s => s.SupplierPayments)
            .HasForeignKey(sp => sp.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        // SupplierPayment - User
        modelBuilder.Entity<SupplierPayment>()
            .HasOne(sp => sp.User)
            .WithMany()
            .HasForeignKey(sp => sp.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure AnimalBatch entity
        modelBuilder.Entity<AnimalBatch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.TotalWeight).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RemainingWeight).HasColumnType("decimal(18,2)");
            entity.Property(e => e.BatchDate).IsRequired();
        });

        // Configure AnimalBatch-Warehouse relationship
        modelBuilder.Entity<AnimalBatch>()
            .HasOne(ab => ab.Warehouse)
            .WithMany()
            .HasForeignKey(ab => ab.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure BatchComponent entity
        modelBuilder.Entity<BatchComponent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ComponentType).IsRequired();
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OriginalQuantity).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Unit).HasMaxLength(50);
        });

        // Configure BatchComponent-AnimalBatch relationship
        modelBuilder.Entity<BatchComponent>()
            .HasOne(bc => bc.AnimalBatch)
            .WithMany(ab => ab.Components)
            .HasForeignKey(bc => bc.AnimalBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        
        // Configure FinancialAccount & FinancialAccountTransaction entities
        modelBuilder.Entity<FinancialAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AccountName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.BankOrWalletName).HasMaxLength(150);
            entity.Property(e => e.AccountNumber).HasMaxLength(100);
            entity.Property(e => e.Currency).HasMaxLength(50);
            entity.Property(e => e.CurrentBalance).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalInflow).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalOutflow).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.AccountType);
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<FinancialAccountTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AmountIn).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AmountOut).HasColumnType("decimal(18,2)");
            entity.Property(e => e.BalanceAfter).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.FinancialAccount)
                  .WithMany(a => a.Transactions)
                  .HasForeignKey(e => e.FinancialAccountId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.FinancialAccountId);
            entity.HasIndex(e => e.TransactionDate);
            entity.HasIndex(e => e.TransactionType);
        });

        // Configure BankDeposit entity
        modelBuilder.Entity<BankDeposit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ReferenceNumber).HasMaxLength(100);
            entity.Property(e => e.DepositorName).HasMaxLength(200);
            entity.Property(e => e.Source).HasMaxLength(100);
        });

        }
    }
}
