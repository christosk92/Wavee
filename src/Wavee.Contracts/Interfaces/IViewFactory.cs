namespace Wavee.Contracts.Interfaces;

public interface IViewFactory
{
    object CreateView(object viewModel);
}