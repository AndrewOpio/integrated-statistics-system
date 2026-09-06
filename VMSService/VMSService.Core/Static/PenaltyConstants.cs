namespace VMSService.Core.Static
{
    public static class PenaltyConstants
    {
        public static class AppliesTo
        {
            public const string PreTransaction = "Pre-Transaction";
            public const string PointOfTransaction = "At the Point of Transaction";
            public const string Processing = "Processing";
        }

        public static class Source
        {
            public const string Withdrawal = "WITHDRAWAL";
            public const string Deposit = "DEPOSIT";
            public const string Processing = "PROCESSING";
        }

        /// <summary>Exact Penalty.Name values for penalties applied automatically by the system rather
        /// than manually checked in the Manage Penalties modal.</summary>
        public static class AutomaticPenalties
        {
            public const string MissingPieces = "Discrepancies in deposits — missing pieces";
            public const string DenominationMixUp = "Mix-up of denominations within bundles";
            public const string FitUnfitMixUp = "Fit notes in unfit notes and vice versa";

            // Withdrawals — names match withdrawal-shared-concepts.md's penalty matrix verbatim.
            public const string LateSubmissionOfLetterOfIntent = "Late submission of letter of intent";
            public const string LateSubmissionOfRtgsToTransact = "Late submission of RTGS to transact";
            public const string TransactingLate = "Transacting late (after 3pm)";
            public const string FailureToAmendInTime = "Failure to amend in time";
            public const string ErrorOnLetterOfIntentAndRtgs = "Error on the letter of intent and RTGS";
        }
    }
}
