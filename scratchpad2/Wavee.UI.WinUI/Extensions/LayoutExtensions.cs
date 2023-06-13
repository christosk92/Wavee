using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Wavee.UI.WinUI.Extensions;

public static class LayoutExtensions
{
    public static async Task UpdateLayoutAsync(this FrameworkElement element, bool update = false)
    {
        var tcs = new TaskCompletionSource<bool>();
        void layoutUpdated(object s1, object e1)
        {
            tcs.TrySetResult(true);
        }

        try
        {
            element.LayoutUpdated += layoutUpdated;

            if (update)
            {
                element.UpdateLayout();
            }

            await tcs.Task;
        }
        finally
        {
            element.LayoutUpdated -= layoutUpdated;
        }
    }
}