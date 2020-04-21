using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSerialization;

namespace Tests
{
    [TestClass]
    public class MSerialization
    {
        [TestMethod]
        public void TestCreateVector3()
        {
            Vector3F vec1 = new Vector3F(555,444,333);
            Vector3I vec2 = new Vector3I(555, 444, 333);
        }

        [TestMethod]
        public void TestСравнениеVector3F()
        {
            Vector3F vec1 = new Vector3F(5, 100, 244);
            Vector3F vec2 = new Vector3F(5, 100, 244);

            bool a;
            bool b;

            a = (vec1 == vec2);
            b = (vec1.x == vec2.x && vec1.y == vec2.y && vec1.z == vec2.z);

            Assert.IsTrue(a==b);
        }

        [TestMethod]
        public void TestСуммаVector3F()
        {
            Vector3F vec1 = new Vector3F(5, 100, 200);
            Vector3F vec2 = new Vector3F(5, 100, 200);

            Vector3F vec3 = vec1 + vec2;

            Assert.IsTrue(vec3.x == 10 && vec3.y == 200 && vec3.z == 400);
        }

        [TestMethod]
        public void TestВычитаниеVector3F()
        {
            Vector3F vec1 = new Vector3F(5, 101, 202);
            Vector3F vec2 = new Vector3F(5, 100, 200);

            Vector3F vec3 = vec1 - vec2;

            Assert.IsTrue(vec3.x == 0 && vec3.y == 1 && vec3.z == 2);
        }

        [TestMethod]
        public void TestУмножениеVector3F()
        {
            Vector3F vec1 = new Vector3F(5, 10, 20);
            Vector3F vec2 = new Vector3F(5, 10, 20);

            Vector3F vec3 = vec1 * vec2;

            Assert.IsTrue(vec3.x == 25 && vec3.y == 100 && vec3.z == 400);
        }

        [TestMethod]
        public void TestУмножение2Vector3F()
        {
            Vector3F vec1 = new Vector3F(5, 10, 20);

            Vector3F vec3 = vec1 * 2;

            Assert.IsTrue(vec3.x == 10 && vec3.y == 20 && vec3.z == 40);
        }
        public void TestделениеVector3F()
        {
            Vector3F vec1 = new Vector3F(5, 20, 60);
            Vector3F vec2 = new Vector3F(5, 10, 20);

            Vector3F vec3 = vec1 / vec2;

            Assert.IsTrue(vec3.x == 1 && vec3.y == 2 && vec3.z == 3);
        }

        [TestMethod]
        public void Testделение2Vector3F()
        {
            Vector3F vec1 = new Vector3F(5, 20, 60);

            Vector3F vec3 = vec1 / 2;

            Assert.IsTrue(vec3.x == 2.5f && vec3.y == 10 && vec3.z == 30);
        }

        [TestMethod]
        public void TestСериализаторVector3F()
        {
            Vector3F vec1 = new Vector3F(1,2.123f,3);
            byte[] data = vec1.Serialization();
            Vector3F vec2=new Vector3F(data);

            Assert.IsTrue(vec2.x == 1 && vec2.y == 2.123f && vec2.z == 3);
        }

        [TestMethod]
        public void TestСериализаторVector3I()
        {
            Vector3I vec1 = new Vector3I(1, 2, 3);
            byte[] data = vec1.Serialization();
            Vector3I vec2 = new Vector3I(data);

            Assert.IsTrue(vec2.x == 1 && vec2.y == 2 && vec2.z == 3);
        }
    }
}
