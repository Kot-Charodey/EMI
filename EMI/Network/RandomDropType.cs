namespace EMI.Network
{
    /// <summary>
    /// Что делать при перегрузке сети
    /// </summary>
    public enum RandomDropType
    {
        /// <summary>
        /// Нечего
        /// </summary>
        Nothing,
        /// <summary>
        /// Негарантированные пакеты могут быть не отправлены
        /// </summary>
        NoGuaranteed,
        /// <summary>
        /// Любой пакет может быть не отправлен (гарантированные будут ожидать отправки)
        /// </summary>
        All,
    }
}
