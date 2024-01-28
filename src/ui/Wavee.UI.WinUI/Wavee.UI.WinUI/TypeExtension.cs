using Microsoft.UI.Xaml.Markup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wavee.UI.WinUI;

[MarkupExtensionReturnType(ReturnType = typeof(Type))]
public sealed class TypeExtension : MarkupExtension
{
    public Type Type { get; set; }

    /// <inheritdoc/>
    protected override object ProvideValue() => Type;
}