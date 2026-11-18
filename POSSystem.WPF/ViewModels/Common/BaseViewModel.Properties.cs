using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace POSSystem.WPF.ViewModels
{
    public abstract partial class BaseViewModel : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected bool _isDisposed;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public virtual void Dispose()
        {
            _isDisposed = true;
        }
    }
}
