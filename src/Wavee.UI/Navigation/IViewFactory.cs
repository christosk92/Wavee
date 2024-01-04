using Tango.Types;

namespace Wavee.UI.Navigation;

public interface IViewFactory
{
    /// <summary>
    /// A view factory is responsible for creating views from view models.
    /// </summary>
    /// <typeparam name="TViewModel"></typeparam>
    /// <typeparam name="TPage"></typeparam>
    /// <returns>
    /// An option containing the view if it was created successfully.
    /// </returns>
    Option<(Type ViewType, ViewType DisplayType)> ViewType<TViewModel>();
}

public enum ViewType
{
    Page,
    Dialog,
}