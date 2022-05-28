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

        public static bool IsReturnData(this MethodInfo method)
        {
            var type = method.ReturnType;
            return type != typeof(void) && type != typeof(Task);
        }

        public static Type[] GetReturnAndParametrs(MethodInfo method)
        {
            Type[] types;
            if (method.IsReturnData())
            {
                var param = method.GetParameters();
                types = new Type[param.Length + 1];
                types[0] = method.ReturnType;
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

        public static Type CreateGenericIndicator(Type indicator, MethodInfo method)
        {
            return indicator.MakeGenericType(GetReturnAndParametrs(method));
        }

        public static string FieldNameCreate(ref int id)
        {
            return $"SyncInterface::Field#[{id++}]";
        }

        public static bool IsAsync(this MethodInfo method)
        {
            if (method.GetCustomAttribute<AsyncCompileAttribute>() != null)
            {
                return true;
            }
            else if (method.Module.GetCustomAttribute<AsyncCompileAttribute>() != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static Type[] GetParametersType(this MethodInfo method)
        {
            var mParametersInfo = method.GetParameters();
            var mParameters = new Type[mParametersInfo.Length];
            for (int i = 0; i < mParametersInfo.Length; i++)
                mParameters[i] = mParametersInfo[i].ParameterType;
            return mParameters;
        }

        public static Type GetRPCDelegate(Type[] types)
        {
            switch (types.Length)
            {
                case 0: return typeof(RPCfunc);
                case 1: return typeof(RPCfunc<>).MakeGenericType(types);
                case 2: return typeof(RPCfunc<,>).MakeGenericType(types);
                case 3: return typeof(RPCfunc<,,>).MakeGenericType(types);
                case 4: return typeof(RPCfunc<,,,>).MakeGenericType(types);
                case 5: return typeof(RPCfunc<,,,,>).MakeGenericType(types);
                case 6: return typeof(RPCfunc<,,,,,>).MakeGenericType(types);
                case 7: return typeof(RPCfunc<,,,,,,>).MakeGenericType(types);
                case 8: return typeof(RPCfunc<,,,,,,,>).MakeGenericType(types);
                case 9: return typeof(RPCfunc<,,,,,,,,>).MakeGenericType(types);
                case 10: return typeof(RPCfunc<,,,,,,,,,>).MakeGenericType(types);
                case 11: return typeof(RPCfunc<,,,,,,,,,,>).MakeGenericType(types);
                case 12: return typeof(RPCfunc<,,,,,,,,,,,>).MakeGenericType(types);
                case 13: return typeof(RPCfunc<,,,,,,,,,,,,>).MakeGenericType(types);
                case 14: return typeof(RPCfunc<,,,,,,,,,,,,,>).MakeGenericType(types);
                case 15: return typeof(RPCfunc<,,,,,,,,,,,,,,>).MakeGenericType(types);
                case 16: return typeof(RPCfunc<,,,,,,,,,,,,,,,>).MakeGenericType(types);
                case 17: return typeof(RPCfunc<,,,,,,,,,,,,,,,,>).MakeGenericType(types);
                case 18: return typeof(RPCfunc<,,,,,,,,,,,,,,,,,>).MakeGenericType(types);
                case 19: return typeof(RPCfunc<,,,,,,,,,,,,,,,,,,>).MakeGenericType(types);
                case 20: return typeof(RPCfunc<,,,,,,,,,,,,,,,,,,,>).MakeGenericType(types);
                default: throw new IndexOutOfRangeException();
            }
        }

        public static Type GetRPCDelegateOut(Type outType, Type[] inTypes)
        {
            var types = new Type[inTypes.Length + 1];
            types[0] = outType;
            for (int i = 0; i < inTypes.Length; i++)
                types[i + 1] = inTypes[i];
            switch (types.Length)
            {
                case 1: return typeof(RPCfuncOut<>).MakeGenericType(types);
                case 2: return typeof(RPCfuncOut<,>).MakeGenericType(types);
                case 3: return typeof(RPCfuncOut<,,>).MakeGenericType(types);
                case 4: return typeof(RPCfuncOut<,,,>).MakeGenericType(types);
                case 5: return typeof(RPCfuncOut<,,,,>).MakeGenericType(types);
                case 6: return typeof(RPCfuncOut<,,,,,>).MakeGenericType(types);
                case 7: return typeof(RPCfuncOut<,,,,,,>).MakeGenericType(types);
                case 8: return typeof(RPCfuncOut<,,,,,,,>).MakeGenericType(types);
                case 9: return typeof(RPCfuncOut<,,,,,,,,>).MakeGenericType(types);
                case 10: return typeof(RPCfuncOut<,,,,,,,,,>).MakeGenericType(types);
                case 11: return typeof(RPCfuncOut<,,,,,,,,,,>).MakeGenericType(types);
                case 12: return typeof(RPCfuncOut<,,,,,,,,,,,>).MakeGenericType(types);
                case 13: return typeof(RPCfuncOut<,,,,,,,,,,,,>).MakeGenericType(types);
                case 14: return typeof(RPCfuncOut<,,,,,,,,,,,,,>).MakeGenericType(types);
                case 15: return typeof(RPCfuncOut<,,,,,,,,,,,,,,>).MakeGenericType(types);
                case 16: return typeof(RPCfuncOut<,,,,,,,,,,,,,,,>).MakeGenericType(types);
                case 17: return typeof(RPCfuncOut<,,,,,,,,,,,,,,,,>).MakeGenericType(types);
                case 18: return typeof(RPCfuncOut<,,,,,,,,,,,,,,,,,>).MakeGenericType(types);
                case 19: return typeof(RPCfuncOut<,,,,,,,,,,,,,,,,,,>).MakeGenericType(types);
                case 20: return typeof(RPCfuncOut<,,,,,,,,,,,,,,,,,,,>).MakeGenericType(types);
                default: throw new IndexOutOfRangeException();
            }
        }
    }
}