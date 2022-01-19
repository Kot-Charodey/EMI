namespace EMI.Indicators
{
    /// <summary>
    /// Указатель на функцию
    /// </summary>
    public class FunctionRef
    {
        /// <summary>
        ///  Указатель на функцию
        /// </summary>
        /// <param name="name">полное имя функции [namespace.class.method]</param>
        public FunctionRef(string name)
        {
            Ref = name;
        }

        private protected FunctionRef()
        {
        }

        internal string Ref { get; private protected set; }
    }

    /// <summary>
    /// Указатель на функцию (упрощённый)
    /// </summary>
    /// <typeparam name="Class">класс содержащий функцию</typeparam>
    public class FunctionRef<Class> : FunctionRef where Class: class
    {
        /// <summary>
        ///  Указатель на функцию
        /// </summary>
        /// <param name="name">имя функции [method]</param>
        public FunctionRef(string name)
        {
            Ref = typeof(Class).FullName + "." + name;
        }
    }
}