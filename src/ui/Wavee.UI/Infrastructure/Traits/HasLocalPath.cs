using LanguageExt.Attributes;
using LanguageExt.Effects.Traits;

namespace Wavee.UI.Infrastructure.Traits
{
    /// <summary>
    /// Type-class giving a struct the trait of supporting local path
    /// </summary>
    /// <typeparam name="RT">Runtime</typeparam>
    [Typeclass("*")]
    public interface HasLocalPath<RT> : HasCancel<RT>
        where RT : struct, HasCancel<RT>
    {
        /// <summary>
        /// Access the local path
        /// </summary>
        string Path { get; }
    }
}