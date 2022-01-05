using SmartPackager;

namespace EMI.Indicators
{
    /// <summary>
    /// Ссылка на удалённый метод (для функций которые возвращаю значение)
    /// </summary>
    public class IndicatorR<TOut> : AIndicator
    {
        internal Packager.M<TOut> Output = Packager.Create<TOut>();

        /// <summary>
        /// Создаёт новую ссылку на функцию
        /// </summary>
        /// <param name="factory">фабрика индикаторов (ищи в Client.RPC)</param>
        /// <param name="methodName">имя метода на который ссылается  ссылка - если имя уникальное достаточно написать только имя, иначе (namespace.class.method)</param>
        public IndicatorR(IndicatorsFactory factory, string methodName) : base(factory, methodName)
        {

        }

        /// <summary>
        /// Создаёт новую ссылку на функцию
        /// </summary>
        /// <param name="factory">фабрика индикаторов (ищи в Client.RPC)</param>
        /// <param name="func">функция которая будет вызываться удалённо</param>
        public IndicatorR(IndicatorsFactory factory, RPCfuncOut<TOut> func) : base(factory, func.Method)
        {

        }
    }

    /// <summary>
    /// Ссылка на удалённый метод (для функций которые возвращаю значение)
    /// </summary>
    public class IndicatorR<TOut, T1> : AIndicator
    {
        internal Packager.M<TOut> Output = Packager.Create<TOut>();
        internal Packager.M<T1> Input = Packager.Create<T1>();

        /// <summary>
        /// Создаёт новую ссылку на функцию
        /// </summary>
        /// <param name="factory">фабрика индикаторов (ищи в Client.RPC)</param>
        /// <param name="methodName">имя метода на который ссылается  ссылка - если имя уникальное достаточно написать только имя, иначе (namespace.class.method)</param>
        public IndicatorR(IndicatorsFactory factory, string methodName) : base(factory, methodName)
        {

        }

        /// <summary>
        /// Создаёт новую ссылку на функцию
        /// </summary>
        /// <param name="factory">фабрика индикаторов (ищи в Client.RPC)</param>
        /// <param name="func">функция которая будет вызываться удалённо</param>
        public IndicatorR(IndicatorsFactory factory, RPCfuncOut<TOut, T1> func) : base(factory, func.Method)
        {

        }
    }
}