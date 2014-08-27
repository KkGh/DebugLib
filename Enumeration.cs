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
		/// オブジェクトの持つプロパティをコンソールに出力する。
		/// </summary>
		/// <param name="source"></param>
		public static void DLOutput(this object source)
		{
			Console.WriteLine(EnumeratePropertiesRecursive(source));
		}

		/// <summary>
		/// コレクションの要素をコンソールに出力する。
		/// </summary>
		/// <param name="source"></param>
		public static void DLOutput(this IEnumerable source)
		{
			Console.WriteLine(EnumerateEnumerableRecursive(source));
		}

		/// <summary>
		/// インスタンスのプロパティを "名前 = 値" 形式で再帰的に列挙する。
		/// コレクション型プロパティは名前の代わりにインデックスを表示する。
		/// プロパティのgetterで例外が発生した場合は値の代わりに例外メッセージを表示する。
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static string EnumeratePropertiesRecursive(object obj)
		{
			var sb = new StringBuilder();

			// 循環参照検知用スタック
			var propertyPath = new Stack();
			propertyPath.Push(obj);

			// obj自身のプロパティの深さ(deep)を0として再帰列挙
			EnumeratePropertiesRecursive(obj, sb, propertyPath);

			return sb.ToString();
		}

		/// <summary>
		/// コレクションの各要素を "インデックス 値" 形式で再帰的に列挙する。
		/// </summary>
		/// <param name="enumerable"></param>
		/// <returns></returns>
		public static string EnumerateEnumerableRecursive(IEnumerable enumerable)
		{
			var sb = new StringBuilder();

			// 循環参照検知用スタック
			var propertyPath = new Stack();
			propertyPath.Push(enumerable);

			// コレクションの要素の深さ(deep)を0として再帰列挙
			EnumerateEnumerableRecursive(enumerable, sb, propertyPath);

			return sb.ToString();
		}

		private static void EnumeratePropertiesRecursive(object obj, StringBuilder sb, Stack propertyPath)
		{
			int deep = propertyPath.Count - 1;
			if (deep > MaxDeep) return;

			string indent = CreateIndent(deep);
			var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

			foreach (var p in properties)
			{
				try
				{
					var value = p.GetValue(obj);
					bool isLoop = propertyPath.Contains(value);
					sb.AppendFormat("{0}{1} = {2}{3}\n", indent, p.Name, ValueToString(value), isLoop ? "<循環参照>" : "");

					// 循環参照による無限ループを防止
					if (isLoop) continue;

					MoveToSubProperty(value, p.PropertyType, sb, propertyPath);
				}
				catch (Exception ex)
				{
					sb.AppendFormat("{0}{1} = {2}\n", indent, p.Name, ex.Message);
				}
			}
		}

		private static void EnumerateEnumerableRecursive(IEnumerable enumerable, StringBuilder sb, Stack propertyPath)
		{
			int deep = propertyPath.Count - 1;
			if (deep > MaxDeep) return;

			string indent = CreateIndent(deep);
			int index = 0;

			foreach (var item in enumerable)
			{
				bool isLoop = propertyPath.Contains(item);
				sb.AppendFormat("{0}[{1}] {2}{3}\n", indent, index, ValueToString(item), isLoop ? "<循環参照>" : "");
				index++;

				// 循環参照による無限ループを防止
				if (isLoop) continue;

				MoveToSubProperty(item, item.GetType(), sb, propertyPath);
			}
		}

		private static void MoveToSubProperty(object value, Type type, StringBuilder sb, Stack propertyPath)
		{
			if (value == null) return;

			// プリミティブは何もしない(string,decimalはIsPrimitiveがfalseになるので明示的に追加する)
			if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal)) return;

			propertyPath.Push(value);

			if (value is IEnumerable)
			{
				// コレクション
				EnumerateEnumerableRecursive((IEnumerable)value, sb, propertyPath);
			}
			else
			{
				// 非コレクション
				EnumeratePropertiesRecursive(value, sb, propertyPath);
			}

			propertyPath.Pop();
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
