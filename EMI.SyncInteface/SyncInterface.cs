using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace EMI.SyncInterface
{
    using Indicators;
    using MyException;

    public class SyncInterface<T> where T : class
    {
        private readonly static ModuleBuilder MBuilder = Utils.InitModuleBuilder();
        private readonly static Dictionary<Type, InterfaceTypes> CachedInterfaces = new Dictionary<Type, InterfaceTypes>();

        private InterfaceTypes Types;
        private readonly List<MarkeredMethod> ServerMethods = new List<MarkeredMethod>();
        private readonly List<MarkeredMethod> ClientMethods = new List<MarkeredMethod>();

        public SyncInterface(string name)
        {
            Type interfaceType = typeof(T);
            if (!interfaceType.IsInterface)
                throw new InvalidInterfaceException("A generic type is not an interface!");


            if (!CachedInterfaces.TryGetValue(interfaceType, out InterfaceTypes types))
            {
                const MethodAttributes mAttributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot;
                const FieldAttributes fieldAttributes = FieldAttributes.Private | FieldAttributes.SpecialName;

                const TypeAttributes tBuilderAttributes = TypeAttributes.Public | TypeAttributes.Class;
                TypeBuilder tBuilderServer = MBuilder.DefineType("SyncInterfaceServerSide::" + name, tBuilderAttributes);
                TypeBuilder tBuilderClient = MBuilder.DefineType("SyncInterfaceClientSide::" + name, tBuilderAttributes);
                tBuilderClient.AddInterfaceImplementation(interfaceType);
                tBuilderServer.AddInterfaceImplementation(interfaceType);

                int NameID = 0; // используется для наименования переменных
                var fieldClientClient = tBuilderClient.DefineField(Utils.FieldNameCreate(ref NameID), typeof(Client), fieldAttributes);
                var fieldClientServer = tBuilderServer.DefineField(Utils.FieldNameCreate(ref NameID), typeof(Client), fieldAttributes);
                var ServerFields = new List<FieldList>();
                var ClientFields = new List<FieldList>();

                var methods = interfaceType.GetMethods();

                foreach (var method in methods)
                {
                    if (method.Attributes.HasFlag(MethodAttributes.SpecialName)) //пропуск спецтодов по типу ToString()
                        continue;

                    var mParameters = method.GetParametersType();

                    void build(TypeBuilder tBuilder, FieldInfo clientField, List<FieldList> fieldsClass, List<MarkeredMethod> methodsList, bool plug)
                    {
                        var mBuilder = tBuilder.DefineMethod(method.Name, mAttributes, method.CallingConvention, method.ReturnType, mParameters);
                        var ilCode = mBuilder.GetILGenerator(); //TODO посчитать стек
                        if (!plug)
                        {
                            ilCode.ThrowException(typeof(NotImplementedException));
                            ilCode.Emit(OpCodes.Ret);
                        }
                        else
                        {
                            bool isReturnData = method.IsReturnData();
                            var indicatorType = Utils.GetIndicatorFunc(method);
                            if (indicatorType.IsGenericType)
                                indicatorType = Utils.CreateGenericIndicator(indicatorType, method);

                            var indicatorMethods = indicatorType.GetMethods();
                            var func = indicatorMethods.FindMethod(nameof(Indicator.Func.RCall));
                            var funParam = func.GetParameters();

                            var fieldIndicatorInst = tBuilder.DefineField(Utils.FieldNameCreate(ref NameID), indicatorType, fieldAttributes);
                            string indicatorName = $"{tBuilder.Name}#{method}";
                            var inst = Activator.CreateInstance(indicatorType, new object[] { indicatorName });
                            methodsList.Add(new MarkeredMethod(method, inst));

                            var fieldRCType = tBuilder.DefineField(Utils.FieldNameCreate(ref NameID), typeof(RCType), fieldAttributes);

                            ilCode.DeclareLocal(typeof(CancellationToken));

                            ilCode.Emit(OpCodes.Ldarg_0);                  //0
                            ilCode.Emit(OpCodes.Ldfld, fieldIndicatorInst);//1 вызываймый метод

                            for (int i = 0; i < mParameters.Length; i++)   //
                                ilCode.Emit(OpCodes.Ldarg, i + 1);         //входные аргументы

                            ilCode.Emit(OpCodes.Ldarg_0);                  //
                            ilCode.Emit(OpCodes.Ldfld, clientField);       //Client загрзука параметра в аргумент

                            ilCode.Emit(OpCodes.Ldarg_0);                  //
                            ilCode.Emit(OpCodes.Ldfld, fieldRCType);       //RCType загрузка параметра в аргумент

                            ilCode.Emit(OpCodes.Ldloca, 0);                         //
                            ilCode.Emit(OpCodes.Initobj, typeof(CancellationToken));//
                            ilCode.Emit(OpCodes.Ldloc, 0);                          //default CancellationToken

                            if (method.IsAsync())
                            {
                                ilCode.Emit(OpCodes.Callvirt, func);
                                //throw new NotImplementedException();
                            }
                            else
                            {
                                ilCode.Emit(OpCodes.Callvirt, func);
                                if (method.ReturnType == typeof(void))
                                {
                                    var taskWait = typeof(Task).GetMethod(nameof(Task.Wait), new Type[] { });
                                    ilCode.Emit(OpCodes.Callvirt, taskWait);
                                }
                                else
                                {
                                    var TaskResultMethod = typeof(Task<>).MakeGenericType(method.ReturnType).GetProperty(nameof(Task<int>.Result)).GetGetMethod();
                                    ilCode.Emit(OpCodes.Callvirt, TaskResultMethod);
                                }
                                ilCode.Emit(OpCodes.Ret);
                            }

                            var t = funParam[funParam.Length - 2].DefaultValue.GetType();


                            fieldsClass.Add(new FieldList() { Type = fieldIndicatorInst.FieldType, FieldInfo = fieldIndicatorInst, Content = inst });
                            fieldsClass.Add(new FieldList() { Type = fieldRCType.FieldType, FieldInfo = fieldRCType, Content = funParam[funParam.Length - 2].DefaultValue });
                        }
                        tBuilder.DefineMethodOverride(mBuilder, method);
                    }

                    var isServer = method.GetCustomAttribute<OnlyServerAttribute>() != null;
                    var isClient = method.GetCustomAttribute<OnlyClientAttribute>() != null;
                    var allSide = !isServer & !isClient;

                    build(tBuilderClient, fieldClientClient, ClientFields, ClientMethods, isClient | allSide);
                    build(tBuilderServer, fieldClientServer, ServerFields, ServerMethods, isServer | allSide);
                }

                var fields = interfaceType.GetProperties();
                foreach (var field in fields)
                {
                    throw new NotImplementedException();
                }

                //конструктор
                void createConstructor(TypeBuilder tBuilder, FieldInfo ClientField, List<FieldList> fieldsClass)
                {
                    var constructorArguments = new List<Type> { typeof(Client) };
                    foreach (var arg in fieldsClass)
                        constructorArguments.Add(arg.Type);

                    const MethodAttributes constAttributes = MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.HideBySig;
                    var construct = tBuilder.DefineConstructor(constAttributes, CallingConventions.Standard, constructorArguments.ToArray());
                    var ilCode = construct.GetILGenerator();
                    ilCode.Emit(OpCodes.Ldarg_0);           // аргумент this
                    ilCode.Emit(OpCodes.Ldarg, 1);          // аргумент client
                    ilCode.Emit(OpCodes.Stfld, ClientField);// загрузить его в поле
                    for (int i = 0; i < fieldsClass.Count; i++)
                    {
                        ilCode.Emit(OpCodes.Ldarg_0);
                        ilCode.Emit(OpCodes.Ldarg, i + 2);
                        ilCode.Emit(OpCodes.Stfld, fieldsClass[i].FieldInfo);
                    }
                    ilCode.Emit(OpCodes.Ret);
                }

                createConstructor(tBuilderClient, fieldClientClient, ClientFields);
                createConstructor(tBuilderServer, fieldClientServer, ServerFields);

                types = new InterfaceTypes(tBuilderClient.CreateType(), ClientFields, tBuilderServer.CreateType(), ServerFields);
                CachedInterfaces.Add(interfaceType, types);
            }

            Types = types;
        }

        public T NewIndicator(Client client)
        {
            if (client.IsServerSide)
            {
                var args = new List<object> { client };
                foreach (var arg in Types.ServerFields)
                    args.Add(arg.Content);
                return (T)Activator.CreateInstance(Types.Server, args.ToArray(), null);
            }
            else
            {
                var args = new List<object> { client };
                foreach (var arg in Types.ClientFields)
                    args.Add(arg.Content);
                return (T)Activator.CreateInstance(Types.Client, args.ToArray(), null);
            }
        }

        public void RegisterClass(Client client, T Class)
        {
            RegisterClass(client.RPC, Class, ServerMethods);
        }

        public void RegisterClass(Server server, T Class)
        {
            RegisterClass(server.RPC, Class, ClientMethods);
        }

        private void RegisterClass(RPC rpc, T Class, List<MarkeredMethod> methods)
        {
            foreach (var mMethod in methods)
            {
                var method = mMethod.MethodInfo;
                var mParameters = method.GetParametersType();
                //создаёт делегат для регистрации метода
                Delegate runDelegate;
                if (method.ReturnType == typeof(void))
                    runDelegate = Utils.MakeRPCDelegate(mParameters, Class, method);
                else
                    runDelegate = Utils.MakeRPCDelegateOut(method.ReturnType, mParameters, Class, method);

                Type delegateType = runDelegate.GetType();
                Type genericDelegateType;
                Type genericIndicatorType = mMethod.Indicator.GetType();

                if (delegateType.IsGenericType)
                    genericDelegateType = delegateType.GetGenericTypeDefinition();
                else
                    genericDelegateType = delegateType;

                if (genericIndicatorType.IsGenericType)
                    genericIndicatorType = genericIndicatorType.GetGenericTypeDefinition();

                //TODO мб можно ускорить
                MethodInfo RegMethod = typeof(RPC).FindMethodPro(nameof(rpc.RegisterMethod), new Type[] { genericDelegateType, genericIndicatorType });
                if(RegMethod.IsGenericMethod)
                    RegMethod = RegMethod.MakeGenericMethod(delegateType.GetGenericArguments());

                RegMethod.Invoke(rpc, new object[] {runDelegate, mMethod.Indicator});
            }
        }
    }
}