namespace EMI.MyException
{
    /// <summary>
    /// Возникает если действие уже было выполнено
    /// </summary>
    public class ClientAlreadyException : System.Exception
    {
        /// <summary>
        /// Возникает если действие уже было выполнено
        /// </summary>
        public ClientAlreadyException() { }
        /// <summary>
        /// Возникает если действие уже было выполнено
        /// </summary>
        public ClientAlreadyException(string message) : base(message) { }
    }
}
