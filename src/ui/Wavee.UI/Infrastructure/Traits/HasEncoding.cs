using System.Text;
using LanguageExt.Attributes;
using LanguageExt.Effects.Traits;

namespace Wavee.UI.Infrastructure.Traits
{
    /// <summary>
    /// Type-class giving a struct the trait of supporting text encoding IO
    /// </summary>
    /// <typeparam name="RT">Runtime</typeparam>
    [Typeclass("*")]
    public interface HasEncoding<RT> : HasCancel<RT>
        where RT : struct, HasCancel<RT>
    {
        /// <summary>
        /// Access the text encoding
        /// </summary>
        Encoding Encoding { get; }
    }
}