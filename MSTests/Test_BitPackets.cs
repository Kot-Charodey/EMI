using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace MSTests
{
    using EMI;
    using EMI.Lower;
    using EMI.Lower.Package;
    using EMI.Lower.Buffer;

    /// <summary>
    /// Проверка правильности заполнения полей пакетов (не должны пересекаться)
    /// </summary>
    [TestClass]
    public class Test_BitPackets
    {

        [TestMethod]
        public void Test1_BitPacketGuaranteed()
        {
            FieldsTest<BitPacketGuaranteed>();
        }

        [TestMethod]
        public void Test2_BitPacketGuaranteedReturned()
        {
            FieldsTest<BitPacketGuaranteedReturned>();
        }

        [TestMethod]
        public void Test3_BitPacketReqGetPkgSegmented()
        {
            FieldsTest<BitPacketReqGetPkgSegmented>();
        }

        [TestMethod]
        public void Test4_BitPacketSegmented()
        {
            FieldsTest<BitPacketSegmented>();
        }

        [TestMethod]
        public void Test5_BitPacketSegmentedReturned()
        {
            FieldsTest<BitPacketSegmentedReturned>();
        }

        [TestMethod]
        public void Test6_BitPacketSimple()
        {
            FieldsTest<BitPacketSimple>();
        }

        [TestMethod]
        public void Test7_BitPackageDeliveryСompleted()
        {
            FieldsTest<BitPackageDeliveryСompleted>();
        }

        static Dictionary<Type, object> SetTestData = InitSetTestData();

        static Dictionary<Type, object> InitSetTestData()
        {
            Dictionary<Type, object> SetTestData = new Dictionary<Type, object>();
            SetTestData.Add(typeof(PacketType), (PacketType)255);

            SetTestData.Add(typeof(byte), byte.MaxValue);
            SetTestData.Add(typeof(short), short.MaxValue);
            SetTestData.Add(typeof(int), int.MaxValue);
            SetTestData.Add(typeof(long), long.MaxValue);

            SetTestData.Add(typeof(sbyte), sbyte.MaxValue);
            SetTestData.Add(typeof(ushort), ushort.MaxValue);
            SetTestData.Add(typeof(uint), uint.MaxValue);
            SetTestData.Add(typeof(ulong), ulong.MaxValue);

            SetTestData.Add(typeof(float), float.MaxValue);
            SetTestData.Add(typeof(double), double.MaxValue);
            SetTestData.Add(typeof(char), char.MaxValue);
            return SetTestData;
        }

        public static void Eq<T>(T a, T b)
        {
            Assert.AreEqual(a, b);
        }

        void FieldsTest<T>() where T : unmanaged
        {
            Type structType = typeof(T);
            FieldInfo[] fields = structType.GetFields();

            unsafe
            {
                //проверка типа SizeOf (правильно ли выставили)
                Assert.AreEqual(sizeof(T), structType.GetField("SizeOf").GetValue(null));
            }

            T clearPacket = new T();

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (!field.IsStatic)
                {
                    Debug.WriteLine($"=================[{i}]\nПоле: {field.Name} тест:");

                    T packet = new T();

                    object val = SetTestData[field.FieldType];
                    field.SetValueDirect(__makeref(packet), val);

                    for (int k = 0; k < fields.Length; k++)
                    {
                        if (!fields[k].IsStatic)
                        {
                            object val1 = fields[k].GetValue(packet);
                            object val2 = fields[k].GetValue(clearPacket);


                            if (k != i)
                            {
                                Debug.WriteLine($"  [{k}] ({fields[k].FieldType.Name})  0x{val1:X} 0x{val2:X}");

                                Type ts = typeof(Test_BitPackets);
                                MethodInfo mi = ts.GetMethod("Eq");
                                var mi2 = mi.MakeGenericMethod(fields[k].FieldType);
                                mi2.Invoke(null, new object[] { val1, val2 });
                            }
                            else
                            {
                                Debug.WriteLine($"  [{k}] ({fields[k].FieldType.Name})  0x{val1:X} 0x{val:X}");
                            }
                        }
                    }
                }
            }
        }
    }
}
