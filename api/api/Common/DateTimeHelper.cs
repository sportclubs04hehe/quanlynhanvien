using System;

namespace api.Common
{
    /// <summary>
    /// Helper class để xử lý DateTime với timezone SE Asia Standard Time (UTC+7)
    /// </summary>
    public static class DateTimeHelper
    {
        /// <summary>
        /// Timezone của Việt Nam (UTC+7)
        /// </summary>
        public static readonly TimeZoneInfo VietnamTimeZone = 
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        /// <summary>
        /// Convert DateTime từ UTC sang múi giờ Việt Nam
        /// </summary>
        public static DateTime ToVietnamTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                // Nếu không phải UTC, coi như là UTC
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }
            
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, VietnamTimeZone);
        }

        /// <summary>
        /// Convert DateTime từ múi giờ Việt Nam sang UTC
        /// Dùng khi nhận date từ frontend (VD: 2025-11-14) và cần convert sang UTC để lưu DB
        /// </summary>
        public static DateTime ToUtcFromVietnam(DateTime vietnamDateTime)
        {
            // Đảm bảo DateTimeKind là Unspecified để TimeZoneInfo hiểu đây là local time
            if (vietnamDateTime.Kind != DateTimeKind.Unspecified)
            {
                vietnamDateTime = DateTime.SpecifyKind(vietnamDateTime, DateTimeKind.Unspecified);
            }
            
            return TimeZoneInfo.ConvertTimeToUtc(vietnamDateTime, VietnamTimeZone);
        }
    }
}
