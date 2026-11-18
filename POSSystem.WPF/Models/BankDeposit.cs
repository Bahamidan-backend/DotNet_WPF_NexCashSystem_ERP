using System;

namespace POSSystem.WPF.Models
{
    public class BankDeposit : BaseModel
    {
        private string _status = "Pending";

        public DateTime DepositDate { get; set; }
        public decimal Amount { get; set; }
        public string DepositorName { get; set; } = "";
        public string Source { get; set; } = "";
        public string ReferenceNumber { get; set; } = "";

        public string Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    OnPropertyChanged(nameof(StatusBadgeText));
                }
            }
        }

        public string CreatedBy { get; set; } = "";
        public string? AttachmentFileName { get; set; }
        public string? AttachmentPath { get; set; }
        public string? Notes { get; set; }

        public string StatusBadgeText => Status switch
        {
            "Approved" => "تمت الموافقة",
            "Rejected" => "مرفوض",
            _ => "قيد المراجعة"
        };
    }
}
