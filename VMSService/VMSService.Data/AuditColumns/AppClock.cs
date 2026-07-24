using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMSService.Data.AuditColumns
{
    public static class AppClock
    {
        private static readonly TimeZoneInfo Eat =
            TimeZoneInfo.FindSystemTimeZoneById("Africa/Kampala"); // Windows: "E. Africa Standard Time"

        // Ugandan time, derived from UTC so it's correct regardless of server zone.
        public static DateTime Now =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Eat);
    }
}
