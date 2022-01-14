namespace EMI
{
    /// <summary>
    /// Позволяет удалить зарегистрированный метод
    /// </summary>
    public interface IRPCRemoveHandle
    {
        /// <summary>
        /// Удаляет метод из списка зарегестрированных (его больше нельзя буджет вызвать)
        /// </summary>
        void Remove();
    }
}