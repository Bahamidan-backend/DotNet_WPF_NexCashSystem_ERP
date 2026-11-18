using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Shered.Proberty
{
    public class AdvancedSettingsProberty:BaseModel
    {

        private string _fullName = string.Empty;
        private string _email = string.Empty;
        private string _phone = string.Empty;
        private string _role = string.Empty;
        private string _currentPassword = string.Empty;
        private string _department = string.Empty;
        private string _newPassword = string.Empty;
        private string _confirmNewPassword = string.Empty;
        private BitmapImage? _currentSystemImage;
        private string? _selectedImagePath;

        public string FullName
        {
            get => _fullName;
            set
            {
                _fullName = value;
                OnPropertyChanged();
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged();
            }
        }

        public string Phone
        {
            get => _phone;
            set
            {
                _phone = value;
                OnPropertyChanged();
            }
        }

        public string Role
        {
            get => _role;
            set
            {
                _role = value;
                OnPropertyChanged();
            }
        }

        public string Department
        {
            get => _department;
            set
            {
                _department = value;
                OnPropertyChanged();
            }
        }



        public string CurrentPassword
        {
            get => _currentPassword;
            set
            {
                _currentPassword = value;
                OnPropertyChanged();
            }
        }

        public string NewPassword
        {
            get => _newPassword;
            set
            {
                _newPassword = value;
                OnPropertyChanged();
            }
        }

        public string ConfirmNewPassword
        {
            get => _confirmNewPassword;
            set
            {
                _confirmNewPassword = value;
                OnPropertyChanged();
            }
        }

      

        public BitmapImage? CurrentSystemImage
        {
            get => _currentSystemImage;
            set
            {
                _currentSystemImage = value;
                OnPropertyChanged();
            }
        }

        public string? SelectedImagePath
        {
            get => _selectedImagePath;
            set
            {
                _selectedImagePath = value;
                OnPropertyChanged();
            }
        }



    }
}
