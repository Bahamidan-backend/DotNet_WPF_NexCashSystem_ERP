

namespace POSSystem.WPF.Models
{
    public class BaseModel:BaseViewModel
    {
        public int Id { get; set; }
      
        public string? Name { get; set; }
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        //[Required]
        //public bool IsActive { get; set; } = false;



        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                        // Use the base OnPropertyChanged without parameter so CallerMemberName
                        // will supply the correct property name "IsActive" to listeners.
                        OnPropertyChanged();
                }
            }
        }

      

    }
}
