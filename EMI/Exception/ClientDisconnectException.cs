namespace EMI.MyException
{
    /// <summary>
    /// Возникает при отключении клиента во время долгой/блокируйщей операции для её прерывания
    /// </summary>
    class ClientDisconnectException : System.Exception
    {
        /// <summary>
        /// Возникает при отключении клиента
        /// </summary>
        public ClientDisconnectException() { }
        /// <summary>
        /// Возникает при отключении клиента
        /// </summary>
        public ClientDisconnectException(string message) : base(message) { }
    }
}
