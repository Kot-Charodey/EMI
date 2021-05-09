using System.Linq;
using System;
using System.Collections.Generic;

namespace EMI
{
    using Lower.Package;

    /// <summary>
    /// массив группы методов для регистрации и выполнения зарегистрированной группы методов по указанному адресу с результатом или без (null),(индексы массива 0-65535),(метод должен быть публичным, как и клас в котором он находиться)
    /// </summary>
    public partial class RPC
    {

        /// <summary>
        /// глобальный список вызываймых методов
        /// </summary>
        public static RPC Global { get; private set; } = new RPC();
        private readonly HashSet<MyAction>[] Functions = new HashSet<MyAction>[ushort.MaxValue];

        internal RPC()
        {
            for(int i = 0; i < ushort.MaxValue; i++)
            {
                Functions[i] = new HashSet<MyAction>();
            }
        }

        internal unsafe byte[] Execute(byte LVL_Permission, byte[] bufferData, ushort RPC_ID, PacketType packetType)
        {
            int startIndex;

            switch (packetType)
            {
                case PacketType.SndSimple:
                    startIndex = sizeof(BitPacketSimple);
                    break;
                case PacketType.SndGuaranteed:
                    startIndex = sizeof(BitPacketGuaranteed);
                    break;
                case PacketType.SndGuaranteedSegmented: //так как пакет будет собираться то он не будет лежать в структуре
                    startIndex = 0;
                    break;
                default:
                    throw new Exception("Execute -> packetType error!");
            }

            var funsLocal= Functions[RPC_ID].ToArray();
            var funsGlobal = Global.Functions[RPC_ID].ToArray();

            if (packetType == PacketType.SndGuaranteedReturned || packetType == PacketType.SndGuaranteedSegmentedReturned)
            {
                MyAction action;

                if (funsLocal.Length == 1)
                {
                    action = funsLocal[0];
                }
                else if(funsGlobal.Length == 1)
                {
                    action = funsGlobal[0];
                }
                else //либо функции нет либо мы пропустили несколько функций с возвратом значений
                {
                    if (funsGlobal.Length>1 || funsLocal.Length > 1)
                    {
                        throw new Exception("Адрес может содержать несколько функций, только если они все с одинаковым набором аргументов и не возвращают значение!");
                    }
                    return null;
                }

                if(action.LVL_Permission <= LVL_Permission)
                {
                    return action.MicroFunct(bufferData, startIndex);
                }
            }
            else //если надо просто выполнить (не надо отправлять результат)
            {
                for (int i = 0; i < funsLocal.Length; i++)
                {
                    if (funsLocal[i].LVL_Permission <= LVL_Permission)
                    {
                        funsLocal[i].MicroFunct(bufferData, startIndex);
                    }
                }

                for (int i = 0; i < funsGlobal.Length; i++)
                {
                    if (funsGlobal[i].LVL_Permission <= LVL_Permission)
                    {
                        funsLocal[i].MicroFunct(bufferData, startIndex);
                    }
                }
            }

            return null;
        }
    }
}