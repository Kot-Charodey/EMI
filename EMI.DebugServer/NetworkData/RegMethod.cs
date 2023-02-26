namespace EMI.DebugServer.NetworkData
{
    public struct RegMethod
    {
        public int ID;
        public string Name;
        public int Count;

        public RegMethod(int iD, string name, int count)
        {
            ID = iD;
            Name = name;
            Count = count;
        }
    }
}
