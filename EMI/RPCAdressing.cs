using System.Collections.Generic;

namespace EMI
{
    internal class RPCAdressing
    {
        internal struct AddressInfo
        {
            public ushort ID;
            public string Name;
        }

        internal ushort?[] ReadresingID = new ushort?[ushort.MaxValue];
        internal List<AddressInfo> AddressInfos = new List<AddressInfo>(500);


        internal void Reinit()
        {
            lock (this)
            {
                AddressInfos = new List<AddressInfo>();
                ReadresingID = new ushort?[ushort.MaxValue];
            }
        }

        /// <summary>
        /// Пытается сопоставить имя и записать переадресацию в ReadresingID
        /// </summary>
        /// <param name="FromID">Какой ID надо сопоставить</param>
        /// <param name="name">С каким именем надо сопоставлять</param>
        /// <returns></returns>
        internal bool DoReaddressing(ushort FromID, string name)
        {
            lock (this)
            {
                if (name.IndexOf('.') != -1)//если имя полное
                {
                    for (int i = 0; i < AddressInfos.Count; i++)
                    {
                        var info = AddressInfos[i];
                        if (info.Name == name)
                        {
                            if (ReadresingID[FromID] != null)
                                throw new System.Exception(); //что то сломалось

                            ReadresingID[FromID] = info.ID;
                        }
                    }
                }
                else
                {
                    //поиск функции только по названию
                    for (int i = 0; i < AddressInfos.Count; i++)
                    {
                        var info = AddressInfos[i];
                        if (info.Name.EndsWith(name))
                        {
                            if (ReadresingID[FromID] != null)
                                throw new System.Exception(); //что то сломалось

                            ReadresingID[FromID] = info.ID;
                        }
                    }
                }
                return false;
            }
        }


        internal void Remove(ushort ID)
        {
            lock (this)
            {
                for (int i = 0; i < ReadresingID.Length; i++)
                {
                    if (ReadresingID[i] == ID)
                    {
                        ReadresingID[i] = null;
                    }
                }

                for (int i = 0; i < AddressInfos.Count; i++)
                {
                    if (AddressInfos[i].ID == ID)
                    {
                        AddressInfos.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        internal void Add(ushort ID, string Name)
        {
            lock (this)
            {
                AddressInfos.Add(new AddressInfo() { ID = ID, Name = Name });
            }
        }
    }
}