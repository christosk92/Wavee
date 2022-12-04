using System.ComponentModel;
using System.Runtime.CompilerServices;
using Eum.UI.Items;
using Eum.UI.ViewModels.Users;

namespace Eum.UI
{
    public class EmptyViewModel : ISignInToXViewModel
    {
        public void OnNavigatedTo(bool isInHistory)
        {
            
        }

        public void OnNavigatedFrom(bool isInHistory)
        {
            
        }

        public bool IsActive { get; set; }
        public ServiceType Service { get; }
        public string? FatalLoginError { get; set; } = null;
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
