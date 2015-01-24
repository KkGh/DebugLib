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
		 * MAYBE:設定可能？
		 *	BindingFlags
		 *	インデントサイズ
		 *	maxDeep
		 */

		private const int IndentSize = 4;
		private const int MaxDeep = 5;
		private const string LoopSignature = "<LoopReference>";
		private const string MaxDeepSignature = "<TooDeep>";
		private static readonly BindingFlags AccessFlags = BindingFlags.Public | BindingFlags.Instance;
		private static readonly Dictionary<string, string> EscapedChars = new Dictionary<string, string>
		{
			{ "\0", "\\0" },
			{ "\a", "\\a" },
			{ "\b", "\\b" },
			{ "\f", "\\f" },
			{ "\n", "\\n" },
			{ "\r", "\\r" },
			{ "\t", "\\t" },
			{ "\v", "\\v" },
		};

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
		public static string DumpToString(object obj)
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
			int deep = propertyPath.Count;
			string indent = CreateIndent(deep);

			var properties = obj.GetType().GetProperties(AccessFlags);
			foreach (var p in properties)
			{
				try
				{
					var value = p.GetValue(obj);
					bool isLooping = propertyPath.Contains(value);
					sb.AppendFormat("{0}{1} = {2}{3}\r\n",
						indent, p.Name, ValueToString(value), isLooping ? LoopSignature : "");

					if (isLooping) continue;

					DumpSubItem(value, sb, propertyPath);
				}
				catch (Exception ex)
				{
					// GetValueで例外有り
					sb.AppendFormat("{0}{1} = {2}\r\n", indent, p.Name, ex.Message);
				}
			}
		}

		private static void DumpCollection(IEnumerable enumerable, StringBuilder sb, Stack propertyPath)
		{
			int deep = propertyPath.Count;
			string indent = CreateIndent(deep);

			int i = 0;
			foreach (var item in enumerable)
			{
				bool isLooping = propertyPath.Contains(item);
				sb.AppendFormat("{0}[{1}] {2}{3}\r\n",
					indent, i, ValueToString(item), isLooping ? LoopSignature : "");
				i++;

				if (isLooping) continue;

				DumpSubItem(item, sb, propertyPath);
			}
		}

		private static void DumpSubItem(object value, StringBuilder sb, Stack propertyPath)
		{
			if (IsPrimitiveOrNull(value)) return;

			int deep = propertyPath.Count;
			string indent = CreateIndent(deep);

			// オブジェクトのプロパティまたはコレクションの各要素のダンプ開始
			propertyPath.Push(value);
			sb.AppendLine(indent + "{");

			if (deep < MaxDeep)
			{
				if (value is IEnumerable)
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
				sb.AppendLine(CreateIndent(deep + 1) + MaxDeepSignature);
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
			if (value == null) return "(null)";

			var type = value.GetType();
			string escapedValue = EspaceString(value.ToString());

			if (type == typeof(string))
				return string.Format("\"{0}\" ({1})", escapedValue, value.GetType());

			if (type == typeof(char))
				return string.Format("'{0}' ({1})", escapedValue, value.GetType());

			return string.Format("{0} ({1})", escapedValue, value.GetType());
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

		private static string CreateIndent(int deep)
		{
			return new string(' ', deep * IndentSize);
		}
	}
}
