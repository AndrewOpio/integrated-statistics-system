namespace VMSService.Core.Static
{
    public static class ReferenceNumberGenerator
    {
        public static string MonthlyShortage(int year, int month, int sequence)
        {
            var period = new DateTime(year, month, 1);
            return $"MS-{period:MMMyyyy}-{sequence:D3}".ToUpper();
        }

        public static string BankingShortage(int year, int month, int sequence)
        {
            var period = new DateTime(year, month, 1);
            return $"BS-{period:MMMyyyy}-{sequence:D3}".ToUpper();
        }

        public static string MonthlySurplus(int year, int month, string branchCode, int sequence)
        {
            var period = new DateTime(year, month, 1);
            return $"MSP-{period:MMMyyyy}-{branchCode.ToUpper()}-{sequence:D3}";
        }

        public static string BankingSurplus(int year, int month, string branchCode, int sequence)
        {
            var period = new DateTime(year, month, 1);
            return $"BSP-{period:MMMyyyy}-{branchCode.ToUpper()}-{sequence:D3}";
        }

        public static string ShortageReimbursement(DateTime period, int branchId)
        {
            return $"MSR-{period:MMMyyyy}-{branchId:D3}".ToUpper();
        }
    }
}
