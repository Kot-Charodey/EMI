namespace EMI.Lower
{
    internal enum PacketType:byte
    {
        SndClose,//сообщает что происходит отключение
        SndSimple,//просто вызвать функцию (не гарантированно)
        SndGuaranteed,//просто вызвать функцию (гарантированно)
        SndGuaranteedRtr,//результат данного пакета следует вернуть
        SndGuaranteedSegmented,//выполнить жирную функцию
        SndGuaranteedRtrSegmented,//выполнить жирную функцию и вернуть результат
        SndGuaranteedReturned,//результат выполнения функции
        SndGuaranteedSegmentedReturned,//результат выполнения функции (жирные данные)
        SndDeliveryСompletedPackage,//пакет доставлен (любой гарантированный (и сегментный и обычный))
        ReqGetPkg,//просит вернуть пакет
        ReqGetPkgSegmented,//просит вернуть сегментный пакет
        ReqPing0,//пинг - отправка
        ReqPing1,//пинг - приём 
        ReqConnection0,//client -> server #1
        ReqConnection1,//server -> client #2 (int hesh code)
        ReqConnection2,//client -> server #3 (int hesh code)
    }
}
