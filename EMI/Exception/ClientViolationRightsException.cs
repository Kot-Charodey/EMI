namespace EMI.MyException
{
    /// <summary>
    /// Возникает при выполнение клиентом недопустимой операции (после данного исключения производиться дисконект пользователя)
    /// </summary>
    public class ClientViolationRightsException : System.Exception
    {
        /// <summary>
        /// Возникает при выполнение клиентом недопустимой операции
        /// </summary>
        public ClientViolationRightsException() { }
        /// <summary>
        /// Возникает при выполнение клиентом недопустимой операции
        /// </summary>
        public ClientViolationRightsException(string message) : base(message) { }
    }
}
