using System.Linq;
using System;
using System.Collections.Generic;

namespace EMI
{
    using Lower.Package;

    /// <summary>
    /// ������ ������ ������� ��� ����������� � ���������� ������������������ ������ ������� �� ���������� ������ � ����������� ��� ��� (null),(������� ������� 0-65535),(����� ������ ���� ���������, ��� � ���� � ������� �� ����������)
    /// </summary>
    public partial class RPC
    {

        /// <summary>
        /// ���������� ������ ���������� �������
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

        internal unsafe byte[] Execute(byte LVL_Permission, byte[] bufferData, ushort RPC_ID, bool NeedReturn)
        {
            var funsLocal= Functions[RPC_ID].ToArray();
            var funsGlobal = Global.Functions[RPC_ID].ToArray();

            //���� ����� ������� ���������
            if (NeedReturn)
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
                else //���� ������� ��� ���� �� ���������� ��������� ������� � ��������� ��������
                {
                    if (funsGlobal.Length>1 || funsLocal.Length > 1)
                    {
                        throw new Exception("����� ����� ��������� ��������� �������, ������ ���� ��� ��� � ���������� ������� ���������� � �� ���������� ��������!");
                    }
                    return null;
                }

                if(action.LVL_Permission <= LVL_Permission)
                {
                    return action.MicroFunct(bufferData);
                }
            }
            else //���� ���� ������ ��������� (�� ���� ���������� ���������)
            {
                for (int i = 0; i < funsLocal.Length; i++)
                {
                    if (funsLocal[i].LVL_Permission <= LVL_Permission)
                    {
                        funsLocal[i].MicroFunct(bufferData);
                    }
                }

                for (int i = 0; i < funsGlobal.Length; i++)
                {
                    if (funsGlobal[i].LVL_Permission <= LVL_Permission)
                    {
                        funsGlobal[i].MicroFunct(bufferData);
                    }
                }
            }

            return null;
        }
    }
}