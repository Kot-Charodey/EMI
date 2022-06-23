namespace EMI
{
    internal static class Deterministic
    {
        /// <summary>
        /// Возвращает хеш код строки текста (детермизированный [одинаковый для одной и той же строки на всех устройсвах])
        /// </summary>
        /// <param name="str">строка текста для которой необходимо расчитать хеш код</param>
        /// <returns>хеш код строки текста</returns>
        public static int DeterministicGetHashCode(this string str)
        {
            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1)
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }
    }
}
