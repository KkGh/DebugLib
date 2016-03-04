using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebugLib
{
    internal class MultiDimensionArrayDumper
    {
        private List<int> indexes = new List<int>();
        private Array array;

        public MultiDimensionArrayDumper(Array array)
        {
            if (array == null) throw new ArgumentNullException("array");
            if (array.Rank == 1) throw new ArgumentException("多次元配列のみ指定できます。");
            this.array = array;
        }

        public void Dump()
        {
            DumpRecursive(0);
        }

        private void DumpRecursive(int currentDimension)
        {
            int length = array.GetLength(currentDimension);
            for (int i = 0; i < length; i++)
            {
                indexes.Add(i);

                if (currentDimension < array.Rank - 1)
                {
                    DumpRecursive(currentDimension + 1);
                }
                else
                {
                    var value = array.GetValue(indexes.ToArray());
                    Write(value, indexes);
                }

                indexes.RemoveAt(indexes.Count - 1);
            }
        }

        public Action<object, List<int>> Write;
    }

}
