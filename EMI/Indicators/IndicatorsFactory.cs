namespace EMI.Indicators
{
    using MyException;

    /// <summary>
    /// Позволяет создать ссылку для вызова удалённого метода
    /// </summary>
    public class IndicatorsFactory
    {
        private readonly bool[] UsedID = new bool[ushort.MaxValue];

        internal IndicatorsFactory()
        {

        }

        /// <summary>
        /// Ищет свободный айди для регистрации функции
        /// </summary>
        /// <returns></returns>
        internal ushort GetID()
        {
            lock (this)
            {
                ushort id = 0;
                while (UsedID[id])
                {
                    if (id == ushort.MaxValue)
                        throw new RegisterLimitException();
                    id++;
                }
                return id;
            }
        }
    }
}