using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DebugLib
{
    /// <summary>
    /// クラスのプロパティを列挙する機能を提供する。
    /// </summary>
    public static class Dumper
    {
        private const string LoopSignature = "<LoopReference>";
        private const string MaxDepthSignature = "<TooDeep>";
        private static readonly string NewLine = Environment.NewLine;
        private static readonly Dictionary<string, string> EscapedChars = new Dictionary<string, string>
        {
            { "\0", "\\0" }, { "\a", "\\a" }, { "\b", "\\b" }, { "\f", "\\f" }, { "\n", "\\n" }, { "\r", "\\r" }, { "\t", "\\t" }, { "\v", "\\v" }
        };
        private const int DefaultIndentSize = 4;    // public? DefaultValueAttributeにして各プロパティに紐づける？
        private const int DefaultMaxDepth = 5;
        private static readonly BindingFlags DefaultAccessFlags = BindingFlags.Public | BindingFlags.Instance;
        private const bool DefaultShowPropertyType = true;
        private const bool DefaultEnumerateDelegate = false;
        private const bool DefaultUseOverriddenToString = false;
        private const bool DefaultShowTypeNameOnly = false;
        private const bool DefaultIgnoreNullProperty = false;

        private static int maxDepth;
        private static int indentSize;

        static Dumper()
        {
            Reset();
        }

        /// <summary>
        /// 0以上のインデント幅を取得または設定する。
        /// デフォルト値は4。
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
        /// デフォルト値は5。
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
        /// デフォルト値は BindingFlags.Public | BindingFlags.Instance。
        /// </summary>
        public static BindingFlags AccessFlags { get; set; }

        /// <summary>
        /// プロパティの型情報を表示するかを取得または設定する。
        /// デフォルト値はtrue。
        /// </summary>
        public static bool ShowPropertyType { get; set; }

        /// <summary>
        /// プロパティの型情報を表示する時、型の名前空間を省略し、
        /// 型名のみを表示するかを取得または設定する。
        /// デフォルト値はfalse。
        /// </summary>
        public static bool ShowTypeNameOnly { get; set; }

        /// <summary>
        /// 再帰的に列挙せず、文字列化する型のコレクションを取得または設定する。
        /// </summary>
        public static List<Type> TypesAsString { get; set; }

        /// <summary>
        /// デリゲートを再帰的に列挙するかを取得または設定する。
        /// デフォルト値はfalse。
        /// </summary>
        public static bool EnumerateDelegate { get; set; }

        /// <summary>
        /// 型がToStringメソッドをオーバーライドしている場合に、再帰的に列挙せず、
        /// ToStringメソッドのみによって文字列化するかを取得または設定する。
        /// デフォルト値はfalse。
        /// </summary>
        public static bool UseOverriddenToString { get; set; }

        /// <summary>
        /// プロパティの値がnullの場合に、プロパティのダンプを省略するかを
        /// 取得または設定する。
        /// デフォルト値はfalse。
        /// </summary>
        public static bool IgnoreNullProperty { get; set; }

        /// <summary>
        /// 全ての設定プロパティをデフォルト値に戻す。
        /// </summary>
        public static void Reset()
        {
            IndentSize = DefaultIndentSize;
            MaxDepth = DefaultMaxDepth;
            AccessFlags = DefaultAccessFlags;
            ShowPropertyType = DefaultShowPropertyType;
            ShowTypeNameOnly = DefaultShowTypeNameOnly;
            TypesAsString = new List<Type>();
            EnumerateDelegate = DefaultEnumerateDelegate;
            UseOverriddenToString = DefaultUseOverriddenToString;
            IgnoreNullProperty = DefaultIgnoreNullProperty;
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
            if (ShouldDumpAsString(obj))
                return ValueToString(obj);

            // 循環参照検知用スタック
            var propertyPath = new Stack();

            var sb = new StringBuilder();
            sb.AppendLine(ValueToString(obj));
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
                    object value = p.GetValue(obj);
                    if (IgnoreNullProperty && value == null) continue;

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
                dumper.Write = (obj, indexes) =>
                {
                    bool isLooping = propertyPath.Contains(obj);
                    string index = "[" + string.Join(",", indexes) + "]";
                    sb.AppendFormat("{0}{1} {2}{3}" + NewLine,
                            indent, index, ValueToString(obj), isLooping ? LoopSignature : "");
                    DumpSubItem(obj, sb, propertyPath);
                };
                dumper.Dump();
            }
            else
            {
                // ジャグ配列
                var dumper = new JaggedArrayDumper(array);
                dumper.Write = (obj, indexes) =>
                {
                    bool isLooping = propertyPath.Contains(obj);
                    string index = "[" + string.Join("][", indexes) + "]";
                    sb.AppendFormat("{0}{1} {2}{3}" + NewLine,
                            indent, index, ValueToString(obj), isLooping ? LoopSignature : "");
                    DumpSubItem(obj, sb, propertyPath);
                };
                dumper.Dump();
            }
        }

        private static void DumpSubItem(object value, StringBuilder sb, Stack propertyPath)
        {
            if (ShouldDumpAsString(value)) return;

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
                sb.AppendLine(CreateIndent(depth + 1) + MaxDepthSignature);
            }

            // ダンプ終了
            sb.AppendLine(indent + "}");
            propertyPath.Pop();
        }

        /// <summary>
        /// objectを再帰でなく文字列としてダンプするかどうかを判定する。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static bool ShouldDumpAsString(object obj)
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

            // delegate
            if (obj is Delegate && !EnumerateDelegate)
                return true;

            // ユーザー指定
            if (TypesAsString != null && TypesAsString.Contains(type))
                return true;

            // ToStringをオーバーライド
            if (UseOverriddenToString && IsToStringOverridden(type))
                return true;

            return false;
        }

        private static string ValueToString(object value)
        {
            if (value == null) return "(null)";

            // 値
            var type = value.GetType();
            string escapedValue = EscapeString(value.ToString());
            string str =
                (type == typeof(string)) ? "\"" + escapedValue + "\"" :
                (type == typeof(char)) ? "'" + escapedValue + "'" :
                escapedValue;

            // 型
            if (ShowPropertyType)
            {
                string typeText;
                if (ShowTypeNameOnly)
                {
                    typeText = type.Name;
                    if (type.GenericTypeArguments.Any())
                    {
                        typeText += $"[{string.Join(",", type.GenericTypeArguments.Select(t => t.Name))}]";
                    }
                }
                else
                {
                    typeText = type.ToString();
                }

                str += " (" + typeText + ")";
            }

            return str;
        }

        /// <summary>
        /// 指定した型のToStringメソッドがオーバーライドされているかどうかを判定する。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static bool IsToStringOverridden(Type type)
        {
            var method = type.GetMethod("ToString", new Type[0]);
            return method.DeclaringType != typeof(object);
        }

        private static string EscapeString(string str)
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
    }
}
