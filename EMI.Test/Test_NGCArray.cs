using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMI.Test
{
    [TestClass]
    public class Test_NGCArray
    {
        [TestInitialize]
        public void Init()
        {
            NGCArray.ArrayLifetime = new TimeSpan(0, 0, 0, 0, 50);
        }

        [TestMethod("Проверка реиспользования массива")]
        public void Test1()
        {
            var arr1 = new NGCArray(1000);
            new NGCArray(5000).Dispose();
            var arr2 = new NGCArray(50000);
            byte[] a1 = arr1.Bytes;
            byte[] a2 = arr2.Bytes;
            arr1.Dispose();
            arr2.Dispose();
            var arrT = new NGCArray(1000);
            Assert.AreEqual(a1, arrT.Bytes);
            arrT.Dispose();
            arrT = new NGCArray(2000);
            Assert.AreNotEqual(a2, arrT.Bytes);
            Assert.AreNotEqual(a1, arrT.Bytes);
            arrT.Dispose();
            arrT = new NGCArray(15000);
            Assert.AreEqual(a2, arrT.Bytes);
            arrT.Dispose();

            Task.Delay(150).Wait();
        }

        [TestMethod("Проверка счётчиков и сборщика неиспользуемых массивов")]
        public void Test2()
        {
            Task.Delay(150).Wait();
            Test1();

            Assert.AreEqual(NGCArray.UseArrays, 0);
            Assert.AreEqual(NGCArray.TotalUseSize, 0);
            Assert.AreEqual(NGCArray.FreeArraysCount, 0);
            Assert.AreEqual(NGCArray.TotalFreeArraysSize, 0);
            var arr1 = new NGCArray(1000);
            Assert.AreEqual(NGCArray.UseArrays, 1);
            Assert.AreEqual(NGCArray.TotalUseSize, 1000);
            var a = arr1.Bytes;
            arr1.Dispose();
            Assert.AreEqual(NGCArray.UseArrays, 0);
            Assert.AreEqual(NGCArray.FreeArraysCount, 1);
            Assert.AreEqual(NGCArray.TotalFreeArraysSize, 1000);
            Assert.AreEqual(NGCArray.TotalUseSize, 0);
            Task.Delay(150).Wait();
            Assert.AreEqual(NGCArray.UseArrays, 0);
            Assert.AreEqual(NGCArray.FreeArraysCount, 0);
            Assert.AreEqual(NGCArray.TotalFreeArraysSize, 0);
            Assert.AreEqual(NGCArray.TotalUseSize, 0);
            Assert.AreNotEqual(a, new NGCArray(1000).Bytes);

            Task.Delay(150).Wait();
        }
    }
}