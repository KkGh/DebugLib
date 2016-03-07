using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DebugLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text;

namespace DebugLibTest
{
    [TestClass]
    public class DumperTest
    {
        [TestInitialize]
        public void BeforeEachTest()
        {
            Dumper.Reset();
        }

        [TestMethod]
        public void Test_Dump_プリミティブ()
        {
            Assert.AreEqual("(null)", Dumper.DumpToString(null));
            Assert.AreEqual("1 (System.Int32)", Dumper.DumpToString(1));
            Assert.AreEqual("10 (System.Int64)", Dumper.DumpToString(10L));
            Assert.AreEqual("79228162514264337593543950335 (System.Decimal)", Dumper.DumpToString(Decimal.MaxValue));
            Assert.AreEqual("0.12 (System.Double)", Dumper.DumpToString(0.12));
            Assert.AreEqual("0.1 (System.Single)", Dumper.DumpToString(0.1f));
            Assert.AreEqual("'A' (System.Char)", Dumper.DumpToString('A'));
            Assert.AreEqual("\"str\" (System.String)", Dumper.DumpToString("str"));
            Assert.AreEqual("\"1\\n2\\r3\" (System.String)", Dumper.DumpToString("1\n2\r3"));
        }

        [TestMethod]
        public void Test_Dump_Enum()
        {
            Assert.AreEqual("Apple (DebugLibTest.TestEnum)", Dumper.DumpToString(TestEnum.Apple));
            Assert.AreEqual("Apple, Google (DebugLibTest.TestEnum)", Dumper.DumpToString(TestEnum.Apple | TestEnum.Google));
        }

        [TestMethod]
        public void Test_Dump_Null許容型()
        {
            int? v = 1;
            Assert.AreEqual("1 (System.Int32)", Dumper.DumpToString(v));

            int? v2 = null;
            Assert.AreEqual("(null)", Dumper.DumpToString(v2));
        }

        [TestMethod]
        public void Test_Dump_クラス()
        {
            var test = new TestClass() { Number = 1 };
            Assert.AreEqual(@"DebugLibTest.TestClass (DebugLibTest.TestClass)
{
    Number = 1 (System.Int32)
}
", Dumper.DumpToString(test));

            // 空クラス
            var empty = new EmptyClass();
            Assert.AreEqual(@"DebugLibTest.EmptyClass (DebugLibTest.EmptyClass)
{
}
", Dumper.DumpToString(empty));

            // ネストクラス
            var nest = new NestClass()
            {
                Child = new NestClass()
            };
            Assert.AreEqual(@"DebugLibTest.NestClass (DebugLibTest.NestClass)
{
    Child = DebugLibTest.NestClass (DebugLibTest.NestClass)
    {
        Child = (null)
    }
}
", Dumper.DumpToString(nest));

            // 循環参照
            var loop = new NestClass();
            loop.Child = loop;
            Assert.AreEqual(
        @"DebugLibTest.NestClass (DebugLibTest.NestClass)
{
    Child = DebugLibTest.NestClass (DebugLibTest.NestClass)<LoopReference>
}
",
            Dumper.DumpToString(loop));
        }

        [TestMethod]
        public void Test_Dump_構造体()
        {
            var st = new MyStruct(1, 2.3);
            Assert.AreEqual(@"DebugLibTest.MyStruct (DebugLibTest.MyStruct)
{
    IntValue = 1 (System.Int32)
    DoubleValue = 2.3 (System.Double)
}
", st.DumpToString());
        }

        [TestMethod]
        public void Test_Dump_コレクション()
        {
            // List<T>
            var listGeneric = new List<int> { 1, 2 };
            Assert.AreEqual(@"System.Collections.Generic.List`1[System.Int32] (System.Collections.Generic.List`1[System.Int32])
{
    [0] 1 (System.Int32)
    [1] 2 (System.Int32)
}
", Dumper.DumpToString(listGeneric));

            // ArrayList
            var list = new ArrayList { 1, "text" };
            Assert.AreEqual(@"System.Collections.ArrayList (System.Collections.ArrayList)
{
    [0] 1 (System.Int32)
    [1] ""text"" (System.String)
}
", Dumper.DumpToString(list));

            // 空のコレクション
            var emptyList = new List<int> { };
            Assert.AreEqual(@"System.Collections.Generic.List`1[System.Int32] (System.Collections.Generic.List`1[System.Int32])
{
}
", Dumper.DumpToString(emptyList));

            // 2重コレクション
            var doubleList = new List<IList>
            {
                new List<int> { 1, 2 },
                new List<long> { 11, 22 },
            };
            Assert.AreEqual(@"System.Collections.Generic.List`1[System.Collections.IList] (System.Collections.Generic.List`1[System.Collections.IList])
{
    [0] System.Collections.Generic.List`1[System.Int32] (System.Collections.Generic.List`1[System.Int32])
    {
        [0] 1 (System.Int32)
        [1] 2 (System.Int32)
    }
    [1] System.Collections.Generic.List`1[System.Int64] (System.Collections.Generic.List`1[System.Int64])
    {
        [0] 11 (System.Int64)
        [1] 22 (System.Int64)
    }
}
", Dumper.DumpToString(doubleList));

            var classList = new List<TestClass>
            {
                new TestClass() { Number = 1 },
                new TestClass() { Number = 2 }
            };
            Assert.AreEqual(@"System.Collections.Generic.List`1[DebugLibTest.TestClass] (System.Collections.Generic.List`1[DebugLibTest.TestClass])
{
    [0] DebugLibTest.TestClass (DebugLibTest.TestClass)
    {
        Number = 1 (System.Int32)
    }
    [1] DebugLibTest.TestClass (DebugLibTest.TestClass)
    {
        Number = 2 (System.Int32)
    }
}
", classList.DumpToString());

            var classArray = new TestClass[]
            {
                new TestClass() { Number = 1 },
                new TestClass() { Number = 2 }
            };
            Assert.AreEqual(
@"DebugLibTest.TestClass[] (DebugLibTest.TestClass[])
{
    [0] DebugLibTest.TestClass (DebugLibTest.TestClass)
    {
        Number = 1 (System.Int32)
    }
    [1] DebugLibTest.TestClass (DebugLibTest.TestClass)
    {
        Number = 2 (System.Int32)
    }
}
", classArray.DumpToString());

        }

        [TestMethod]
        public void Test_Dump_ジャグ配列_多次元配列()
        {
            var jagg2D = new int[][]
            {
                new [] {1,2,3},
                new [] {4,5,6,7},
            };
            Assert.AreEqual(@"System.Int32[][] (System.Int32[][])
{
    [0][0] 1 (System.Int32)
    [0][1] 2 (System.Int32)
    [0][2] 3 (System.Int32)
    [1][0] 4 (System.Int32)
    [1][1] 5 (System.Int32)
    [1][2] 6 (System.Int32)
    [1][3] 7 (System.Int32)
}
", jagg2D.DumpToString());


            var jagg3D = new int[][][]
            {
                new int[][] {
                    new [] { 1,1 },
                    new [] { 2 },
                    new [] { 3,3,3 },
                },
                new int[][] {
                    new [] { 4 },
                    new [] { 5,5,5 },
                    new [] { 6 },
                }
            };
            Assert.AreEqual(@"System.Int32[][][] (System.Int32[][][])
{
    [0][0][0] 1 (System.Int32)
    [0][0][1] 1 (System.Int32)
    [0][1][0] 2 (System.Int32)
    [0][2][0] 3 (System.Int32)
    [0][2][1] 3 (System.Int32)
    [0][2][2] 3 (System.Int32)
    [1][0][0] 4 (System.Int32)
    [1][1][0] 5 (System.Int32)
    [1][1][1] 5 (System.Int32)
    [1][1][2] 5 (System.Int32)
    [1][2][0] 6 (System.Int32)
}
", jagg3D.DumpToString());


            var multi2D = new int[,]
            {
                 { 1, 2, 3 },
                 { 11, 12, 13 },
            };
            Assert.AreEqual(@"System.Int32[,] (System.Int32[,])
{
    [0,0] 1 (System.Int32)
    [0,1] 2 (System.Int32)
    [0,2] 3 (System.Int32)
    [1,0] 11 (System.Int32)
    [1,1] 12 (System.Int32)
    [1,2] 13 (System.Int32)
}
", multi2D.DumpToString());

            var multi3D = new int[,,]
            {
                 {
                    { 1, 2, 3 },
                    { 4, 5, 6 }
                 },
                 {
                    { 11, 12, 13 },
                    { 14, 15, 16 }
                },
            };
            Assert.AreEqual(@"System.Int32[,,] (System.Int32[,,])
{
    [0,0,0] 1 (System.Int32)
    [0,0,1] 2 (System.Int32)
    [0,0,2] 3 (System.Int32)
    [0,1,0] 4 (System.Int32)
    [0,1,1] 5 (System.Int32)
    [0,1,2] 6 (System.Int32)
    [1,0,0] 11 (System.Int32)
    [1,0,1] 12 (System.Int32)
    [1,0,2] 13 (System.Int32)
    [1,1,0] 14 (System.Int32)
    [1,1,1] 15 (System.Int32)
    [1,1,2] 16 (System.Int32)
}
", multi3D.DumpToString());

            var classArray = new TestClass[,]
            {
                {
                    new TestClass() { Number = 1 },
                    new TestClass() { Number = 2 }
                },
                {
                    new TestClass() { Number = 3 },
                    new TestClass() { Number = 4 }
                }
            };
            Assert.AreEqual(@"DebugLibTest.TestClass[,] (DebugLibTest.TestClass[,])
{
    [0,0] DebugLibTest.TestClass (DebugLibTest.TestClass)
    {
        Number = 1 (System.Int32)
    }
    [0,1] DebugLibTest.TestClass (DebugLibTest.TestClass)
    {
        Number = 2 (System.Int32)
    }
    [1,0] DebugLibTest.TestClass (DebugLibTest.TestClass)
    {
        Number = 3 (System.Int32)
    }
    [1,1] DebugLibTest.TestClass (DebugLibTest.TestClass)
    {
        Number = 4 (System.Int32)
    }
}
", classArray.DumpToString());
        }

        [TestMethod]
        public void Test_Dump_例外()
        {
            var err = new ErrorClass();
            Assert.AreEqual(
        @"DebugLibTest.ErrorClass (DebugLibTest.ErrorClass)
{
    Error = 呼び出しのターゲットが例外をスローしました。
}
",
            Dumper.DumpToString(err));
        }

        [TestMethod]
        public void Test_Dump_Delegate()
        {
            Func<string, int> func = (s) => int.Parse(s);
            Action<string> action = (s) => Console.WriteLine(s);
            DelegateMethod method = i => i * 2;
            Assert.AreEqual("System.Func`2[System.String,System.Int32] (System.Func`2[System.String,System.Int32])",
                func.DumpToString());

            Assert.AreEqual("System.Action`1[System.String] (System.Action`1[System.String])",
                action.DumpToString());

            Assert.AreEqual("DebugLibTest.DelegateMethod (DebugLibTest.DelegateMethod)",
                method.DumpToString());
        }

        #region PropertySetting

        [TestMethod]
        public void Test_Dump_UseOverriddenToString()
        {
            Dumper.UseOverriddenToString = true;

            var over = new OverrideClass();
            Assert.AreEqual("overridden! (DebugLibTest.OverrideClass)", over.DumpToString());

            // DateTime
            var dateTime = new DateTime(2016, 1, 1, 1, 1, 1);
            Assert.AreEqual("2016/01/01 1:01:01 (System.DateTime)", dateTime.DumpToString());
        }

        [TestMethod]
        public void Test_Dump_TypesAsString()
        {
            Dumper.TypesAsString.Add(typeof(DateTime));

            var dateTime = new DateTime(2016, 1, 1, 1, 1, 1);
            Assert.AreEqual("2016/01/01 1:01:01 (System.DateTime)", dateTime.DumpToString());
        }

        [TestMethod]
        public void Test_Dump_IndentSize()
        {
            AssertEx.Throws<ArgumentOutOfRangeException>(() => Dumper.IndentSize = -1);
            Dumper.IndentSize = 0;
            var test = new TestClass();
            Assert.AreEqual(@"DebugLibTest.TestClass (DebugLibTest.TestClass)
{
Number = 0 (System.Int32)
}
", test.DumpToString());
        }

        [TestMethod]
        public void Test_Dump_MaxDepth()
        {
            AssertEx.Throws<ArgumentOutOfRangeException>(() => Dumper.MaxDepth = -1);

            var deep = new DeepClass()
            {
                Number = 0,
                Child = new DeepClass()
                {
                    Number = 1,
                    Child = new DeepClass(),
                }
            };

            Dumper.MaxDepth = 0;
            Assert.AreEqual(@"DebugLibTest.DeepClass (DebugLibTest.DeepClass)
{
    Child = DebugLibTest.DeepClass (DebugLibTest.DeepClass)
    {
        <TooDeep>
    }
    Number = 0 (System.Int32)
}
", deep.DumpToString());

            Dumper.MaxDepth = 1;
            Assert.AreEqual(@"DebugLibTest.DeepClass (DebugLibTest.DeepClass)
{
    Child = DebugLibTest.DeepClass (DebugLibTest.DeepClass)
    {
        Child = DebugLibTest.DeepClass (DebugLibTest.DeepClass)
        {
            <TooDeep>
        }
        Number = 1 (System.Int32)
    }
    Number = 0 (System.Int32)
}
", deep.DumpToString());
        }

        [TestMethod]
        public void Test_Dump_ShowPropertyType()
        {
            Dumper.ShowPropertyType = false;
            var test = new TestClass();
            Assert.AreEqual(@"DebugLibTest.TestClass
{
    Number = 0
}
", test.DumpToString());
        }

        [TestMethod]
        public void Test_Dump_ShowTypeNameOnly()
        {
            Dumper.ShowTypeNameOnly = true;

            string str = "test";
            Assert.AreEqual("\"test\" (String)", str.DumpToString());

            var list = new List<char>() { 'w' };
            Assert.AreEqual(@"System.Collections.Generic.List`1[System.Char] (List`1[Char])
{
    [0] 'w' (Char)
}
", list.DumpToString());

            var tuple = Tuple.Create<string, int>("a", 1);
            Assert.AreEqual(@"(a, 1) (Tuple`2[String,Int32])
{
    Item1 = ""a"" (String)
    Item2 = 1 (Int32)
}
", tuple.DumpToString());
        }

        [TestMethod]
        public void Test_Dump_IgnoreNullProperty()
        {
            Dumper.IgnoreNullProperty = true;

            var deep = new DeepClass()
            {
                Number = 1,
            };

            Assert.AreEqual(@"DebugLibTest.DeepClass (DebugLibTest.DeepClass)
{
    Number = 1 (System.Int32)
}
", deep.DumpToString());

            var list = new List<string> { "ABC", null, "DEF" };
            list.Dump();
            Assert.AreEqual(@"System.Collections.Generic.List`1[System.String] (System.Collections.Generic.List`1[System.String])
{
    [0] ""ABC"" (System.String)
    [1] (null)
    [2] ""DEF"" (System.String)
}
", list.DumpToString());
        }

        #endregion
    }

    delegate int DelegateMethod(int arg);

    public class EmptyClass
    {
    }

    public class TestClass
    {
        public int Number { get; set; }
    }

    public class NestClass
    {
        public NestClass Child { get; set; }
    }

    public class DeepClass
    {
        public DeepClass Child { get; set; }
        public int Number { get; set; }
    }

    public class ErrorClass
    {
        public object Error
        {
            get { throw new Exception("error!"); }
        }
    }

    public class OverrideClass
    {
        public override string ToString()
        {
            return "overridden!";
        }
    }

    public struct MyStruct
    {
        public int IntValue { get; set; }
        public double DoubleValue { get; set; }

        public MyStruct(int intValue, double doubleValue)
        {
            IntValue = intValue;
            DoubleValue = doubleValue;
        }
    }

    [Flags]
    public enum TestEnum
    {
        Apple = 1,
        MS = 2,
        Google = 4,
    }


}
