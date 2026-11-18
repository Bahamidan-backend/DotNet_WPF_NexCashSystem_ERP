//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace POSSystem.WPF.Shered.Commands
//{
//    public class AdvancedSettingsCommands
//    {
//        public ICommand SaveSettingsCommand { get; }
//        public ICommand ChangePasswordCommand { get; }
//        public ICommand ChangeImageCommand { get; }
//        public ICommand RemoveImageCommand { get; }
//        public ICommand CancelCommand { get; }

//        public AdvancedSettingsCommands(AdvancedSettingsViewModel vm)
//        {
//            SaveSettingsCommand = new RelayCommand( () => vm.SaveSettings());
//            ChangePasswordCommand = new RelayCommand(() => vm.ChangePasswordFromUI());
//            ChangeImageCommand = new RelayCommand(() => vm.SelectImage());
//            RemoveImageCommand = new RelayCommand(() => vm.RemoveSystemImage());
//            CancelCommand = new RelayCommand(() => vm.CloseWindow(null));
//        }
//    }
//}
