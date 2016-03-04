using System;
using System.IO;
using DebugLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DebugLibTest
{
    [TestClass]
    public class MatrixDumperTest
    {
        [TestMethod]
        public void _Test_Dump2D()
        {
            var mat = new int[,]
            {
                { 1, 2, 3333 },
                { 4, 555, 6 },
            };

            Assert.AreEqual(
@"   1    2 3333
   4  555    6
", MatrixDumper.DumpMatrixToString(mat));

            Assert.AreEqual(
@"   1,    2, 3333
   4,  555,    6
", MatrixDumper.DumpMatrixToString(mat, ", "));
        }
    }
}
