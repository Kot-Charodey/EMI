namespace EMI
{
    /// <summary>
    /// Возникает при отключении клиента
    /// </summary>
    class ClientOutException : System.Exception
    {
        /// <summary>
        /// Возникает при отключении клиента
        /// </summary>
        public ClientOutException() { }
        /// <summary>
        /// Возникает при отключении клиента
        /// </summary>
        public ClientOutException(string message) : base(message) { }
    }
}
