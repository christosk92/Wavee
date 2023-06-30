using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Wavee.UI.WinUI.Controls;

public sealed partial class CalendarControl : UserControl
{
    public static readonly DependencyProperty UntilProperty = DependencyProperty.Register(nameof(Until), typeof(DateTimeOffset), typeof(CalendarControl), new PropertyMetadata(default(DateTimeOffset), PropertyChangedCallback));

    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (CalendarControl)d;
        control.Recompute();
    }

    public ObservableCollection<CountdownKey> CountdownKeys { get; } = new();

    DispatcherTimer? timer;
    public CalendarControl()
    {
        this.InitializeComponent();
    }


    private void Recompute()
    {
        timer?.Stop();
        CountdownKeys.Clear();
        var now = DateTimeOffset.Now;
        var until = Until;
        var diff = until - now;
        var days = diff.Days;
        var hours = diff.Hours;
        var minutes = diff.Minutes;
        var seconds = diff.Seconds;
        CountdownKeys.Add(new CountdownKey { Type = CountDownKeyType.Day, Value = days });
        CountdownKeys.Add(new CountdownKey { Type = CountDownKeyType.Hour, Value = hours });
        CountdownKeys.Add(new CountdownKey { Type = CountDownKeyType.Minute, Value = minutes });
        CountdownKeys.Add(new CountdownKey { Type = CountDownKeyType.Second, Value = seconds });
        timer?.Start();
    }
    private void Timer_Tick(object sender, object e)
    {
        var diff = Until - DateTimeOffset.Now;
        var days = diff.Days;
        var hours = diff.Hours;
        var minutes = diff.Minutes;
        var seconds = diff.Seconds;
        foreach (var key in CountdownKeys)
        {
            switch (key.Type)
            {
                case CountDownKeyType.Day:
                    key.Value = days;
                    break;
                case CountDownKeyType.Hour:
                    key.Value = hours;
                    break;
                case CountDownKeyType.Minute:
                    key.Value = minutes;
                    break;
                case CountDownKeyType.Second:
                    key.Value = seconds;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
    public DateTimeOffset Until
    {
        get => (DateTimeOffset)GetValue(UntilProperty);
        set => SetValue(UntilProperty, value);
    }

    private void CalendarControl_OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (timer is not null)
            timer.Tick -= Timer_Tick;
        timer?.Stop();
        timer = null;
    }

    private void CalendarControl_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (timer is null)
        {
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += Timer_Tick;
            timer.Start();
        }
        else
        {
            timer?.Start();
        }
    }
}

public class CountdownKey : ObservableObject
{
    private int _value;
    public CountDownKeyType Type { get; set; }
    public string Key => Type switch
    {
        CountDownKeyType.Day => "Days",
        CountDownKeyType.Hour => "Hours",
        CountDownKeyType.Minute => "Minutes",
        CountDownKeyType.Second => "Seconds",
        _ => throw new NotImplementedException()
    };

    public int Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}

public enum CountDownKeyType
{
    Day,
    Hour,
    Minute,
    Second
}