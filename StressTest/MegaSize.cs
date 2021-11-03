namespace StressTest
{
    public static class MegaSize
    {
        static string[] meg = { "", "Кило", "Мега", "Гига", "Тера" };

        public static string ForByte(double num)
        {
            int i;
            for (i = 0; i < meg.Length && num >= 1024; i++)
            {
                num /= 1024;
            }
            return $"{num} {meg[i]}Байт";
        }

        public static string ForBit(double num)
        {
            num = num * 8;
            int i;
            for (i = 0; i < meg.Length && num >= 1000; i++)
            {
                num /= 1000;
            }
            return $"{num} {meg[i]}Бит";
        }
    }
}