using System.Globalization;

namespace SfsTF2ServeMeBot.Modules;

public static class UnixTimestampModule
{
    private static readonly Dictionary<int, int> RegionOffsets = new()
    {
        { 1, -4 },  // US EDT (-4)
        { 2, -5 },  // US EST/CDT (-5)
        { 3, -6 },  // US CST/MDT (-6)
        { 4, -7 },  // US MST/PDT (-7)
        { 5, -8 },  // US PST/AKDT (-8)
        { 6, -9 },  // US AKST (-9)
        { 7, -10 }, // US HST (-10)
        { 8, 1 },   // Europe (+1)
        { 9, 11 },  // South East Asia (+11)
        { 10, 8 }   // Australia (+8)
    };

    public static long ConvertToUnixTimestamp(string date, string time, int region)
    {
        if (!DateTime.TryParseExact($"{date} {time}", "yyyy-MM-dd HH:mm", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime localDateTime))
        {
            throw new ArgumentException("Invalid date or time format.");
        }

        // Get timezone offset from region dictionary, default to UTC if not found
        int offset = RegionOffsets.ContainsKey(region) ? RegionOffsets[region] : 0;

        // Convert to UTC based on the region's offset
        DateTimeOffset dateTimeOffset = new DateTimeOffset(localDateTime, TimeSpan.FromHours(offset));
        return dateTimeOffset.ToUnixTimeSeconds();
    }

    public static string FormatForDiscord(long unixTimestamp, string format = "F")
    {
        return $"<t:{unixTimestamp}:{format}>";
    }
}