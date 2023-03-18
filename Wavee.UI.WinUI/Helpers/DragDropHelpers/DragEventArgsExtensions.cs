using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using Windows.Storage;

namespace Wavee.UI.WinUI.Helpers.DragDropHelpers
{
    internal static class DragEventArgsExtensions
    {
        public static void AllowDrag(this DragEventArgs e, string text, DataPackageOperation dataPackageOperation)
        {
            e.AcceptedOperation = dataPackageOperation;
            e.DragUIOverride.Caption = text; // Sets custom UI text
            e.DragUIOverride.IsCaptionVisible = true; // Sets if the caption is visible
            e.DragUIOverride.IsContentVisible = true; // Sets if the dragged content is visible
            e.DragUIOverride.IsGlyphVisible = true; // Sets if the glyph is visibile
        }

        public static async Task<IStorageItem> GetStorageItemAsync(this DragEventArgs e)
        {
            if (!e.DataView.Contains(StandardDataFormats.StorageItems))
                return null;

            var items = await e.DataView.GetStorageItemsAsync();
            switch (items.Count)
            {
                case > 1:
                case 0:
                    return null;
                default:
                    {
                        var storageItem = items[0];
                        return storageItem;
                    }
            }
        }
        public static async Task<IReadOnlyList<IStorageItem>> GetStorageItemsAsync(this DragEventArgs e)
        {
            if (!e.DataView.Contains(StandardDataFormats.StorageItems))
                return null;

            var items = await e.DataView.GetStorageItemsAsync();
            return items;
        }
    }
}
