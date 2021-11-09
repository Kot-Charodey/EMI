namespace EMI.Lower
{
    /// <summary>
    /// Хранит информацию о потерянных пакетах
    /// </summary>
    internal struct LostPackageInfo
    {
        public ulong ID;
        public bool IsSegment;

        public LostPackageInfo(ulong iD, bool isSegment)
        {
            ID = iD;
            IsSegment = isSegment;
        }
    }
}