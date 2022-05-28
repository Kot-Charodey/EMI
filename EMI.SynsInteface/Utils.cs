using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace EMI.SynsInteface
{
    using Indicators;

    internal static class Utils
    {
        public static ModuleBuilder InitModuleBuilder()
        {
            AssemblyName aName = new AssemblyName("EMI.SyncInterface");
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
            string name = nameof(Indicator.Func);
            if(method.ReturnType != typeof(void))
                name = nameof(Indicator.FuncOut<int>);

            int gen = method.GetParameters().Length;

            if (method.ReturnType != typeof(void))
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

        public static Type[] GenericOutList(Type output, Type[] generic)
        {
            Type[] list = new Type[generic.Length + 1];
            list[0] = output;
            for (int i = 0; i < generic.Length; i++)
            {
                list[i + 1] = generic[i];
            }
            return list;
        }

        public static string FieldNameCreate(MethodInfo method,string name)
        {
            return $"SynsInterface::{method.Name}#{method.GetHashCode()}_{name}";
        }

        public static string FieldNameCreate(string name)
        {
            return $"SynsInterface::{name}";
        }
    }
}