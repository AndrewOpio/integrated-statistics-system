namespace VMSService.Core.Services.Submissions
{
    public enum SubmissionClassification
    {
        Standard,
        SameDayExceptional,
        LateInvalid
    }

    public static class SubmissionClassifier
    {
        private static readonly TimeZoneInfo EatZone =
            TimeZoneInfo.FindSystemTimeZoneById("E. Africa Standard Time");

        /// <summary>
        /// Classifies a withdrawal submission based on approval timestamp vs collection date.
        /// </summary>
        /// <param name="approvalUtc">The approval timestamp in UTC.</param>
        /// <param name="collectionDate">The requested collection date.</param>
        /// <param name="isDayBeforeHoliday">
        /// Whether the calendar day immediately before the collection date is a public holiday.
        /// // TODO: This is a simplified boolean flag. A future implementation may need a
        /// per-date holiday lookup (e.g. a config table or holiday service) rather than a
        /// single Boolean passed at call site.
        /// </param>
        public static SubmissionClassification Classify(
            DateTime approvalUtc,
            DateOnly collectionDate,
            bool isDayBeforeHoliday)
        {
            var eat = TimeZoneInfo.ConvertTimeFromUtc(approvalUtc, EatZone);
            var approvalDate = DateOnly.FromDateTime(eat);
            var approvalTime = TimeOnly.FromDateTime(eat);

            if (approvalDate > collectionDate) return SubmissionClassification.LateInvalid;
            if (approvalDate < collectionDate) return SubmissionClassification.Standard;

            // Same day — apply grace-window logic
            var dayBefore = collectionDate.AddDays(-1);
            bool dayBeforeIsNonWorking =
                dayBefore.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                || isDayBeforeHoliday;

            // Grace window: if the day before was non-working, approvals at or before
            // 11:00:00 EAT are treated as standard (the requestor had no prior working day).
            // Strictly after 11:00:00 is exceptional even within the grace window.
            if (dayBeforeIsNonWorking && approvalTime <= new TimeOnly(11, 0, 0))
                return SubmissionClassification.Standard;

            return SubmissionClassification.SameDayExceptional;
        }
    }
}
