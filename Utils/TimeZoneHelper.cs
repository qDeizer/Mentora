namespace PsikologProje_Void.Utils
{
    public static class TimeZoneHelper
    {
        public static TimeZoneInfo GetTurkeyTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
            }
        }

        public static DateTime GetTurkeyNow()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GetTurkeyTimeZone());
        }

        public static DateTime ToTurkeyTime(DateTime utcDateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, GetTurkeyTimeZone());
        }
    }
}
