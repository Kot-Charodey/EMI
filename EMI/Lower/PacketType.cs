namespace EMI.Lower
{
    internal enum PacketType:byte
    {
        /* 0*/SndClose,//сообщает что происходит отключение
        /* 1*/SndSimple,//просто вызвать функцию (не гарантированно)
        /* 2*/SndGuaranteed,//просто вызвать функцию (гарантированно)
        /* 3*/SndGuaranteedRtr,//результат данного пакета следует вернуть
        /* 4*/SndGuaranteedSegmented,//выполнить жирную функцию
        /* 5*/SndGuaranteedRtrSegmented,//выполнить жирную функцию и вернуть результат
        /* 6*/SndGuaranteedReturned,//результат выполнения функции
        /* 7*/SndGuaranteedSegmentedReturned,//результат выполнения функции (жирные данные)
        /* 8*/SndDeliveryСompletedPackage,//пакет доставлен (любой гарантированный (и сегментный и обычный))
        /* 9*/ReqGetPkg,//просит вернуть пакет
        /*10*/ReqGetPkgSegmented,//просит вернуть сегментный пакет
        /*11*/ReqPing0,//пинг - отправка
        /*12*/ReqPing1,//пинг - приём 
        /*13*/ReqConnection0,//client -> server #1
        /*14*/ReqConnection1,//server -> client #2 (int hesh code)
        /*15*/ReqConnection2,//client -> server #3 (int hesh code)
    }
}
