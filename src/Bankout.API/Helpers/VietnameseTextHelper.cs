using System.Globalization;
using System.Text;

namespace Bankout.API.Helpers;

public static class VietnameseTextHelper
{
    public static string ToUppercaseNoAccent(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }
}
