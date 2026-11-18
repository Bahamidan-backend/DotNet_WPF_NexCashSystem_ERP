using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace POSSystem.WPF.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;
        
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        
        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        
        public void Execute(object? parameter) => _execute();
        
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
    
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;
        
        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        
        private static bool TryGetTypedParameter(object? parameter, out T typed)
        {
            if (parameter is T valid)
            {
                typed = valid;
                return true;
            }
            if (parameter is null)
            {
                typed = default!;
                return default(T) is null;
            }
            try
            {
                var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                if (targetType.IsEnum && parameter is string enumStr)
                {
                    typed = (T)Enum.Parse(targetType, enumStr, ignoreCase: true);
                    return true;
                }
                typed = (T)Convert.ChangeType(parameter, targetType);
                return true;
            }
            catch
            {
                typed = default!;
                return false;
            }
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecute == null) return true;
            return TryGetTypedParameter(parameter, out var typed) && _canExecute(typed);
        }
        
        public void Execute(object? parameter)
        {
            if (TryGetTypedParameter(parameter, out var typed))
            {
                _execute(typed);
            }
            else if (parameter is null)
            {
                _execute(default!);
            }
        }
        
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
