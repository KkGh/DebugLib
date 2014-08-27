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
		 * 設定可能？
		 * BindingFlags,
		 * インデントサイズ、
		 * ValueToString、
		 * プロパティの書式、
		 * コレクション項目を列挙するか、
		 * maxDeep,
		 * 
		 */

		private const int IndentSize = 4;
		private const int MaxDeep = 5;
		private const string LoopSignature = "<循環参照>";
		private static readonly BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

		/*
		private readonly static Dictionary<char, string> convertEscapeSequences = new Dictionary<char, string>
		{
			{ '\0', "\\0" },
			{ '\a', "\\a" },
			{ '\b', "\\b" },
			{ '\f', "\\f" },
			{ '\n', "\\n" },
			{ '\r', "\\r" },
			{ '\t', "\\t" },
			{ '\v', "\\v" },
		};
		*/

		/// <summary>
		/// 指定されたオブジェクトの内容を再帰的に出力する。
		/// </summary>
		/// <param name="obj"></param>
		public static void Damp(this object obj)
		{
			Console.WriteLine(DampToString(obj));
		}

		/// <summary>
		/// 指定されたオブジェクトの内容を再帰的に文字列化する。
		/// インスタンスはプロパティ、コレクションは各要素、プリミティブは値のみを
		/// 文字列化する。
		/// 例外が発生した場合は値の代わりに例外メッセージを文字列化する。
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static string DampToString(object obj)
		{
			if (IsPrimitiveOrNull(obj))
				return ValueToString(obj);

			// 循環参照検知用スタック
			var propertyPath = new Stack();

			var sb = new StringBuilder();
			DampValue(obj, obj.GetType(), sb, propertyPath);

			return sb.ToString();
		}

		private static void DampObject(object obj, StringBuilder sb, Stack propertyPath)
		{
			int deep = propertyPath.Count;
			if (deep > MaxDeep)
			{
				return;
			}

			string indent = CreateIndent(deep);
			var properties = obj.GetType().GetProperties(flags);

			foreach (var p in properties)
			{
				try
				{
					var value = p.GetValue(obj);
					bool isLoop = propertyPath.Contains(value);
					sb.AppendFormat("{0}{1} = {2}{3}\r\n",
						indent, p.Name, ValueToString(value), isLoop ? LoopSignature : "");

					// 循環参照による無限ループを防止
					if (isLoop) continue;

					DampValue(value, p.PropertyType, sb, propertyPath);
				}
				catch (Exception ex)
				{
					sb.AppendFormat("{0}{1} = {2}\r\n", indent, p.Name, ex.Message);
				}
			}
		}

		private static void DampCollection(IEnumerable enumerable, StringBuilder sb, Stack propertyPath)
		{
			int deep = propertyPath.Count;
			if (deep > MaxDeep)
				return;

			string indent = CreateIndent(deep);
			
			int i = 0;
			foreach (var item in enumerable)
			{
				bool isLoop = propertyPath.Contains(item);
				sb.AppendFormat("{0}[{1}] {2}{3}\r\n",
					indent, i, ValueToString(item), isLoop ? LoopSignature : "");
				i++;

				// 循環参照による無限ループを防止
				if (isLoop) continue;

				DampValue(item, item.GetType(), sb, propertyPath);
			}
		}

		private static void DampValue(object value, Type type, StringBuilder sb, Stack propertyPath)
		{
			if (IsPrimitiveOrNull(value)) return;

			propertyPath.Push(value);

			// オブジェクト・コレクションは括弧で括る
			int deep = propertyPath.Count;
			string bracketsIndent = CreateIndent(deep - 1);
			sb.AppendLine(bracketsIndent + "{");

			if (value is IEnumerable)
			{
				DampCollection((IEnumerable)value, sb, propertyPath);
			}
			else
			{
				DampObject(value, sb, propertyPath);
			}
	
			sb.AppendLine(bracketsIndent + "}");

			propertyPath.Pop();
		}

		private static bool IsPrimitiveOrNull(object obj)
		{
			if (obj == null)
				return true;

			// string,decimalはIsPrimitive = falseなので明示する
			var type = obj.GetType();
			if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
				return true;

			return false;
		}

		private static string ValueToString(object value)
		{
			if (value == null) return "(null)";

			var type = value.GetType();

			if (type == typeof(string))
				return "\"" + value + "\"";

			if (type == typeof(char))
				return "\'" + value + "\'";

			return value.ToString();
		}

		private static string CreateIndent(int deep)
		{
			return new string(' ', deep * IndentSize);
		}
	}
}
