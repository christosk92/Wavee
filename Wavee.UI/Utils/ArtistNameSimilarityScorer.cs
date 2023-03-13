namespace Wavee.UI.Utils;

public class ArtistNameSimilarityScorer
{
    private const double JaroWinklerThreshold = 0.8;

    public double CalculateScore(string name1, string name2)
    {
        // Normalize the strings by removing special characters and converting to lowercase
        name1 = NormalizeString(name1);
        name2 = NormalizeString(name2);

        // Calculate the Jaro-Winkler Distance between the strings
        var jwDistance = JaroWinklerDistance(name1, name2);

        // Return 1 if the distance is above the threshold, 0 otherwise
        return jwDistance > JaroWinklerThreshold ? 1.0 : 0.0;
    }

    private string NormalizeString(string input)
    {
        // Remove special characters and convert to lowercase
        var normalized = new string(input.Where(c => char.IsLetterOrDigit(c)).ToArray()).ToLowerInvariant();

        // Remove parentheses and everything inside them
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\([^)]*\)", "");

        // Convert the artist name to its romanized form (if applicable)
        var romanized = string.Empty;
        if (input.Contains("(") && input.Contains(")"))
        {
            var start = input.IndexOf("(") + 1;
            var end = input.IndexOf(")");
            var originalName = input.Substring(start, end - start);
            romanized = new string(originalName.Where(c => char.IsLetterOrDigit(c)).ToArray()).ToLowerInvariant();
        }

        // Replace the original form of the artist name with its romanized form (if applicable)
        if (!string.IsNullOrEmpty(romanized))
        {
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, romanized, "");
        }

        // Concatenate the original and romanized forms of the artist name
        normalized = normalized + romanized;

        return normalized;
    }

    private double JaroWinklerDistance(string s1, string s2)
    {
        // Calculate the Jaro Distance between the strings
        var jaroDistance = JaroDistance(s1, s2);

        // Calculate the prefix length (up to a maximum of 4)
        var prefixLength = 0;
        for (int i = 0; i < Math.Min(4, Math.Min(s1.Length, s2.Length)); i++)
        {
            if (s1[i] == s2[i])
            {
                prefixLength++;
            }
            else
            {
                break;
            }
        }

        // Calculate the Jaro-Winkler Distance
        var jwDistance = jaroDistance + 0.1 * prefixLength * (1 - jaroDistance);

        return jwDistance;
    }

    private double JaroDistance(string s1, string s2)
    {
        // Calculate the length of the matching window
        var matchWindow = Math.Max(0, Math.Max(s1.Length, s2.Length) / 2 - 1);

        // Find matching characters in each string
        var s1Matches = new bool[s1.Length];
        var s2Matches = new bool[s2.Length];

        var matchingChars = 0;
        for (int i = 0; i < s1.Length; i++)
        {
            var start = Math.Max(0, i - matchWindow);
            var end = Math.Min(i + matchWindow + 1, s2.Length);

            for (int j = start; j < end; j++)
            {
                if (!s2Matches[j] && s1[i] == s2[j])
                {
                    s1Matches[i] = true;
                    s2Matches[j] = true;
                    matchingChars++;
                    break;
                }
            }
        }

        // Return 0 if there are no matching characters
        if (matchingChars == 0)
        {
            return 0.0;
        }

        // Calculate the number of transpositions
        var transpositions = 0;
        var k = 0;
        for (int i = 0; i < s1.Length; i++)
        {
            if (s1Matches[i])
            {
                while (!s2Matches[k])
                {
                    k++;
                }

                if (s1[i] != s2[k])
                {
                    transpositions++;
                }

                k++;
            }
        }

        // Calculate the Jaro Distance
        var jaroDistance = ((double)matchingChars / s1.Length + (double)matchingChars / s2.Length +
                            (double)(matchingChars - transpositions / 2) / matchingChars) / 3;

        return jaroDistance;
    }
}