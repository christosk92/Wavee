
using System;
using System.ComponentModel;
using Eum.UI.ViewModels.Playlists;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Eum.UI.WinUI;

public class PermissionLevelToButtonsSelector : DataTemplateSelector
{
    public DataTemplate Public { get; set; }
    public DataTemplate Owner { get; set; }
    public DataTemplate Collab { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            OwnerButtons => Owner,
            ViewerButtons => Public,
            CollabButtons => Collab,
            _ => base.SelectTemplateCore(item, container)
        };
    }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return item switch
        {
            OwnerButtons => Public,
            ViewerButtons => Owner,
            CollabButtons => Collab,
            _ => base.SelectTemplateCore(item)
        };
    }
}