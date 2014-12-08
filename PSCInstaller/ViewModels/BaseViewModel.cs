using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using System.Reflection;

namespace PSCInstaller.ViewModels
{
    public class BaseViewModel 
        :   INotifyPropertyChanged, 
            IDisposable
    {
        public static readonly bool _isDesignMode;
        public static bool IsDesignMode { get { return _isDesignMode; } }

        static BaseViewModel()
        {
            var prop = DesignerProperties.IsInDesignModeProperty;
            if (prop != null)
            {
                _isDesignMode = (bool)prop.DefaultMetadata.DefaultValue;
            }
        }

        public BaseViewModel()
        {
            if (IsDesignMode)
                SetDesignerProperties();
        }

        public virtual async Task Initialize()
        {
            await Task.Yield();
        }

        public virtual void Dispose()
        {

        }

        protected virtual void SetDesignerProperties(){ }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetProperty<T>(ref T value, T newValue, [CallerMemberNameAttribute] string propertyName = "")
        {
            var prop = this.GetType().GetProperties().FirstOrDefault(p => p.Name == propertyName);
            if (prop == null)
                throw new InvalidOperationException(string.Format("property {0} is undefined", propertyName));

            if (value != null)
            {
                bool implementsInterface = false;
                foreach (var item in value.GetType().GetInterfaces())
                {
                    if (item == prop.PropertyType)
                    {
                        implementsInterface = true;
                        break;
                    }
                }

                if (!implementsInterface && (value.GetType() != prop.PropertyType))
                    throw new InvalidOperationException(string.Format("property {0} is not of type {1}", propertyName, value.GetType().Name));
            }

            //Set Property
            value = newValue;
        
            var handler = PropertyChanged;
            if (handler != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void UpdateUIThreadSafe(Action action)
        {
            var localAction = action;
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                localAction();
            });
        }
    }
}
