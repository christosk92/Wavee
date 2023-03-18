using Wavee.UI.Interfaces.Services;

namespace Wavee.UI.Helpers.Extensions
{
    public static class DateTimeExtensions
    {
        public static string CalculateRelativeDateString(
            this DateTime trackLastChanged,
            double minimumSecondsDifference,
            IStringLocalizer localizer)
        {
            var now = DateTime.UtcNow;
            var diff = now - trackLastChanged;
            if (diff.TotalSeconds <= minimumSecondsDifference)
            {
                return Localize("DateTimes/today", -1, localizer);
            }
            if (diff.TotalSeconds < 60 && diff.TotalSeconds > minimumSecondsDifference)
                return Localize("DateTimes/xsecondsago", (int)diff.TotalSeconds, localizer);
            if (diff.TotalMinutes < 60 && diff.TotalSeconds > minimumSecondsDifference)
                return Localize("DateTimes/xminutesago", (int)diff.TotalMinutes, localizer);
            if (diff.TotalHours < 24 && diff.TotalSeconds > minimumSecondsDifference)
                return Localize("DateTimes/xhoursago", (int)diff.TotalHours, localizer);

            switch (diff.TotalDays)
            {
                case < 7 when diff.TotalSeconds > minimumSecondsDifference:
                    var totalDays = (int)diff.TotalDays;
                    return totalDays switch
                    {
                        > 2 => Localize("DateTimes/xdaysago", (int)diff.TotalDays, localizer),
                        > 1 => Localize("DateTimes/yesterday", -1, localizer),
                        _ => Localize("DateTimes/today", -1, localizer)
                    };
                case < 31 when diff.TotalSeconds > minimumSecondsDifference:
                    return Localize("DateTimes/xweeksago", (int)(diff.TotalDays / 7), localizer);
                default:
                    return trackLastChanged.ToString("d");
            }
        }

        private static string Localize(string key, int count, IStringLocalizer localizer)
        {
            //check for plural
            if (count > 1)
            {
                key += "/plural";
            }
            else
            {
                key = key.Replace("x", string.Empty);
            }

            var localized = localizer.GetValue(key);
            return string.Format(localized, count);
        }
    }
}