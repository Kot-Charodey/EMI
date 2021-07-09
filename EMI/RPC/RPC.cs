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
        public static RPC Global { get; private set; } = new RPC() { IsGlobal = true };
        private readonly HashSet<MyAction>[] Functions = new HashSet<MyAction>[ushort.MaxValue];
        private bool IsGlobal=false;

        internal RPC()
        {
            for (int i = 0; i < ushort.MaxValue; i++)
            {
                Functions[i] = new HashSet<MyAction>();
            }
        }

        internal unsafe byte[] Execute(byte LVL_Permission, byte[] bufferData, ushort RPC_ID, bool NeedReturn, bool guaranteed)
        {
            var funsLocal = Functions[RPC_ID].ToArray();
            var funsGlobal = Global.Functions[RPC_ID].ToArray();

            //если нужер вернуть результат
            if (NeedReturn)
            {
                MyAction action;

                if (funsLocal.Length == 1)
                {
                    action = funsLocal[0];
                }
                else if (funsGlobal.Length == 1)
                {
                    action = funsGlobal[0];
                }
                else //либо функции нет либо мы пропустили несколько функций с возвратом значений
                {
                    if (funsGlobal.Length > 1 || funsLocal.Length > 1)
                    {
                        throw new Exception("Адрес может содержать несколько функций, только если они все с одинаковым набором аргументов и не возвращают значение!");
                    }
                    return null;
                }

                if (action.LVL_Permission <= LVL_Permission)
                {
                    return action.MicroFunct(bufferData, guaranteed);
                }
            }
            else //если надо просто выполнить (не надо отправлять результат)
            {
                if (funsGlobal.Length == 1 && funsGlobal[0].Forwarding)//если это пересылка
                {
                    funsGlobal[0].MicroFunct(bufferData, guaranteed);
                }
                else
                {
                    for (int i = 0; i < funsLocal.Length; i++)
                    {
                        if (funsLocal[i].LVL_Permission <= LVL_Permission)
                        {
                            funsLocal[i].MicroFunct(bufferData, guaranteed);
                        }
                    }

                    for (int i = 0; i < funsGlobal.Length; i++)
                    {
                        if (funsGlobal[i].LVL_Permission <= LVL_Permission)
                        {
                            funsGlobal[i].MicroFunct(bufferData, guaranteed);
                        }
                    }
                }
            }

            return null;
        }
    }
}