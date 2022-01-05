namespace EMI
{
    /// <summary>
    /// Информация о том как следует вызвать метод
    /// </summary>
    public enum RPCInvokeInfo
    {
        /// <summary>
        /// Просто вызвать
        /// </summary>
        Simple,
        /// <summary>
        /// Обеспечить гарантию доставки/вызвова (иначе пакет может потеряться если такое предусмотренно текущим INetworkClient)
        /// </summary>
        Guarantee,
        /// <summary>
        /// Гарантия доставки + ожидать сообщения о том что функция выполнена или произошла ошибка (функция не найдена)
        /// </summary>
        GuaranteeAndWaitReturn,
    }

}