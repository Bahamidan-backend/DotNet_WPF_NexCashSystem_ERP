using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Shered.Proberty
{
    public class SettingsProberty:BaseModel
    {
        public static SettingsProberty LoadSettings() => POSSystem.WPF.Services.SettingsService.LoadSettings();
        public void SaveSettings() => POSSystem.WPF.Services.SettingsService.SaveSettings(this);

        private string _companyName = "متجر نقاط البيع";
        private string _companyAddress = "الرياض، المملكة العربية السعودية";
        private string _companyPhone = "+966-50-123-4567";
        private string _companyEmail = "info@posstore.com";
        private decimal _taxRate = 0.0m;
        private bool _taxIncludedInPrice = false;
        private string _receiptHeader = "متجر نقاط البيع";
        private string _receiptFooter = "شكراً لاختيارك متجرنا!";
        private string _currency = "YER";
        private decimal _exchangeRate = 1.0m;
        private bool _autoBackup = true;
        private int _backupInterval = 24; // hours
        private string _backupLocation = @"C:\POSBackup";
        private string _printerName = "XP-80C";
        private bool _printCustomerReceipt = true;
        private bool _printKitchenReceipt = true;
        private bool _printWaiterReceipt = true;
        private bool _darkMode = false;
        private string _language = "ar";
        private bool _notifications = true;
        private bool _soundEnabled = true;
        private int _sessionTimeout = 30; // minutes
        private string _shiftPassword = "1234"; // Default shift password
        private string _logoPath = "";
        private double _cashierScale = 1.0;
        private double _adminScale = 1.0;
        private double _itemFontSize = 14;
        private bool _separateProductSizes = false;
        private string _taxNumber = "300000000000003";
        private string _defaultBankOrWallet = "حاسب كريمي";

        
        private List<string> _customWallets = new List<string>();
        public List<string> CustomWallets
        {
            get => _customWallets;
            set
            {
                _customWallets = value ?? new List<string>();
                OnPropertyChanged();
            }
        }

        private List<string> _customExpenseAccounts = new List<string>();
        public List<string> CustomExpenseAccounts
        {
            get => _customExpenseAccounts;
            set
            {
                _customExpenseAccounts = value ?? new List<string>();
                OnPropertyChanged();
            }
        }

        public string DefaultBankOrWallet
        {
            get => _defaultBankOrWallet;
            set
            {
                _defaultBankOrWallet = value;
                OnPropertyChanged();
            }
        }

        public string TaxNumber
        {
            get => _taxNumber;
            set
            {
                _taxNumber = value;
                OnPropertyChanged();
            }
        }

        public string LogoPath
        {
            get => _logoPath;
            set
            {
                _logoPath = value;
                OnPropertyChanged();
            }
        }

        // Company Information
        public string CompanyName
        {
            get => _companyName;
            set
            {
                _companyName = value;
                OnPropertyChanged();
            }
        }

        public string CompanyAddress
        {
            get => _companyAddress;
            set
            {
                _companyAddress = value;
                OnPropertyChanged();
            }
        }

        public string CompanyPhone
        {
            get => _companyPhone;
            set
            {
                _companyPhone = value;
                OnPropertyChanged();
            }
        }

        public string CompanyEmail
        {
            get => _companyEmail;
            set
            {
                _companyEmail = value;
                OnPropertyChanged();
            }
        }

        public decimal TaxRate
        {
            get => _taxRate;
            set
            {
                _taxRate = value;
                OnPropertyChanged();
            }
        }

        public bool TaxIncludedInPrice
        {
            get => _taxIncludedInPrice;
            set
            {
                _taxIncludedInPrice = value;
                OnPropertyChanged();
            }
        }

        // Receipt Settings
        public string ReceiptHeader
        {
            get => _receiptHeader;
            set
            {
                _receiptHeader = value;
                OnPropertyChanged();
            }
        }

        public string ReceiptFooter
        {
            get => _receiptFooter;
            set
            {
                _receiptFooter = value;
                OnPropertyChanged();
            }
        }

        // Currency Settings
        public string Currency
        {
            get => _currency;
            set
            {
                _currency = value;
                OnPropertyChanged();
            }
        }

        public decimal ExchangeRate
        {
            get => _exchangeRate;
            set
            {
                _exchangeRate = value;
                OnPropertyChanged();
            }
        }

        // Backup Settings
        public bool AutoBackup
        {
            get => _autoBackup;
            set
            {
                _autoBackup = value;
                OnPropertyChanged();
            }
        }

        public int BackupInterval
        {
            get => _backupInterval;
            set
            {
                _backupInterval = value;
                OnPropertyChanged();
            }
        }

        public string BackupLocation
        {
            get => _backupLocation;
            set
            {
                _backupLocation = value;
                OnPropertyChanged();
            }
        }

        // Print Settings
        public bool PrintCustomerReceipt
        {
            get => _printCustomerReceipt;
            set
            {
                _printCustomerReceipt = value;
                OnPropertyChanged();
            }
        }

        public bool PrintKitchenReceipt
        {
            get => _printKitchenReceipt;
            set
            {
                _printKitchenReceipt = value;
                OnPropertyChanged();
            }
        }

        public bool PrintWaiterReceipt
        {
            get => _printWaiterReceipt;
            set
            {
                _printWaiterReceipt = value;
                OnPropertyChanged();
            }
        }

        public bool PrintReceipt
        {
            get => _printCustomerReceipt;
            set
            {
                _printCustomerReceipt = value;
                OnPropertyChanged();
            }
        }

        public bool PrintInvoice
        {
            get => _printCustomerReceipt;
            set
            {
                _printCustomerReceipt = value;
                OnPropertyChanged();
            }
        }

        public string PrinterName
        {
            get => _printerName;
            set
            {
                _printerName = value;
                OnPropertyChanged();
            }
        }

        // System Settings
        public bool DarkMode
        {
            get => _darkMode;
            set
            {
                _darkMode = value;
                OnPropertyChanged();
            }
        }

        public string Language
        {
            get => _language;
            set
            {
                _language = value;
                OnPropertyChanged();
            }
        }

        public bool Notifications
        {
            get => _notifications;
            set
            {
                _notifications = value;
                OnPropertyChanged();
            }
        }

        public bool SoundEnabled
        {
            get => _soundEnabled;
            set
            {
                _soundEnabled = value;
                OnPropertyChanged();
            }
        }

        public int SessionTimeout
        {
            get => _sessionTimeout;
            set
            {
                _sessionTimeout = value;
                OnPropertyChanged();
            }
        }

        public string ShiftPassword
        {
            get => _shiftPassword;
            set
            {
                _shiftPassword = value;
                OnPropertyChanged();
            }
        }

        public double CashierScale
        {
            get => _cashierScale;
            set
            {
                _cashierScale = Math.Round(value, 2);
                OnPropertyChanged();
            }
        }

        public double AdminScale
        {
            get => _adminScale;
            set
            {
                _adminScale = Math.Round(value, 2);
                OnPropertyChanged();
            }
        }

        public double ItemFontSize
        {
            get => _itemFontSize;
            set
            {
                _itemFontSize = Math.Round(value, 0);
                OnPropertyChanged();
            }
        }

        public bool SeparateProductSizes
        {
            get => _separateProductSizes;
            set
            {
                _separateProductSizes = value;
                OnPropertyChanged();
            }
        }
    }
}
