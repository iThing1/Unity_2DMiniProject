public static class MathUtility
{
    private static readonly string[] Suffixes = { "", "K", "M", "B", "T" };

    public static string ToAbbreviatedString(this float value, int digits = 1)
    {
        if (value <= 0) return "0";

        int suffixIndex = 0;
        double num = value;
            
        while (num >= 1000d && suffixIndex < Suffixes.Length - 1)
        {
            num /= 1000d;
            suffixIndex++;
        }

        return $"{num.ToString($"f{digits}")}{Suffixes[suffixIndex]}";
    }
}