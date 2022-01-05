using System.Reflection;

namespace EMI.Indicators
{
    /// <summary>
    /// Ссылка на удалённый метод
    /// </summary>
    public abstract class AIndicator
    {
        /// <summary>
        /// Айди индикатора (не метода)
        /// </summary>
        protected internal ushort ID;
        /// <summary>
        /// Имя удалённого метода
        /// </summary>
        protected internal string Name;

        /// <summary>
        /// Создаёт новую ссылку на функцию
        /// </summary>
        /// <param name="factory">фабрика индикаторов (ищи в Client.RPC)</param>
        /// <param name="methodName">имя метода на который ссылкается ссылка - если имя уникальное достаточно написать только имя, иначе (namespace.class.method)</param>
        internal AIndicator(IndicatorsFactory factory, string methodName)
        {
            ID = factory.GetID();
            Name = methodName;
        }

        /// <summary>
        /// Создаёт новую ссылку на функцию
        /// </summary>
        /// <param name="factory">фабрика индикаторов (ищи в Client.RPC)</param>
        /// <param name="method"></param>
        internal AIndicator(IndicatorsFactory factory, MethodInfo method)
        {
            ID = factory.GetID();
            Name = $"{method.DeclaringType.FullName}.{method.Name}";
        }
    }
}