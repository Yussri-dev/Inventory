namespace Inventory.Domain.Barcodes
{
    public static class EanTools
    {
        public static string Normalize(string code)
        {
            code = new string(code.Where(char.IsDigit).ToArray());

            if (code.Length == 12)
                return code + CalculateChecksum(code);

            if (code.Length == 13)
            {
                var expected = CalculateChecksum(code[..12]);
                return code[..12] + expected;
            }

            return code;
        }

        private static int CalculateChecksum(string digits)
        {
            int sum = 0;

            for (int i = 0; i < digits.Length; i++)
            {
                int n = digits[i] - '0';
                sum += (i % 2 == 0) ? n : n * 3;
            }

            return (10 - (sum % 10)) % 10;
        }
    }
}
