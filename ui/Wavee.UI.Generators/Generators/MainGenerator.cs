using Microsoft.CodeAnalysis;
using Wavee.UI.Generators.Abstractions;

namespace Wavee.UI.Generators.Generators;

[Generator]
internal class MainGenerator : CombinedGenerator
{
    public MainGenerator()
    {
        // AddStaticFileGenerator<AutoInterfaceAttributeGenerator>();
        AddStaticFileGenerator<AutoNotifyAttributeGenerator>();
        Add<AutoNotifyGenerator>();
        // Add<AutoInterfaceGenerator>();
        Add<UiContextConstructorGenerator>();
        Add<FluentNavigationGenerator>();
    }
}