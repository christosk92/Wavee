using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Eum.UI.WinUI.AttachedBehaviors;
public static class PasswordHelper
{
    // public static readonly DependencyProperty BoundPassword =
    //     DependencyProperty.RegisterAttached("BoundPassword", typeof(string), typeof(PasswordHelper), new PropertyMetadata(string.Empty, OnBoundPasswordChanged));

    public static readonly DependencyProperty BindPassword = DependencyProperty.RegisterAttached(
        "BindPassword", typeof(bool), typeof(PasswordHelper), new PropertyMetadata(false, OnBindPasswordChanged));

    private static readonly DependencyProperty UpdatingPassword =
        DependencyProperty.RegisterAttached("UpdatingPassword", typeof(bool), typeof(PasswordHelper), new PropertyMetadata(false));

    public static readonly DependencyProperty BoundPasswordProperty = DependencyProperty.RegisterAttached("BoundPassword", typeof(string), typeof(PasswordHelper), new PropertyMetadata(default(string), OnBoundPasswordChanged));

    private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        PasswordBox box = d as PasswordBox;
    
        // only handle this event when the property is attached to a PasswordBox
        // and when the BindPassword attached property has been set to true
        if (d == null || !GetBindPassword(d as PasswordBox))
        {
            return;
        }
    
        // avoid recursive updating by ignoring the box's changed event
        box.PasswordChanged -= HandlePasswordChanged;
    
        string newPassword = (string)e.NewValue;
    
        if (!GetUpdatingPassword(box))
        {
            box.Password = newPassword;
        }
    
        box.PasswordChanged += HandlePasswordChanged;
    }

    private static void OnBindPasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
    {
        // when the BindPassword attached property is set on a PasswordBox,
        // start listening to its PasswordChanged event

        PasswordBox box = dp as PasswordBox;

        if (box == null)
        {
            return;
        }

        bool wasBound = (bool)(e.OldValue);
        bool needToBind = (bool)(e.NewValue);

        if (wasBound)
        {
            box.PasswordChanged -= HandlePasswordChanged;
        }

        if (needToBind)
        {
            box.PasswordChanged += HandlePasswordChanged;
        }
    }

    private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
    {
        PasswordBox box = sender as PasswordBox;

        // set a flag to indicate that we're updating the password
        SetUpdatingPassword(box, true);
        // push the new password into the BoundPassword property
        SetBoundPassword(box, box.Password);
        SetUpdatingPassword(box, false);
    }

    public static void SetBindPassword(UIElement dp, bool value)
    {
        dp.SetValue(BindPassword, value);
    }

    public static bool GetBindPassword(UIElement dp)
    {
        return (bool)dp.GetValue(BindPassword);
    }

    // public static string GetBoundPassword(UIElement dp)
    // {
    //     return (string)dp.GetValue(BoundPassword);
    // }
    //
    // public static void SetBoundPassword(UIElement dp, string value)
    // {
    //     dp.SetValue(BoundPassword, value);
    // }

    private static bool GetUpdatingPassword(UIElement dp)
    {
        return (bool)dp.GetValue(UpdatingPassword);
    }

    private static void SetUpdatingPassword(UIElement dp, bool value)
    {
        dp.SetValue(UpdatingPassword, value);
    }

    public static string GetBoundPassword(UIElement element)
    {
        return (string) element.GetValue(BoundPasswordProperty);
    }

    public static void SetBoundPassword(UIElement element, string value)
    {
        element.SetValue(BoundPasswordProperty, value);
    }
}
