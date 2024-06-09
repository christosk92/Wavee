using System.Collections.Generic;

namespace Wavee.UI.Generators.Abstractions;

internal abstract class StaticFileGenerator
{
	public abstract IEnumerable<(string FileName, string Source)> Generate();
}
