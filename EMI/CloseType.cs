namespace EMI
{
    /// <summary>
    /// причина отключения
    /// </summary>
    public enum CloseType:byte
    {
        /// <summary>
        /// Не известно 
        /// </summary>
        None,
        /// <summary>
        /// Я просто отключился
        /// </summary>
        MyNormalStop,
        /// <summary>
        /// Он просто отключился
        /// </summary>
        NormalStop,
        /// <summary>
        /// У меня ошибка подключения
        /// </summary>
        MyStopConnectionError,
        /// <summary>
        /// У него ошибка подключения
        /// </summary>
        StopConnectionError,
        /// <summary>
        /// Мой пакет уже был уничтожен
        /// </summary>
        MyStopPackageDestroyed,
        /// <summary>
        /// Его пакет уже был уничтожен
        /// </summary>
        StopPackageDestroyed,
        /// <summary>
        /// Мне пришёл пакет с ошибкой
        /// </summary>
        MyStopPackageBad,
        /// <summary>
        /// Ему пришёл пакет с ошибкой
        /// </summary>
        StopPackageBad,
    }
}
