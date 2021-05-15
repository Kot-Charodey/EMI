using System;
using System.Collections.Generic;
using System.Reflection;
using SmartPackager;

namespace EMI
{
    using EMI.Lower.Package;

    public partial class RPC
    {
        internal delegate byte[] RPCMicroFunct(byte[] arrayData);

        internal class MyAction
        {
            public byte LVL_Permission;
            public RPCMicroFunct MicroFunct;
            public bool CanReturnValue;
            public object Context;
            /// <summary>
            /// для CanAddFunction - проверяет все ли функции с одиновым набором аргументов
            /// </summary>
            public Type[] TypeList;

            public MyAction(byte lVL_Permission, RPCMicroFunct microFunct, bool canReturnValue, object context, Type[] typeList)
            {
                LVL_Permission = lVL_Permission;
                MicroFunct = microFunct ?? throw new ArgumentNullException(nameof(microFunct));
                CanReturnValue = canReturnValue;
                Context = context ?? throw new ArgumentNullException(nameof(context));
                TypeList = typeList ?? throw new ArgumentNullException(nameof(typeList));
            }
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

        private void CanAddFunction(ushort address, bool thisCanReturn, Type[] TypeList)
        {
            bool error = false;
            foreach (MyAction action in Functions[address])
            {
                if (action.CanReturnValue != thisCanReturn || action.TypeList.Length != TypeList.Length)
                {
                    error = true;
                }
                else
                {
                    for (int i = 0; i < TypeList.Length; i++)
                    {
                        if (action.TypeList[i] != TypeList[i])
                        {
                            error = true;
                            break;
                        }
                    }
                }

                break;
            }

            if (error)
                throw new Exception("Адрес может содержать несколько функций, только если они все с одинаковым набором аргументов и не возвращают значение!");

            //если это не глобальный список вызовов, ещё заглянем туда
            if (Global != this)
            {
                Global.CanAddFunction(address, thisCanReturn, TypeList);
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
            Type[] TypeList = { };
            CanAddFunction(Address, false, TypeList);

            byte[] MicroFunct(byte[] arrayData)
            {
                Funct();
                return null;
            }
            MyAction action = new MyAction(LVL_Permission, MicroFunct, false, Funct.Target, TypeList);

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
        {
            Type[] TypeList = { typeof(T1) };
            CanAddFunction(Address, false, TypeList);

            var packagerIN = Packager.Create<T1>();

            byte[] MicroFunct(byte[] arrayData)
            {
                packagerIN.UnPack(arrayData, 0, out T1 t1);
                Funct(t1);
                return null;
            }
            MyAction action = new MyAction(LVL_Permission, MicroFunct, false, Funct.Target, TypeList);

            Functions[Address].Add(action);
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
        {
            Type[] TypeList = { typeof(TOut) };
            CanAddFunction(Address, true, TypeList);

            var packagerOUT = Packager.Create<TOut>();

            byte[] MicroFunct(byte[] arrayData)
            {
                var data = Funct();
                return packagerOUT.PackUP(data);
            }
            MyAction action = new MyAction(LVL_Permission, MicroFunct, false, Funct.Target, TypeList);

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
        public Handle RegisterMethod<TOut, T1>(ushort Address, byte LVL_Permission, RPCfunctOut<TOut, T1> Funct)
        {
            Type[] TypeList = { typeof(TOut), typeof(T1) };
            CanAddFunction(Address, true, TypeList);

            var packagerIN = Packager.Create<T1>();
            var packagerOUT = Packager.Create<TOut>();

            byte[] MicroFunct(byte[] arrayData)
            {
                packagerIN.UnPack(arrayData, 0, out T1 t1);
                var data = Funct(t1);
                return packagerOUT.PackUP(data);
            }
            MyAction action = new MyAction(LVL_Permission, MicroFunct, false, Funct.Target, TypeList);

            Functions[Address].Add(action);
            Handle handle = new Handle(this, action, Address);

            return handle;
        }
        #endregion
    }
}