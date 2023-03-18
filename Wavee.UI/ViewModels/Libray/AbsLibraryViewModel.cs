using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;
using Wavee.Enums;
using Wavee.UI.ViewModels.Shell;

namespace Wavee.UI.ViewModels.Libray
{
    public abstract partial class AbsLibraryViewModel : ObservableObject
    {
        private bool _heartedFilter;
        private bool _hasHeartedFilter;
        private readonly string _type;
        public AbsLibraryViewModel()
        {
            _type = this.GetType().Name;
            HeartedFilter = ShellViewModel.Instance.User.ReadPreference<bool>($"{_type}.hearted_only");
        }

        public bool HeartedFilter
        {
            get => _heartedFilter;
            set
            {
                if (SetProperty(ref _heartedFilter, value))
                {
                    //save user preferences
                    ShellViewModel.Instance.User.SavePreference($"{_type}.hearted_only", value);
                    Initialize(true);
                }
            }
        }

        public bool HasHeartedFilter
        {
            get => _hasHeartedFilter;
            set => SetProperty(ref _hasHeartedFilter, value);
        }
        public ServiceType? Service { get; set; }

        public abstract Task Initialize(bool ignoreAlreadyInitialized = false);
    }
}
