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

            if (method.ReturnType != typeof(void) && method.ReturnType!=typeof(Task))
                gen++;

            if (gen > 0)
                name += "`" + gen;

            for(int i = 0; i < classes.Length; i++)
            {
                if (classes[i].Name==name)
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
                types = new Type[param.Length+1];
                types[0] = method.ReturnType;
                for (int i = 0; i < param.Length; i++)
                    types[i+1] = param[i].ParameterType;
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

        public static Type CreateGenericIndicator(Type indicator,MethodInfo method)
        {
            return indicator.MakeGenericType(GetReturnAndParametrs(method));
        }

        public static string FieldNameCreate(ref int id)
        {
            return $"SyncInterface::Field#[{id++}]";
        }
    }
}