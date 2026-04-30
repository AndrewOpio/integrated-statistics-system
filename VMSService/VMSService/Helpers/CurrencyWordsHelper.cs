namespace VMSService.Web.Helpers
{
    public static class CurrencyWordsHelper
    {
        private static readonly string[] Ones =
        [
            "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
        "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
        "Seventeen", "Eighteen", "Nineteen"
        ];

        private static readonly string[] Tens =
        [
            "", "", "Twenty", "Thirty", "Forty", "Fifty",
        "Sixty", "Seventy", "Eighty", "Ninety"
        ];

        private static string WordsUnder1000(long n)
        {
            if (n == 0) return "";
            if (n < 20) return Ones[n];
            if (n < 100) return Tens[n / 10] + (n % 10 > 0 ? " " + Ones[n % 10] : "");
            return Ones[n / 100] + " Hundred" + (n % 100 > 0 ? " " + WordsUnder1000(n % 100) : "");
        }

        public static string ToWords(decimal amount)
        {
            var n = (long)Math.Floor(amount);
            if (n == 0) return "Zero Shillings Only";

            var scales = new (long Scale, string Name)[]
            {
            (1_000_000_000_000, "Trillion"),
            (1_000_000_000,     "Billion"),
            (1_000_000,         "Million"),
            (1_000,             "Thousand"),
            (1,                 "")
            };

            var res = "";
            var rem = n;
            foreach (var (scale, name) in scales)
            {
                if (rem >= scale)
                {
                    var chunk = rem / scale;
                    res += WordsUnder1000(chunk) + (name != "" ? " " + name + " " : " ");
                    rem %= scale;
                }
            }

            return res.Trim() + " Shillings Only";
        }
    }
}
