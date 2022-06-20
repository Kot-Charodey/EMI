using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;

namespace EMI.SyncInterface
{
    using Indicators;

    internal static class Utils
    {
        public static ModuleBuilder InitModuleBuilder()
        {
            SmartPackager.PackMethods.SetupAgainPackMethods(); //иначе smart packager будет пытаться просканировать несозданные типы и крашнет всю прогу
            AssemblyName aName = new AssemblyName("EMI.DynamicCodeGenerate.SyncInterface");
            AppDomain appDomain = Thread.GetDomain();
            AssemblyBuilder aBuilder = appDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);

            return aBuilder.DefineDynamicModule(aName.Name);
        }

        public static MethodInfo FindMethod(this MethodInfo[] methods, string name)
        {
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == name)
                    return methods[i];
            }
            throw new KeyNotFoundException();
        }

        /// <summary>
        /// Ищит и возвращает подходящий индикатор для метода
        /// </summary>
        /// <param name="method">метод для которого ищится индикатор</param>
        /// <returns>индикатор (может быть generic (надо создать))</returns>
        /// <exception cref="KeyNotFoundException">метод не найден</exception>
        public static Type GetIndicatorFunc(MethodInfo method)
        {
            var classes = typeof(Indicator).GetNestedTypes();
            string name;
            if (method.IsReturnData())
                name = nameof(Indicator.FuncOut<int>);
            else
                name = nameof(Indicator.Func);

            int gen = method.GetParameters().Length;

            if (method.ReturnType != typeof(void) && method.ReturnType != typeof(Task))
                gen++;

            if (gen > 0)
                name += "`" + gen;

            for (int i = 0; i < classes.Length; i++)
            {
                if (classes[i].Name == name)
                    return classes[i];
            }
            throw new KeyNotFoundException();
        }

        public static MethodInfo FindMethodPro(this Type type,string name, Type[] types)
        {
            foreach(var method in type.GetMethods())
            {
                if (method.Name == name)
                {
                    var param = method.GetParametersType();
                    if (param.Length == types.Length)
                    {
                        for (int i = 0; i < param.Length; i++)
                        {
                            if (param[i].Name != types[i].Name)
                                goto skip;
                        }
                        return method;
                    }
                }
            skip:;
            }
            throw new KeyNotFoundException();
        }

        public static bool IsReturnData(this MethodInfo method)
        {
            var type = method.ReturnType;
            return type != typeof(void) && type != typeof(Task);
        }

        public static Type GetReturnType(this MethodInfo method)
        {
            if (method.IsAsync(true))
                return method.ReturnType.GenericTypeArguments[0];
            else
                return method.ReturnType;
        }

        public static Type[] GetReturnAndParametrs(this MethodInfo method)
        {
            Type[] types;
            if (method.IsReturnData())
            {
                var param = method.GetParameters();
                types = new Type[param.Length + 1];
                types[0] = method.GetReturnType();

                for (int i = 0; i < param.Length; i++)
                    types[i + 1] = param[i].ParameterType;
            }
            else
            {
                var param = method.GetParameters();
                types = new Type[param.Length];
                for (int i = 0; i < param.Length; i++)
                    types[i] = param[i].ParameterType;
            }
            return types;
        }

        /// <summary>
        /// Создаёт generic метод
        /// </summary>
        /// <param name="indicator">индикатор который необходимо создать</param>
        /// <param name="method">метод для которого создаётся индикатор</param>
        /// <returns></returns>
        public static Type CreateGenericIndicator(Type indicator, MethodInfo method)
        {
            return indicator.MakeGenericType(method.GetReturnAndParametrs());
        }

        public static string FieldNameCreate(ref int id)
        {
            return $"SyncInterface::Field#[{id++}]";
        }

        public static bool IsAsync(this MethodInfo method,bool onlyTaskReturn = false)
        {
            var rp = method.ReturnParameter.ParameterType;
            return rp == typeof(Task) && !onlyTaskReturn || rp.BaseType == typeof(Task);
        }

        public static Type[] GetParametersType(this MethodInfo method)
        {
            var mParametersInfo = method.GetParameters();
            var mParameters = new Type[mParametersInfo.Length];
            for (int i = 0; i < mParametersInfo.Length; i++)
                mParameters[i] = mParametersInfo[i].ParameterType;
            return mParameters;
        }

        public static Delegate MakeRPCDelegate(Type[] types,object context,MethodInfo mi)
        {
            switch (types.Length)
            {
                case 0: return Delegate.CreateDelegate(typeof(RPCfunc),context,mi);
                case 1: return Delegate.CreateDelegate(typeof(RPCfunc<>).MakeGenericType(types), context,mi);
                case 2: return Delegate.CreateDelegate(typeof(RPCfunc<,>).MakeGenericType(types),context,mi);
                case 3: return Delegate.CreateDelegate(typeof(RPCfunc<,,>).MakeGenericType(types),context,mi);
                case 4: return Delegate.CreateDelegate(typeof(RPCfunc<,,,>).MakeGenericType(types),context,mi);
                case 5: return Delegate.CreateDelegate(typeof(RPCfunc<,,,,>).MakeGenericType(types),context,mi);
                case 6: return Delegate.CreateDelegate(typeof(RPCfunc<,,,,,>).MakeGenericType(types),context,mi);
                case 7: return Delegate.CreateDelegate(typeof(RPCfunc<,,,,,,>).MakeGenericType(types),context,mi);
                case 8: return Delegate.CreateDelegate(typeof(RPCfunc<,,,,,,,>).MakeGenericType(types),context,mi);
                case 9: return Delegate.CreateDelegate(typeof(RPCfunc<,,,,,,,,>).MakeGenericType(types),context,mi);
                case 10: return Delegate.CreateDelegate(typeof(RPCfunc<,,,,,,,,,>).MakeGenericType(types),context,mi);
                case 11: return Delegate.CreateDelegate(typeof(RPCfunc<,,,,,,,,,,>).MakeGenericType(types),context,mi);
                case 12: return Delegate.CreateDelegate(typeof(RPCfunc<,,,,,,,,,,,>).MakeGenericType(types),context,mi);
                case 13: return Delegate.CreateDelegate(typeof(RPCfunc<,,,,,,,,,,,,>).MakeGenericType(types),context,mi);
                case 14: return Delegate.CreateDelegate(typeof(RPCfunc<,,,,,,,,,,,,,>).MakeGenericType(types),context,mi);
                case 15: return Delegate.CreateDelegate(typeof(RPCfunc<,,,,,,,,,,,,,,>).MakeGenericType(types),context,mi);
                case 16: return Delegate.CreateDelegate(typeof(RPCfunc<,,,,,,,,,,,,,,,>).MakeGenericType(types),context,mi);
                case 17: return Delegate.CreateDelegate(typeof(RPCfunc<,,,,,,,,,,,,,,,,>).MakeGenericType(types),context,mi);
                case 18: return Delegate.CreateDelegate(typeof(RPCfunc<,,,,,,,,,,,,,,,,,>).MakeGenericType(types),context,mi);
                case 19: return Delegate.CreateDelegate(typeof(RPCfunc<,,,,,,,,,,,,,,,,,,>).MakeGenericType(types),context,mi);
                case 20: return Delegate.CreateDelegate(typeof(RPCfunc<,,,,,,,,,,,,,,,,,,,>).MakeGenericType(types),context,mi);
                default: throw new IndexOutOfRangeException();
            }
        }

        public static Delegate MakeRPCDelegateOut(Type outType, Type[] inTypes, object context, MethodInfo mi)
        {
            var types = new Type[inTypes.Length + 1];
            types[0] = outType;
            for (int i = 0; i < inTypes.Length; i++)
                types[i + 1] = inTypes[i];
            switch (types.Length)
            {
                case 1: return Delegate.CreateDelegate(typeof(RPCfuncOut<>).MakeGenericType(types),context,mi);
                case 2: return Delegate.CreateDelegate(typeof(RPCfuncOut<,>).MakeGenericType(types),context,mi);
                case 3: return Delegate.CreateDelegate(typeof(RPCfuncOut<,,>).MakeGenericType(types),context,mi);
                case 4: return Delegate.CreateDelegate(typeof(RPCfuncOut<,,,>).MakeGenericType(types),context,mi);
                case 5: return Delegate.CreateDelegate(typeof(RPCfuncOut<,,,,>).MakeGenericType(types),context,mi);
                case 6: return Delegate.CreateDelegate(typeof(RPCfuncOut<,,,,,>).MakeGenericType(types),context,mi);
                case 7: return Delegate.CreateDelegate(typeof(RPCfuncOut<,,,,,,>).MakeGenericType(types),context,mi);
                case 8: return Delegate.CreateDelegate(typeof(RPCfuncOut<,,,,,,,>).MakeGenericType(types),context,mi);
                case 9: return Delegate.CreateDelegate(typeof(RPCfuncOut<,,,,,,,,>).MakeGenericType(types),context,mi);
                case 10: return Delegate.CreateDelegate(typeof(RPCfuncOut<,,,,,,,,,>).MakeGenericType(types),context,mi);
                case 11: return Delegate.CreateDelegate(typeof(RPCfuncOut<,,,,,,,,,,>).MakeGenericType(types),context,mi);
                case 12: return Delegate.CreateDelegate(typeof(RPCfuncOut<,,,,,,,,,,,>).MakeGenericType(types),context,mi);
                case 13: return Delegate.CreateDelegate(typeof(RPCfuncOut<,,,,,,,,,,,,>).MakeGenericType(types),context,mi);
                case 14: return Delegate.CreateDelegate(typeof(RPCfuncOut<,,,,,,,,,,,,,>).MakeGenericType(types),context,mi);
                case 15: return Delegate.CreateDelegate(typeof(RPCfuncOut<,,,,,,,,,,,,,,>).MakeGenericType(types),context,mi);
                case 16: return Delegate.CreateDelegate(typeof(RPCfuncOut<,,,,,,,,,,,,,,,>).MakeGenericType(types),context,mi);
                case 17: return Delegate.CreateDelegate(typeof(RPCfuncOut<,,,,,,,,,,,,,,,,>).MakeGenericType(types),context,mi);
                case 18: return Delegate.CreateDelegate(typeof(RPCfuncOut<,,,,,,,,,,,,,,,,,>).MakeGenericType(types),context,mi);
                case 19: return Delegate.CreateDelegate(typeof(RPCfuncOut<,,,,,,,,,,,,,,,,,,>).MakeGenericType(types),context,mi);
                case 20: return Delegate.CreateDelegate(typeof(RPCfuncOut<,,,,,,,,,,,,,,,,,,,>).MakeGenericType(types),context,mi);
                default: throw new IndexOutOfRangeException();
            }
        }
    }
}