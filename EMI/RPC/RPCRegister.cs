using System;
using System.Collections.Generic;
using System.Reflection;
using SpeedByteConvector;

namespace EMI
{
    using EMI.Lower.Package;

    public partial class RPC
    {
        internal class MyAction
        {
            public byte LVL_Permission;
            public MethodInfo MethodInfo;
            public object Context;
        }

        /// <summary>
        /// Ссылка на зарегистрированную функцию
        /// </summary>
        public class Handle
        {
            private bool IsExists = true;
            private readonly ushort Address;
            private readonly RPC refRPC;
            private readonly MyAction Action;

            internal Handle(RPC refRPC, MyAction action, ushort address)
            {
                this.refRPC = refRPC;
                Action = action;
                Address = address;
            }

            /// <summary>
            /// Убирает функцию из списка зарегистрированных
            /// </summary>
            public void DestroyFuntion()
            {
                if (IsExists == false)
                {
                    throw new Exception("Function has already been removed!");
                }

                refRPC.Functions[Address].Remove(Action);
                IsExists = false;
            }
        }

        #region In
        /// <summary>
        /// Регистрирует метод для последующего вызова по сети
        /// </summary>
        /// <param name="Address">Адресс функции</param>
        /// <param name="LVL_Permission">Уровень прав которыми должен обладать пользователь чтобы запустить</param>
        /// <param name="Funct">Функция</param>
        /// <returns>Ссылка на функцию</returns>
        public Handle RegisterMethod(ushort Address, byte LVL_Permission, RPCfunct Funct)
        {
            MyAction action = new MyAction() { Context = Funct.Target, LVL_Permission = LVL_Permission, MethodInfo = Funct.Method };
            Functions[Address].Add(action);
            Handle handle = new Handle(this, action, Address);

            return handle;
        }

        /// <summary>                                                                                                           
        /// Регистрирует метод c аргументами для последующего вызова по сети                                                    
        /// </summary>
        /// <typeparam name="T1">Тип аргумента №1</typeparam>                                                                                                         
        /// <param name="Address">Адресс функции</param>                                                                      
        /// <param name="LVL_Permission">Уровень прав которыми должен обладать пользователь чтобы запустить</param>           
        /// <param name="Funct">Функция</param>                                                                                
        /// <returns>Ссылка на функцию</returns>                                                                                
        public unsafe Handle RegisterMethod<T1>(ushort Address, byte LVL_Permission, RPCfunct<T1> Funct)
        where T1 : unmanaged
        {
            MethodInfo mi = Funct.GetMethodInfo();
            //массив аргументов                                                                                                 
            object[] args = new object[1];
            //Генерируем микрокод для распаковки массива байт в аргументы и запуска самой функции                               
            RPCfunct<IntPtr> act = (IntPtr A1) =>
            {
                T1* t1 = (T1*)(A1 + sizeof(BitArgument));
                args[0] = *t1;
                mi.Invoke(Funct.Target, args);
            };

            MyAction action = new MyAction() { Context = act.Target, LVL_Permission = LVL_Permission, MethodInfo = act.Method };
            //регистрирует функцию по указанному адресу                                                                         
            Functions[Address].Add(action);
            //создаёт  указатель на регистрацию для возможности дерегистрировать                                                
            Handle handle = new Handle(this, action, Address);

            return handle;
        }
        //  МАССИВЫ 
        public unsafe Handle RegisterMethod<T1>(ushort Address, byte LVL_Permission, RPCfunct<T1[]> Funct)
        where T1 : unmanaged
        {
            MethodInfo mi = Funct.GetMethodInfo();
            //массив аргументов                                                                                                 
            object[] args = new object[1];
            //Генерируем микрокод для распаковки массива байт в аргументы и запуска самой функции                               
            RPCfunct<IntPtr, IntPtr> act = (IntPtr A1Size, IntPtr A1Data) =>
            {
                T1* t1 = (T1*)(A1Data + sizeof(BitArgument));
                T1[] arrT1 = new T1[*((int*)(A1Size + sizeof(BitArgument)))];
                int arrT1Size = sizeof(T1) * arrT1.Length;
                fixed (void* arr = &arrT1[0])
                    Buffer.MemoryCopy(t1, arr, arrT1Size, arrT1Size);

                args[0] = arrT1;

                mi.Invoke(Funct.Target, args);
            };

            MyAction action = new MyAction() { Context = act.Target, LVL_Permission = LVL_Permission, MethodInfo = act.Method };
            //регистрирует функцию по указанному адресу                                                                         
            Functions[Address].Add(action);
            //создаёт  указатель на регистрацию для возможности дерегистрировать                                                
            Handle handle = new Handle(this, action, Address);

            return handle;
        }
        #endregion
        #region Out

        /// <summary>
        /// Регистрирует метод для последующего вызова по сети [Для функций которые возвращают результат]
        /// </summary>
        /// <typeparam name="TOut">Тип результата выполнения функции</typeparam>
        /// <param name="Address">Адресс функции</param>
        /// <param name="LVL_Permission">Уровень прав которыми должен обладать пользователь чтобы запустить</param>
        /// <param name="Funct">Функция</param>
        /// <returns>Ссылка на функцию</returns>
        public unsafe Handle RegisterMethod<TOut>(ushort Address, byte LVL_Permission, RPCfunctOut<TOut> Funct)
            where TOut : unmanaged
        {
            MethodInfo mi = Funct.GetMethodInfo();
            object[] args = new object[0];

            RPCfunctOut<byte[]> act = () =>
            {
                byte[] buffer = new byte[sizeof(TOut)];
                PackConvector.PackUP(buffer, (TOut)mi.Invoke(Funct.Target, args));
                return buffer;
            };

            MyAction action = new MyAction() { Context = act.Target, LVL_Permission = LVL_Permission, MethodInfo = act.Method };
            Functions[Address].Add(action);
            Handle handle = new Handle(this, action, Address);

            return handle;
        }

        /// <summary>                                                                                                           
        /// Регистрирует метод для последующего вызова по сети [Для функций которые возвращают результат]                       
        /// </summary>                                                                                                          
        /// <typeparam name="TOut">Тип результата выполнения функции</typeparam>
        /// <typeparam name="T1">Тип аргумента №1</typeparam>                                             
        /// <param name="Address">Адресс функции</param>                                                                      
        /// <param name="LVL_Permission">Уровень прав которыми должен обладать пользователь чтобы запустить</param>           
        /// <param name="Funct">Функция</param>                                                                               
        /// <returns>Ссылка на функцию</returns>                                                                                
        public unsafe Handle RegisterMethod<TOut, T1>(ushort Address, byte LVL_Permission, RPCfunctOut<TOut, T1> Funct)
        where TOut : unmanaged
        where T1 : unmanaged
        {
            MethodInfo mi = Funct.GetMethodInfo();
            //массив аргументов                                                                                                 
            object[] args = new object[1];
            //Генерируем микрокод для распаковки массива байт в аргументы и запуска самой функции                               
            RPCfunctOut<byte[], IntPtr> act = (IntPtr A1) =>
            {
                T1* t1 = (T1*)(A1 + sizeof(BitArgument));
                args[0] = *t1;
                //Выполняет функцию и упаковывает результат в массив и возвращает его                                           
                byte[] buffer = new byte[sizeof(TOut)];
                PackConvector.PackUP(buffer, (TOut)mi.Invoke(Funct.Target, args));
                return buffer;
            };

            MyAction action = new MyAction() { Context = act.Target, LVL_Permission = LVL_Permission, MethodInfo = act.Method };
            //регистрирует функцию по указанному адресу                                                                         
            Functions[Address].Add(action);
            //создаёт  указатель на регистрацию для возможности дерегистрировать                                                
            Handle handle = new Handle(this, action, Address);

            return handle;
        }
        #endregion
    }
}