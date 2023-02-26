namespace EMI.DebugServer
{
    using NetworkData;
    using EMI.Indicators;

    public enum DebuggerType
    {
        Server,
        Client,
    }

    public static class EDebuggerBothRC
    {
        public static readonly Indicator.FuncOut<NGCInfo> GetNGCInfo = new Indicator.FuncOut<NGCInfo>("GetNGCInfo");
        public static readonly Indicator.Func<MSG> OnMSG = new Indicator.Func<MSG>("OnMSG");
        /// <summary>
        /// Вызываеться при подключении и ожидает заверешния настройки отладчика - отсылает тип отлаживаемого компонента
        /// </summary>
        public static readonly Indicator.Func<DebuggerType> InitDebugger = new Indicator.Func<DebuggerType>("InitDebugger");
    }
}
