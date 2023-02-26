namespace EMI.DebugServer.NetworkData
{
    public struct NGCInfo
    {
        public int UseArrays;
        public long TotalUseSize;
        public int FreeArraysCount;
        public long TotalFreeArraysSize;

        public void Create()
        {
            FreeArraysCount = NGC.NGCArray.FreeArraysCount;
            TotalFreeArraysSize = NGC.NGCArray.TotalFreeArraysSize;
            TotalUseSize = NGC.NGCArray.TotalUseSize;
            UseArrays = NGC.NGCArray.UseArrays;
        }
    }
}
