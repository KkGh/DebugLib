using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DebugLib
{
	/// <summary>
	/// クラスプロパティの列挙機能を提供する。
	/// </summary>
	public static class Enumeration
	{
		/* 
		 * TODO
		 *	ArrayDumperのコンストラクタでWrite指定
		 *	String.FormatのLoopSignatureのメソッド化
		 *	型情報を表示するかの設定
		 *	
		 */
		private const string LoopSignature = "<LoopReference>";
		private const string MaxDeepSignature = "<TooDeep>";
		private static readonly string NewLine = Environment.NewLine;
		private static readonly Dictionary<string, string> EscapedChars = new Dictionary<string, string>
		{
			{ "\0", "\\0" }, { "\a", "\\a" }, { "\b", "\\b" }, { "\f", "\\f" }, { "\n", "\\n" }, { "\r", "\\r" }, { "\t", "\\t" }, { "\v", "\\v" }
		};
		private const int DefaultIndentSize = 4;	// public?
		private const int DefaultMaxDepth = 5;
		private static readonly BindingFlags DefaultAccessFlags = BindingFlags.Public | BindingFlags.Instance;
		private const bool DefaultShowPropertyType = true;
		private static int indentSize = 4;
		private static int maxDepth = 5;
		private static BindingFlags accessFlags = DefaultAccessFlags;
		private static bool showPropertyType = DefaultShowPropertyType;

		/// <summary>
		/// 0以上のインデント幅を取得または設定する。
		/// </summary>
		public static int IndentSize
		{
			get { return indentSize; }
			set
			{
				if (value < 0) throw new ArgumentOutOfRangeException();
				indentSize = value;
			}
		}

		/// <summary>
		/// 0以上の最大のプロパティの深さを取得または設定する。
		/// 0を設定した場合はダンプ対象となるオブジェクトが持つプロパティのみを列挙する。
		/// </summary>
		public static int MaxDepth
		{
			get { return maxDepth; }
			set
			{
				if (value < 0) throw new ArgumentOutOfRangeException();
				maxDepth = value;
			}
		}

		/// <summary>
		/// 列挙するプロパティのアクセスフラグを取得または設定する。
		/// </summary>
		public static BindingFlags AccessFlags
		{
			get { return accessFlags; }
			set { accessFlags = value; }
		}

		/// <summary>
		/// プロパティの型情報を表示するかを取得または設定する。
		/// </summary>
		public static bool ShowPropertyType
		{
			get { return showPropertyType; }
			set { showPropertyType = value; }
		}

		/// <summary>
		/// 指定されたオブジェクトの内容を再帰的に出力する。
		/// </summary>
		/// <param name="obj"></param>
		public static void Dump(this object obj)
		{
			Console.WriteLine(DumpToString(obj));
		}

		/// <summary>
		/// 指定されたオブジェクトの内容を再帰的に文字列化する。
		/// インスタンスはプロパティ、コレクションは各要素、プリミティブは値のみを
		/// 文字列化する。
		/// 例外が発生した場合は値の代わりに例外メッセージを文字列化する。
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static string DumpToString(this object obj)
		{
			if (IsPrimitiveOrNull(obj))
				return ValueToString(obj);

			// 循環参照検知用スタック
			var propertyPath = new Stack();

			var sb = new StringBuilder();
			DumpSubItem(obj, sb, propertyPath);

			return sb.ToString();
		}

		private static void DumpObject(object obj, StringBuilder sb, Stack propertyPath)
		{
			int depth = propertyPath.Count;
			string indent = CreateIndent(depth);

			var properties = obj.GetType().GetProperties(AccessFlags);
			foreach (var p in properties)
			{
				try
				{
					var value = p.GetValue(obj);
					bool isLooping = propertyPath.Contains(value);
					sb.AppendFormat("{0}{1} = {2}{3}" + NewLine,
						indent, p.Name, ValueToString(value), isLooping ? LoopSignature : "");

					if (isLooping) continue;

					DumpSubItem(value, sb, propertyPath);
				}
				catch (Exception ex)
				{
					// GetValueで例外有り
					sb.AppendFormat("{0}{1} = {2}" + NewLine, indent, p.Name, ex.Message);
				}
			}
		}

		private static void DumpCollection(IEnumerable enumerable, StringBuilder sb, Stack propertyPath)
		{
			int depth = propertyPath.Count;
			string indent = CreateIndent(depth);
			int i = 0;

			foreach (var item in enumerable)
			{
				bool isLooping = propertyPath.Contains(item);
				sb.AppendFormat("{0}[{1}] {2}{3}" + NewLine,
					indent, i, ValueToString(item), isLooping ? LoopSignature : "");
				i++;

				if (isLooping) continue;

				DumpSubItem(item, sb, propertyPath);
			}
		}

		/// <summary>
		/// 配列をダンプする。
		/// ジャグ配列、多次元配列もダンプ可能。
		/// </summary>
		/// <param name="array"></param>
		private static void DumpArray(Array array, StringBuilder sb, Stack propertyPath)
		{
			int depth = propertyPath.Count;
			string indent = CreateIndent(depth);

			//// 混合した配列はダンプできないので文字列化する

			if (array.Rank >= 2)
			{
				// 多次元配列
				var dumper = new MultiDimensionArrayDumper(array);
				dumper.Write += (s, e) =>
				{
					bool isLooping = propertyPath.Contains(e.Obj);
					string index = "[" + string.Join(",", e.Indexes) + "]";
					sb.AppendFormat("{0}{1} {2}{3}" + NewLine,
							indent, index, ValueToString(e.Obj), isLooping ? LoopSignature : "");
				};
				dumper.Dump();
			}
			else
			{
				// ジャグ配列
				var dumper = new JaggedArrayDumper(array);
				dumper.Write += (s, e) =>
				{
					bool isLooping = propertyPath.Contains(e.Obj);
					string index = "[" + string.Join("][", e.Indexes) + "]";
					sb.AppendFormat("{0}{1} {2}{3}" + NewLine,
							indent, index, ValueToString(e.Obj), isLooping ? LoopSignature : "");
				};
				dumper.Dump();
			}
		}

		private static void DumpSubItem(object value, StringBuilder sb, Stack propertyPath)
		{
			if (IsPrimitiveOrNull(value)) return;

			int depth = propertyPath.Count;
			string indent = CreateIndent(depth);

			// オブジェクトのプロパティまたはコレクションの各要素のダンプ開始
			sb.AppendLine(indent + "{");
			propertyPath.Push(value);

			if (depth <= MaxDepth)
			{
				if (value is Array)
				{
					DumpArray((Array)value, sb, propertyPath);
				}
				else if (value is IEnumerable)
				{
					DumpCollection((IEnumerable)value, sb, propertyPath);
				}
				else
				{
					DumpObject(value, sb, propertyPath);
				}
			}
			else
			{
				sb.AppendLine(CreateIndent(depth + 1) + MaxDeepSignature);
			}

			// ダンプ終了
			sb.AppendLine(indent + "}");
			propertyPath.Pop();
		}

		// FIX:メソッド名 enumも含む
		private static bool IsPrimitiveOrNull(object obj)
		{
			if (obj == null)
				return true;

			// string,decimalはIsPrimitive = falseなので明示する
			var type = obj.GetType();
			if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
				return true;

			// enum
			if (obj is Enum)
				return true;

			return false;
		}

		private static string ValueToString(object value)
		{
			if (value == null)
				return "(null)";

			var type = value.GetType();
			string escapedValue = EspaceString(value.ToString());

			string str =
				(type == typeof(string)) ? "\"" + escapedValue + "\"" :
				(type == typeof(char)) ? "'" + escapedValue + "'" :
				escapedValue;

			if (ShowPropertyType)
			{
				str += " (" + type + ")";
			}

			return str;
		}


		private static string EspaceString(string str)
		{
			var sb = new StringBuilder(str);
			foreach (var converter in EscapedChars)
			{
				sb.Replace(converter.Key, converter.Value);
			}

			return sb.ToString();
		}

		private static string CreateIndent(int depth)
		{
			return new string(' ', depth * IndentSize);
		}

		private class JaggedArrayDumper
		{
			private List<int> indexes = new List<int>();
			private Array array;

			public JaggedArrayDumper(Array array)
			{
				if (array == null) throw new ArgumentNullException("array");
				if (array.Rank != 1) throw new ArgumentException("ジャグ配列のみ指定できます。");
				this.array = array;
			}

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

					if (subArray != null && subArray.Rank == 1)
					{
						DumpRecursive(subArray);
					}
					else
					{
						OnWrite(new WriteEventArgs(item, indexes));
					}

					indexes.RemoveAt(indexes.Count - 1);
				}
			}

			public event EventHandler<WriteEventArgs> Write;
			protected virtual void OnWrite(WriteEventArgs e)
			{
				if (Write != null)
					Write(this, e);
			}
		}

		private class MultiDimensionArrayDumper
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
						OnWrite(new WriteEventArgs(value, indexes));
					}

					indexes.RemoveAt(indexes.Count - 1);
				}
			}

			public event EventHandler<WriteEventArgs> Write;
			protected virtual void OnWrite(WriteEventArgs e)
			{
				if (Write != null)
					Write(this, e);
			}
		}

		private class WriteEventArgs : EventArgs
		{
			public WriteEventArgs(object obj, IEnumerable<int> indexes)
			{
				this.Obj = obj;
				this.Indexes = indexes;
			}

			public object Obj { get; private set; }
			public IEnumerable<int> Indexes { get; private set; }
		}
	}
}
