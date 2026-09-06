using System;

namespace VMSService.Core.Models.WithdrawalRequests
{
    // Shared by the CB- and BOU-facing withdrawal History pages: the resolved (defaulted/clamped)
    // date-range filter actually applied to the query, plus the "from" picker's floor.
    public class DateRangeFilterViewModel
    {
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }

        // Earliest date the "from" picker will accept. DateOnly.MinValue means no real floor
        // (BOU); the CB side passes today - 1 year.
        public DateOnly MinFrom { get; set; } = DateOnly.MinValue;

        /// <summary>Defaults an unset range to the last 7 days, then clamps "from" to
        /// <paramref name="minFrom"/> if given and the caller-selected (or defaulted) "from"
        /// falls before it.</summary>
        public static DateRangeFilterViewModel Resolve(DateOnly today, DateOnly? from, DateOnly? to, DateOnly? minFrom = null)
        {
            var resolvedFrom = from ?? today.AddDays(-7);
            if (minFrom.HasValue && resolvedFrom < minFrom.Value)
                resolvedFrom = minFrom.Value;

            return new DateRangeFilterViewModel
            {
                From = resolvedFrom,
                To = to ?? today,
                MinFrom = minFrom ?? DateOnly.MinValue,
            };
        }
    }
}
