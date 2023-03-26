using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.Enums;

namespace Wavee.UI.WinUI.TemplateSelectors;
internal sealed class RepeatStateButtonContentSelector : DataTemplateSelector
{
    public DataTemplate None
    {
        get;
        set;
    }
    public DataTemplate Context
    {
        get;
        set;
    }
    public DataTemplate Track
    {
        get;
        set;
    }
    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is not RepeatState r)
        {
            return null;
        }

        return r switch
        {
            RepeatState.None => None,
            RepeatState.Context => Context,
            RepeatState.Track => Track,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
