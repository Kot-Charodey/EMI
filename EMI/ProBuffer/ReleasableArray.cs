using System;
using System.Threading;

namespace EMI.ProBuffer
{
    /// <summary>
    /// Массив выделенный ProArrayBuffer
    /// </summary>
    public class ReleasableArray:IReleasableArray
    {
        /// <summary>
        /// Размер массива
        /// </summary>
        public int Length { get; internal set; }
        /// <summary>
        /// Массив (размер массива следует считывать из другого поля)
        /// </summary>
        public byte[] Bytes { get; private set; }
        /// <summary>
        /// Смещение - от куда следует считывать
        /// </summary>
        public int Offset { get; set; }
        /// <summary>
        /// Хранит время когда начал или прекратил использоваться
        /// </summary>
        private DateTime TimeAllocate;
        private readonly Timer Timer;
        private bool IsRelease;
#if DEBUG
        /// <summary>
        /// 
        /// </summary>
        public string DEBUG_NAME;
#endif


        private readonly ProArrayBuffer MyBuffer;
        /// <summary>
        /// Необходимо вызвать после использования массива
        /// </summary>
        public void Release()
        {
            if (Bytes != null)
            {
                lock (MyBuffer.ReleasableArrays)
                {
                    Offset = 0;
                    IsRelease = true;
                    MyBuffer.ReleasableArrays.Add(this);
                    TimeAllocate = CurrentTime.Now;
                }
            }
        }
        internal ReleasableArray(ProArrayBuffer myBuffer, int size,bool isRelease)
        {
            Bytes = new byte[size];
            MyBuffer = myBuffer;
            Offset = 0;
            Length = size;
            IsRelease = isRelease;
            TimeAllocate = CurrentTime.Now;

            myBuffer.ForceRelese += MyBufferForceRelese;
            Timer = new Timer(TimeCheck, null, 500, 500);
        }

        internal void Allocate(int size)
        {
            Length = size;
            IsRelease = false;
            TimeAllocate = CurrentTime.Now;
        }

        private void TimeCheck(object _)
        {
            if (Bytes!=null)
            {
                if (IsRelease)
                {
                    if (CurrentTime.Now - TimeAllocate > ProArrayBuffer.ReleasableArrayLifetime)
                    {
                        lock (MyBuffer.ReleasableArrays)
                        {
                            if (MyBuffer.ReleasableArrays.Remove(this))
                            {
                                StopTimer();
                            }
                        }
                    }
                }
                else
                {
                    if (CurrentTime.Now - TimeAllocate > ProArrayBuffer.AllocadedArrayMaxLifetime)
                    {
                        const string err = "The allocated array is not released";
#if DEBUG
                        System.Diagnostics.Debug.WriteLine(err + DEBUG_NAME);
                        Console.WriteLine(err + DEBUG_NAME);
#else
                    System.Diagnostics.Debug.WriteLine(err);
                    Console.WriteLine(err);
#endif
                        Bytes = null;
                        StopTimer();
                    }
                }
            }
        }

        private void MyBufferForceRelese()
        {
            StopTimer();
            Bytes = null;
        }

        private void StopTimer()
        {
            lock (Timer)
            {
                Timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }
    }
}
