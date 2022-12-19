using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData.Kernel;
using Microsoft.UI.Xaml;

namespace Eum.UI.WinUI.Behaviors;
internal class AvaloniaPropertyObservable<T> : LightweightObservableBase<T>, IDescription
{
    private long _propertyChangedCallbackId;
    private DependencyObject _target;
    private readonly DependencyProperty _property;
    private Optional<T> _value;

    public AvaloniaPropertyObservable(
        DependencyObject target,
        DependencyProperty property)
    {
        _target = target;
        _property = property;
    }

    public string Description => $"{_target.GetType().Name}.{_property.GetType().Name}";

    protected override void Initialize()
    {
        _value = (T)_target.GetValue(_property)!;
        _propertyChangedCallbackId = _target.RegisterPropertyChangedCallback(_property, PropertyChanged);
    }


    protected override void Deinitialize()
    {

        _target.UnregisterPropertyChangedCallback(_property, _propertyChangedCallbackId);

        _value = default;
        _target = null;
    }

    protected override void Subscribed(IObserver<T> observer, bool first)
    {
        if (_value.HasValue)
            observer.OnNext(_value.Value);
    }

    private void PropertyChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (dp == _property)
        {
            T newValue = (T)sender.GetValue(dp);

            if (!_value.HasValue ||
                !EqualityComparer<T>.Default.Equals(newValue, _value.Value))
            {
                _value = newValue;
                PublishNext(_value.Value!);
            }
        }
    }
}

