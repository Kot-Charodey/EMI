using System.Linq;
using System;
using System.Collections.Generic;

namespace EMI
{
    using Lower.Package;

    /// <summary>
    /// массив группы методов для регистрации и выполняния зарегистрированной группы методов по указаному адресу с результатом или без (null),(индексы массива 0-65535),(метод должен быть публичным, как и клас вкотором он находиться)
    /// </summary>
    public partial class RPC
    {

        /// <summary>
        /// глобальный список вызываймых методов
        /// </summary>
        public static RPC Global { get; private set; } = new RPC();
        private readonly HashSet<MyAction>[] Functions =new  HashSet<MyAction>[ushort.MaxValue];

        internal RPC()
        {
            for(int i = 0; i < ushort.MaxValue; i++)
            {
                Functions[i] = new HashSet<MyAction>();
            }
        }

        internal unsafe PackageToReturnData Execute(byte LVL_Permission, byte* buff)
        {
            Package* package = (Package*)buff;

            var funsLocal= Functions[package->RPC_ID].ToArray();
            var funsGlobal = Global.Functions[package->RPC_ID].ToArray();

            object[] args = new object[package->ArgumentCount];
            int scan = 0;
            IntPtr point = (IntPtr)(buff) + sizeof(Package);
            while (scan < args.Length)
            {
                args[scan++] = point;
                point = (IntPtr)((long)point + ((BitArgument*)point)->Size);
            }

            PackageToReturnData ptrd = new PackageToReturnData();
            if (package->CameBack==PackageCameBack.NeedCameBack) {
                ptrd.NeedReturn = true;
                ptrd.Data = new byte[funsLocal.Length + funsGlobal.Length][];
                byte[] arr;
                int RetSize = sizeof(PackageReturned);

                for (int i = 0; i < funsLocal.Length; i++)
                {
                    if (funsLocal[i].LVL_Permission <= LVL_Permission)
                    {
                        arr = funsLocal[i].MethodInfo.Invoke(funsLocal[i].Context, args) as byte[];
                        ptrd.Data[i] = arr;
                        RetSize += arr.Length + sizeof(BitArgument);
                    }
                }

                for (int i = 0; i < funsGlobal.Length; i++)
                {
                    if (funsGlobal[i].LVL_Permission <= LVL_Permission)
                    {
                        arr = funsGlobal[i].MethodInfo.Invoke(funsGlobal[i].Context, args) as byte[];
                        ptrd.Data[i + funsLocal.Length] = arr;
                        RetSize += arr.Length + sizeof(BitArgument);
                    }
                }

                if (RetSize > ushort.MaxValue)
                {
                    throw new InsufficientMemoryException("Size > 65535");
                }

                ptrd.AllSize = (ushort)RetSize;

                return ptrd;
            }
            else
            {
                for (int i = 0; i < funsLocal.Length; i++)
                {
                    if (funsLocal[i].LVL_Permission <= LVL_Permission)
                    {
                        funsLocal[i].MethodInfo.Invoke(funsLocal[i].Context, args);
                    }
                }

                for (int i = 0; i < funsGlobal.Length; i++)
                {
                    if (funsGlobal[i].LVL_Permission <= LVL_Permission)
                    {
                        funsGlobal[i].MethodInfo.Invoke(funsGlobal[i].Context, args);
                    }
                }

                return ptrd;
            }
        }
    }
}