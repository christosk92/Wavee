using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Wavee.UI.Features.Dialog.Queries;

namespace Wavee.UI.WinUI.Dialogs.QueryHandlers;

public sealed class PromptDeviceSelectionQueryHandler : IQueryHandler<PromptDeviceSelectionQuery, PromptDeviceSelectionResult>
{
    private readonly IServiceProvider _serviceProvider;

    public PromptDeviceSelectionQueryHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async ValueTask<PromptDeviceSelectionResult> Handle(PromptDeviceSelectionQuery query, CancellationToken cancellationToken)
    {
        var mainWindow = MainWindow.Instance;
        var dialog = _serviceProvider.GetRequiredService<DeviceSelectionDialog>();
        dialog.XamlRoot = mainWindow.Content.XamlRoot;
        await dialog.ShowAsync();

        var result = await dialog.Result;

        return result;
    }
}