namespace EMI
{
    /// <summary>
    /// Необходим при инициализации RPCAdress требуется 1 для клиента и 1 для сервера
    /// </summary>
    public class RPCAddressTable
    {
       internal ushort ID;
    }

    /// <summary>
    /// Адресс для вызова и регистрации функций
    /// </summary>
    public class RPCAddress
    {
        internal ushort ID;

        /// <summary>
        /// Инициализирует адресс
        /// </summary>
        public RPCAddress(RPCAddressTable table)
        {
            lock (table)
            {
                ID=table.ID++;
            }
        }
    }

    /// <summary>
    /// Адресс для вызова и регистрации функций
    /// </summary>
    public class RPCAddress<T1>
    {
        internal ushort ID;

        /// <summary>
        /// Инициализирует адресс
        /// </summary>
        public RPCAddress(RPCAddressTable table)
        {
            lock (table)
            {
                ID=table.ID++;
            }
        }
    }

    /// <summary>
    /// Адресс для вызова и регистрации функций
    /// </summary>
    public class RPCAddressOut<TOut>
    {
        internal ushort ID;

        /// <summary>
        /// Инициализирует адресс
        /// </summary>
        public RPCAddressOut(RPCAddressTable table)
        {
            lock (table)
            {
                ID = table.ID++;
            }
        }
    }

    /// <summary>
    /// Адресс для вызова и регистрации функций
    /// </summary>
    public class RPCAddressOut<TOut, T1>
    {
        internal ushort ID;

        /// <summary>
        /// Инициализирует адресс
        /// </summary>
        public RPCAddressOut(RPCAddressTable table)
        {
            lock (table)
            {
                ID=table.ID++;
            }
        }
    }
}