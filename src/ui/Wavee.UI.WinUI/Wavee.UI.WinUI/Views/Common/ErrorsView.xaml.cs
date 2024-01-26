using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Library;


namespace Wavee.UI.WinUI.Views.Common;

public sealed partial class ErrorsView : UserControl
{
    public static readonly DependencyProperty ErrorsProperty = DependencyProperty.Register(nameof(Errors),
        typeof(INotifyCollectionChanged), typeof(ErrorsView),
        new PropertyMetadata(default(INotifyCollectionChanged), PropertyChangedCallback));


    private readonly DispatcherQueue _dispatcherQueue;
    public ErrorsView()
    {
        this.InitializeComponent();
        _dispatcherQueue = this.DispatcherQueue;
        RetryCommand = new RelayCommand<Action>(action => action());
    }
    private void ReRender()
    {
        var firstError = (Errors as ObservableCollection<ExceptionForProfile>)?.FirstOrDefault();
        if (firstError is null)
        {
            ErrorDescription.Text = string.Empty;
            RetryButton.IsEnabled = false;
            return;
        }

        ErrorDescription.Text = firstError.Error.Message;
        RetryButton.IsEnabled = firstError.Retry is not null;
        RetryButton.CommandParameter = firstError.Retry;
    }
    public INotifyCollectionChanged Errors
    {
        get => (INotifyCollectionChanged)GetValue(ErrorsProperty);
        set => SetValue(ErrorsProperty, value);
    }

    public ICommand RetryCommand { get; }

    private void ErrorsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(ReRender);
    }

    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var x = (ErrorsView)d;
        x.SourceChanged(e.OldValue, e.NewValue);
    }

    private void SourceChanged(object eOldValue, object eNewValue)
    {
        if (eOldValue is INotifyCollectionChanged old)
        {
            old.CollectionChanged -= ErrorsOnCollectionChanged;
        }

        ReRender();

        if (eNewValue is INotifyCollectionChanged _new)
        {
            _new.CollectionChanged += ErrorsOnCollectionChanged;
        }
    }
    private void ErrorsView_OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (Errors is not null)
        {
            Errors.CollectionChanged -= ErrorsOnCollectionChanged;
        }

        Errors = null;
    }

    private void ErrorsView_OnLoaded(object sender, RoutedEventArgs e)
    {
        ReRender();
    }
}