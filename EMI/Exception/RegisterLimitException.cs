namespace EMI.MyException
{
    /// <summary>
    /// Возникает если зарегестрированно больше 2^16 функций
    /// </summary>
    public class RegisterLimitException : System.Exception
    {
        /// <summary>
        /// Возникает если зарегестрированно больше 2^16 функций
        /// </summary>
        public RegisterLimitException() { }
        /// <summary>
        /// Возникает если зарегестрированно больше 2^16 функций
        /// </summary>
        public RegisterLimitException(string message) : base(message) { }
    }
}
