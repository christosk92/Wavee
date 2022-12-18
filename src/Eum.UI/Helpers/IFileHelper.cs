using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Eum.UI.Helpers
{
    public interface IFileHelper
    {
        ValueTask<Stream> GetStreamForString(string? playlistImagePath, CancellationToken cancellationToken);
    }
}
