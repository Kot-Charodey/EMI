namespace EMI.MyException
{
    /// <summary>
    /// Возникает при попытки заставить подключиться уже подключённый клиент
    /// </summary>
    public class ClientAlreadyConnectException : System.Exception
    {
        /// <summary>
        /// Возникает при попытки заставить подключиться уже подключённый клиент
        /// </summary>
        public ClientAlreadyConnectException() { }
        /// <summary>
        /// Возникает при попытки заставить подключиться уже подключённый клиент
        /// </summary>
        public ClientAlreadyConnectException(string message) : base(message) { }
    }
}
