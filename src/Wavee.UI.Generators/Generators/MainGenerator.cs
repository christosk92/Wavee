using Microsoft.CodeAnalysis;
using Wavee.UI.Generators.Abstractions;

namespace Wavee.UI.Generators.Generators;

[Generator]
internal class MainGenerator : CombinedGenerator
{
	public MainGenerator()
	{
		AddStaticFileGenerator<AutoNotifyAttributeGenerator>();
		Add<AutoNotifyGenerator>();
	}
}
