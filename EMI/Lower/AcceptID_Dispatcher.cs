using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMI.Lower
{
    using Accepter;
    using Buffer;
    using Package;

    /// <summary>
    /// Отвечает:
    /// * исключение повторения отправки данных
    /// * ведёт список потерянных пакетов
    /// * управляет очисткой PacketSendBuffer
    /// </summary>
    internal class AcceptID_Dispatcher
    {
        //ссылки на компоненты клиента что бы отправлять запросы о доставке
        private readonly IMyAccepter Accepter;
        private readonly SendID_Dispatcher SendID;
        private readonly PacketSendBuffer PacketSendBuffer;

        public AcceptID_Dispatcher(IMyAccepter accepter, SendID_Dispatcher sendID, PacketSendBuffer packetSendBuffer)
        {
            Accepter = accepter;
            SendID = sendID;
            PacketSendBuffer = packetSendBuffer;
        }

        /// <summary>
        /// последний принятый ID
        /// </summary>
        private ulong ReqID = 0;
        /// <summary>
        /// Список потерянных пакетов [key - ID, value - isSegment]
        /// </summary>
        public Dictionary<ulong, bool> LostID = new Dictionary<ulong, bool>();

        /// <summary>
        /// Добавляет в список потерянные пакеты и будит поток по их поиску (не указывает что пакет сегментный - это должен делать SndGuaranteedSegmented и SndGuaranteedSegmentedReturned)
        /// </summary>
        /// <param name="startID">ID первого пропущенного пакета</param>
        /// <param name="endID">ID последнего пропущенного пакета</param>
        private void LostPackagesStartFind(ulong startID, ulong endID)
        {
            for (ulong i = startID; i <= endID; i++)
            {
                if (!LostID.ContainsKey(i))
                    LostID.Add(i, false);
            }
        }

        private void SendSndDeliveryСompletedPackage(ulong completID)
        {
            ulong sndID = SendID.GetNewID();
            var bp = new BitPackageDeliveryСompleted()
            {
                FullID = completID,
                ID = sndID,
                PacketType = PacketType.SndDeliveryСompletedPackage,
            };


            byte[] buffer = Packagers.SndDeliveryСompletedPackage.PackUP(bp);
            Task.Run(async () => await PacketSendBuffer.Storing(sndID, buffer));

            Accepter.Send(buffer, buffer.Length);
        }

        /// <summary>
        /// Возвращает ID пакета который должен быть доставлен следуйщим
        /// </summary>
        /// <returns>ID</returns>
        public ulong GetID()
        {
            return ReqID;
        }

        /// <summary>
        /// Проверяет потерян ли пакет и доставлен ли он уже
        /// Выполняет процесс по поиску потерянных пакетов
        /// </summary>
        /// <param name="ID">айди пакета</param>
        /// <returns>если true то пакет пришёл в первые</returns>
        public bool CheckPacket(ulong ID)
        {
            lock (this)
            {
                //если пакет новый
                if (ID == ReqID)
                {
                    ReqID++;
                    SendSndDeliveryСompletedPackage(ID);
                    return true;
                }

                if (ID > ReqID)//если пакет слишком новый
                {
                    LostPackagesStartFind(ReqID, ID - 1);
                    ReqID = ID + 1;
                    SendSndDeliveryСompletedPackage(ID);
                    return true;
                }
                else//если пакет устарел
                {
                    if (LostID.ContainsKey(ID))//если это потерянный пакет
                    {
                        LostID.Remove(ID);
                        return true;
                    }
                    else//иначе он уже приходил
                    {
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// Просто проверяет доставлен ли данный пакет (нужен для сегментных пакетов)
        /// </summary>
        /// <param name="ID">айди пакета</param>
        /// <returns>если true то пакет пришёл в первые</returns>
        public bool PreCheckPacketSegment(ulong ID)
        {
            lock (this)
            {
                bool contains = LostID.TryGetValue(ID, out bool isSegment);

                if (contains && !isSegment) //говорим что пакет сегментный
                    LostID[ID] = true;

                //если пакет новый
                if (ID == ReqID)
                {
                    if (!contains)//пакета не существует в потерянных то добавим (иначе если он будет устаревшим то не будет работать)
                        LostID.Add(ID, true);
                    return true;
                }

                if (ID > ReqID)//если пакет слишком новый
                {
                    LostPackagesStartFind(ReqID, ID - 1);
                    return true;
                }
                else//если пакет устарел
                {
                    if (contains)//если это потерянный пакет
                    {
                        return true;
                    }
                    else//иначе он уже приходил
                    {
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// Необходимо вызвать если сегментный пакет доставлен
        /// </summary>
        /// <param name="ID">айди пакета</param>
        /// <returns>если true то пакет пришёл в первые</returns>
        public void DoneCheckPacketSegment(ulong ID)
        {
            lock (this)
            {
                if (LostID.ContainsKey(ID))
                    LostID.Remove(ID);

                //если пакет новый
                if (ID == ReqID)
                {
                    ReqID++;
                }
                else if (ID > ReqID)//если пакет слишком новый
                {
                    ReqID++;
                }

                SendSndDeliveryСompletedPackage(ID);
            }
        }
    }
}
