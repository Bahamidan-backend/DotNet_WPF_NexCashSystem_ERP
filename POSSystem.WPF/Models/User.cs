namespace POSSystem.WPF.Models
{
    public class User
    {
        public int Id { get; set; }
        
        public string? Email { get; set; } = string.Empty;
        
        public string? Password { get; set; } = string.Empty;
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Phone { get; set; }
        
        public UserRole Role { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        public bool IsActive { get; set; } = false;
        
        // Security properties
        public bool MustChangePassword { get; set; } = false;
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockedUntil { get; set; }
        
        // Employee properties
        public decimal? BaseSalary { get; set; }
        public string? JobTitle { get; set; }  // المسمى الوظيفي
        public DateTime? HireDate { get; set; }  // تاريخ التعيين

        // Granular Permissions & Role Template (Feature 4)
        public string? RoleTemplateName { get; set; }
        public string? PermissionsJson { get; set; }
        
        // Navigation properties for employee management
        public List<EmployeeAdvance> Advances { get; set; } = new List<EmployeeAdvance>();
        public List<EmployeeSalary> Salaries { get; set; } = new List<EmployeeSalary>();
        
        // Additional properties for UI display
        public string RoleText => !string.IsNullOrWhiteSpace(RoleTemplateName)
            ? RoleTemplateName
            : (Role switch
            {
                UserRole.Admin => "مدير",
                UserRole.User => "موظف",
                _ => "غير محدد"
            });
        
        // Navigation properties
        public List<Order> Orders { get; set; } = new List<Order>();
        public List<Sale> Sales { get; set; } = new List<Sale>();
        
        // Additional properties for UI display
        public string CustomerName => Name; // For compatibility with OrderDetailsWindow
        public string DisplayName => Name;
        public string WelcomeMessage => $"مرحباً، {Name}!";
        public string RoleDisplayName => RoleText;
        public string FullName => Name;
        public string ShortName => Name.Length > 20 ? Name.Substring(0, 20) + "..." : Name;
        public string Initials => Name.Length > 0 ? Name.Substring(0, 1).ToUpper() : "U";
        public string AvatarColor => Role == UserRole.Admin ? "#dc3545" : "#01922a";
        public string StatusText => IsActive ? "نشط" : "غير نشط";
    }
    
    public enum UserRole
    {
        Admin,
        User
    }
}
