namespace EMI.MyException
{
    /// <summary>
    /// Возникает если зарегестрированно больше 2^16 функций
    /// </summary>
    public class RPCRegisterLimitException : System.Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public RPCRegisterLimitException() { }
        /// <summary>
        /// 
        /// </summary>
        public RPCRegisterLimitException(string message) : base(message) { }
    }
}
