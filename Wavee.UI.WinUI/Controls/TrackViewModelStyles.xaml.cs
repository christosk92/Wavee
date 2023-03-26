using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wavee.UI.WinUI.Controls
{
    partial class TrackViewModelStyles : ResourceDictionary
    {
        public TrackViewModelStyles()
        {
            this.InitializeComponent();
        }

        public bool IsNull(object? obj)
        {
            return obj is null;
        }
    }
}
