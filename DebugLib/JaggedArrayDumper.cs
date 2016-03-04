using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebugLib
{
    internal class JaggedArrayDumper
    {
        private List<int> indexes = new List<int>();
        private Array array;

        public JaggedArrayDumper(Array array)
        {
            if (array == null) throw new ArgumentNullException("array");
            if (array.Rank != 1) throw new ArgumentException("ジャグ配列のみ指定できます。");
            this.array = array;
        }

        public Action<object, List<int>> Write { get; set; }

        public void Dump()
        {
            DumpRecursive(this.array);
        }

        private void DumpRecursive(Array array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                var item = array.GetValue(i);
                var subArray = item as Array;
                indexes.Add(i);

                // 配列
                if (subArray != null && subArray.Rank == 1)
                {
                    DumpRecursive(subArray);
                }
                // 要素
                else
                {
                    Write(item, indexes);
                }

                indexes.RemoveAt(indexes.Count - 1);
            }
        }
    }
}
