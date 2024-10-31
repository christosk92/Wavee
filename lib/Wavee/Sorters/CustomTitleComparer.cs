using Wavee.Playback.Player;

namespace Wavee.Sorters;

public class CustomTitleComparer : IComparer<WaveePlayerMediaItem>
{
    public int Compare(WaveePlayerMediaItem x, WaveePlayerMediaItem y)
    {
        // Extract and process titles
        var titleX = ProcessTitleForSorting(x.Metadata.TryGetValue("name", out var t1) ? t1 : string.Empty);
        var titleY = ProcessTitleForSorting(y.Metadata.TryGetValue("name", out var t2) ? t2 : string.Empty);

        // Categorize titles
        var categoryX = GetCategory(titleX);
        var categoryY = GetCategory(titleY);

        // Compare categories
        int categoryComparison = categoryX.CompareTo(categoryY);
        if (categoryComparison != 0)
        {
            return categoryComparison;
        }

        // If categories are the same, compare titles
        int titleComparison = string.Compare(titleX, titleY, StringComparison.OrdinalIgnoreCase);
        if (titleComparison != 0)
        {
            return titleComparison;
        }

        // If titles are the same, compare original_index
        int indexX = int.TryParse(x.Metadata.TryGetValue("original_index", out var idx1) ? idx1 : "0", out var ix) ? ix : 0;
        int indexY = int.TryParse(y.Metadata.TryGetValue("original_index", out var idx2) ? idx2 : "0", out var iy) ? iy : 0;

        return indexX.CompareTo(indexY);
    }

    private static string ProcessTitleForSorting(string title)
    {
        // Remove leading articles
        title = RemoveLeadingArticles(title);
        // Trim and convert to uppercase for consistency
        return title.Trim().ToUpperInvariant();
    }

    private static string RemoveLeadingArticles(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        string[] articles = { "A ", "AN ", "THE " };
        foreach (var article in articles)
        {
            if (input.StartsWith(article, StringComparison.OrdinalIgnoreCase))
            {
                return input.Substring(article.Length).TrimStart();
            }
        }
        return input;
    }

    private static int GetCategory(string title)
    {
        if (string.IsNullOrEmpty(title))
            return 0; // Category for empty titles

        char firstChar = title[0];

        if (char.IsLetter(firstChar))
        {
            return 3; // Letters A-Z
        }
        else if (char.IsDigit(firstChar))
        {
            return 2; // Digits and numerals
        }
        else if (!char.IsPunctuation(firstChar) && !char.IsLetterOrDigit(firstChar))
        {
            return 1; // Non-alphanumeric characters (excluding punctuation)
        }
        else
        {
            return 4; // Other characters
        }
    }
}