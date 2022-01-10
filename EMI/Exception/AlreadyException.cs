namespace EMI.MyException
{
    /// <summary>
    /// Возникает если действие уже было выполнено
    /// </summary>
    public class AlreadyException : System.Exception
    {
        /// <summary>
        /// Возникает если действие уже было выполнено
        /// </summary>
        public AlreadyException() { }
        /// <summary>
        /// Возникает если действие уже было выполнено
        /// </summary>
        public AlreadyException(string message) : base(message) { }
    }
}
