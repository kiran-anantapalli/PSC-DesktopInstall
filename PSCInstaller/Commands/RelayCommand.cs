using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PSCInstaller.Commands
{
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _executeAction;
        private readonly Predicate<T> _canExecuteAction;

        public RelayCommand(Action<T> execute, Predicate<T> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute cannot be null");
            if (canExecute == null)
                throw new ArgumentNullException("canExecute cannot be null");

            _executeAction = execute;
            _canExecuteAction = canExecute;
        }

        public RelayCommand(Action<T> execute)
        {
            _executeAction = execute;
            _canExecuteAction = null;
        }

        public bool CanExecute(object parameter)
        {
            var handler = _canExecuteAction;
            if (handler != null)
                return handler((T)parameter);
            return true;
        }

        public void Execute(object parameter)
        {
            var handler = _executeAction;
            if (handler != null)
                handler((T)parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
