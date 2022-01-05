namespace EMI.MyException
{
    /// <summary>
    /// Возникает если функция с таким именем занята
    /// </summary>
    public class RPCRegisterNameException : System.Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public RPCRegisterNameException() { }
        /// <summary>
        /// 
        /// </summary>
        public RPCRegisterNameException(string message) : base(message) { }
    }
}
