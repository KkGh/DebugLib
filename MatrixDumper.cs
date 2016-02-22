using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebugLib
{
    /// <summary>
    /// 2次元配列のダンプ機能を提供する。
    /// </summary>
	public static class MatrixDumper
	{
		/// <summary>
		/// 2次元配列を行列形式で出力する。
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <param name="separator"></param>
		public static void DumpMatrix<T>(this T[,] source, string separator = " ")
		{
			Console.WriteLine(source.DumpMatrixToString(separator));
		}

		/// <summary>
		/// 2次元配列を行列形式で文字列化する。
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <param name="separator"></param>
		/// <returns></returns>
		public static string DumpMatrixToString<T>(this T[,] source, string separator = " ")
		{
			int height = source.GetLength(0);
			int width = source.GetLength(1);
			int maxLength = source.Cast<T>().Max(x => x.ToString().Length);
			string format = "{0," + maxLength + "}";
			var sb = new StringBuilder();

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					sb.AppendFormat(format, source[y, x]);
					if (x != width - 1)
					{
						sb.Append(separator);
					}
				}

				sb.AppendLine();
			}

			return sb.ToString();
		}
	}
}
