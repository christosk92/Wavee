using CommunityToolkit.Mvvm.ComponentModel;
using Eum.UI.Services.Albums;
using Eum.UI.ViewModels.Navigation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Eum.UI.ViewModels.Home
{
    [INotifyPropertyChanged]
    public sealed partial class HomeViewModel : INavigatable
    {
        public void OnNavigatedTo(object parameter)
        {
            
        }

        public void OnNavigatedFrom()
        {
           
        }

        public int MaxDepth { get; }
    }
}
