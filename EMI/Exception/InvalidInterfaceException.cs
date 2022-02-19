namespace EMI.MyException
{
    /// <summary>
    /// Возникает если интерфейс задан неверно
    /// </summary>
    public class InvalidInterfaceException : System.Exception
    {
        /// <summary>
        /// Возникает если интерфейс задан неверно
        /// </summary>
        public InvalidInterfaceException() { }
        /// <summary>
        /// Возникает если интерфейс задан неверно
        /// </summary>
        public InvalidInterfaceException(string message) : base(message) { }
    }
}
