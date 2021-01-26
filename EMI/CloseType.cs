namespace EMI
{
    public enum CloseType:byte
    {
        StopConnectionLimit,
        StopConnectionError,
        StopChangedConnectionLimit,
        StopPackageDestroyed,
        Stop
    }
}
