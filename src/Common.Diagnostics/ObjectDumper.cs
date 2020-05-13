using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web.UI;
using System.Text;
using System.Linq;
using Common.Diagnostics;

namespace Common.Diagnostics
{
	public class ObjectDumper
	{
		private const int DEFAULT_MAX_DEPTH = 10;
		private static readonly string[] DEFAULT_IGNORE_MEMBERS = new string[] { };
		private List<object> _objects = new List<object>();
		private ObjectDumperFormatting _formatting = ObjectDumperFormatting.Default;
		private TextWriter _writer;
		private int _depth = 0;
		private int _maxDepth = 10;
		private List<string> _ignoreMembers;

		private ObjectDumper(TextWriter writer)
		{
			_writer = writer;
		}

		private ObjectDumper(TextWriter writer, ObjectDumperFormatting formatting, string[] ignoreMembers)
			: this(writer)
		{
			_formatting = formatting;
			_ignoreMembers = new List<string>(ignoreMembers);
		}

		private void Dump(object obj)
		{
			if(obj == null || obj is ValueType || obj is string)
			{
				// Handle non-object types
				if(obj == null)
				{
					_writer.Write(_formatting.MakeNonValueString("null"));
				}
				else
				{
					_writer.Write(_formatting.ValueEscaper(obj.ToString()));
				}
			}
			else
			{
				// Handle object types
				IEnumerable enumerableElement = obj as IEnumerable;
				if(enumerableElement != null)
				{
					DumpList(enumerableElement);
				}
				else
				{
					DumpObject(obj);
				}
			}
		}

		private bool SafeToDive()
		{
			if(_depth >= _maxDepth)
			{
				WriteLine(_formatting.ErrorFormat, string.Format("Reached the current max depth of {0}. Call the dumper with a specific depth to go deeper.", _maxDepth));
				return false;
			}
			return true;
		}

		private void DumpList(IEnumerable list)
		{
			_depth++;
			if(SafeToDive())
			{
				WriteLine(_formatting.ListPrefix, MakeTypeDescriptor(list));
				if(_formatting.CanDrawAsTable && true)
				{
					var enumerator = list.GetEnumerator();

					if(enumerator.MoveNext())
					{
						var item = enumerator.Current;
						var itemType = item.GetType();
						var members = GetMembers(itemType);
						var memberCount = members.Count();
						var showMembers = memberCount > 0 && !itemType.IsValueType && itemType != typeof(string);
						WriteLine(_formatting.MakeTableHeader(MakeTypeDescriptor(list), memberCount));
						if(showMembers)
						{
							Write(_formatting.TableHeaderPrefix);
							foreach(var m in members)
							{
								Write(_formatting.TableHeaderMemberFormat, m.Name);
							}
							WriteLine(_formatting.TableHeaderSuffix);
						}
						do
						{
							item = enumerator.Current;
							Write(_formatting.TableItemPrefix);
							if(showMembers)
							{
								foreach(var m in members)
								{
									Write(_formatting.TableItemValuePrefix);
									Dump(GetMemberValue(item, m));
									Write(_formatting.TableItemValueSuffix);
								}
							}
							else
							{
								Write(_formatting.TableItemValuePrefix);
								Dump(item);
								Write(_formatting.TableItemValueSuffix);
							}
							Write(_formatting.TableItemSuffix);
						} while(enumerator.MoveNext());
						//					WriteLine(string.Concat(_formatting.MakePropNameString(m.Name), _formatting.MakePropValueString(value)));
					}
					else
					{
						WriteLine(_formatting.MakeTableHeader(MakeTypeDescriptor(list), 1));
						Write(_formatting.TableItemPrefix);
						Write(_formatting.TableItemValuePrefix);
						Write(_formatting.MakeNonValueString("list is empty"));
						Write(_formatting.TableItemValueSuffix);
						Write(_formatting.TableItemSuffix);
					}
				}
				else
				{
					WriteLine(_formatting.ListHeaderFormat, MakeTypeDescriptor(list));
					foreach(object item in list)
					{
						WriteLine(_formatting.ListItemPrefix, item.GetType());
						if(item is ValueType || item is string)
						{
							WriteLine(item);
						}
						else
						{
							Dump(item);
						}
						WriteLine(_formatting.ListItemSuffix);
					}
				}
				WriteLine(_formatting.ListSuffix);
			}
			_depth--;
		}

		private MemberInfo[] GetMembers(Type type)
		{
			//			var type = obj.GetType();

			// get all field and property type members that aren't in the ignore list
			return type
				.GetMembers(BindingFlags.Public | BindingFlags.Instance)
				.Where(m => !_ignoreMembers.Contains(m.Name) && (m is FieldInfo || m is PropertyInfo)).ToArray();
		}

		private void DumpObject(object obj)
		{
			MemberInfo[] members;

			if(_objects.Contains(obj))
			{
				WriteLine(_formatting.MakeNonValueString(string.Format("Skipped, cyclic or redundant reference (#{0})", obj.GetHashCode())));
				return;
			}
			_objects.Add(obj);

			var type = obj.GetType();
			WriteLine(_formatting.ObjectPrefix, MakeTypeDescriptor(obj));
			WriteLine(_formatting.ObjectHeaderFormat, MakeTypeDescriptor(obj));

			// get all field and property type members that aren't in the ignore list
			members = GetMembers(type);

			foreach(var m in members)
			{
				DumpObjectMember(obj, m);
			}
			WriteLine(_formatting.ObjectSuffix);
		}

		private void DumpObjectMember(object obj, MemberInfo m)
		{
			// check for the custom ignore attribute
			var noDumpAttrib = m.GetCustomAttributes(typeof(DumperIgnoreAttribute), true);
			if (noDumpAttrib.Length > 0)
			{
				return;
			}

			object value = GetMemberValue(obj, m);
			if(value != null)
			{
				if(value is ValueType || value is string)
				{
					WriteLine(string.Concat(_formatting.MakePropNameString(m.Name), _formatting.MakePropValueString(value)));
				}
				else
				{
					WriteLine(_formatting.MakePropNameString(m.Name));
					WriteLine(_formatting.PropValuePrefix);
					_depth++;
					if(SafeToDive())
					{
						Dump(value);
					}
					_depth--;
					WriteLine(_formatting.PropValueSuffix);
				}
			}
			else
			{
				WriteLine(string.Concat(_formatting.MakePropNameString(m.Name), _formatting.MakeNonValuePropString("null")));
			}
		}

		private object GetMemberValue(object obj, MemberInfo m)
		{
			FieldInfo field = m as FieldInfo;
			PropertyInfo prop = m as PropertyInfo;
			object value = null;
			//if(field != null || prop != null)
			//{
			//}

			if(prop != null)
			{
				if(prop.GetIndexParameters().Length == 0)
				{
					try
					{
						value = prop.GetValue(obj, null);
					}
					catch(Exception ex)
					{
						value = string.Format("Attempt to get value resulted in an exception: {0}", ex.Message);
					}
				}
				else
				{
					value = _formatting.MakeNonValueString("type indexer skipped");
				}
			}
			else
			{
				value = field.GetValue(obj);
			}
			return value;
		}

		//private string[] ExtractValueArray(MemberInfo[] members, object obj)
		//{
		//    string[] values;

		//    foreach(var m in members)
		//    {
		//        FieldInfo field = m as FieldInfo;
		//        PropertyInfo prop = m as PropertyInfo;

		//        if(field != null || prop != null)
		//        {
		//            object value = null;
		//            if(prop != null)
		//            {
		//                if(prop.GetIndexParameters().Length == 0)
		//                {
		//                    try
		//                    {
		//                        value = prop.GetValue(obj, null);
		//                    }
		//                    catch(Exception ex)
		//                    {
		//                        value = string.Format("Attempt to get value resulted in an exception: {0}", ex.Message);
		//                    }
		//                }
		//                else
		//                {
		//                    value = _formatting.MakeNonValueString("type indexer skipped");
		//                }
		//            }
		//            else
		//            {
		//                value = field.GetValue(obj);
		//            }
		//            if(value != null)
		//            {
		//                if(value is ValueType || value is string)
		//                {
		//                    WriteLine(string.Concat(_formatting.MakePropNameString(m.Name), _formatting.MakePropValueString(value)));
		//                }
		//                else
		//                {
		//                    WriteLine(_formatting.MakePropNameString(m.Name));
		//                    WriteLine(_formatting.PropValuePrefix);
		//                    _depth++;
		//                    if(SafeToDive())
		//                    {
		//                        Dump(value);
		//                    }
		//                    _depth--;
		//                    WriteLine(_formatting.PropValueSuffix);
		//                }
		//            }
		//            else
		//            {
		//                WriteLine(string.Concat(_formatting.MakePropNameString(m.Name), _formatting.MakeNonValuePropString("null")));
		//            }
		//        }

		//    }

		//    return result;
		//}

		private string MakeTypeDescriptor(object value)
		{
			return string.Format("{0} (#{1})", value.GetType().ToString().Replace(",", ", "), value.GetHashCode());
		}

		private void WriteLine(object obj)
		{
			WriteLine(obj.ToString());
		}

		private void WriteLine(string format, params object[] args)
		{
			Write(format, args);
			_writer.WriteLine();
		}

		private void Write(string format, params object[] args)
		{
			if(format != null && format.Trim() != "")
			{
				if(!string.IsNullOrEmpty(_formatting.IndentString))
				{
					for(int i = 0; i < _depth; i++) _writer.Write(_formatting.IndentString);
				}
				if(args.Length > 0)
				{
					_writer.Write(format, args);
				}
				else
				{
					_writer.Write(format);
				}
			}
		}

		public static string DumpAsText(object obj)
		{
			return DumpAsText(obj, DEFAULT_IGNORE_MEMBERS, DEFAULT_MAX_DEPTH);
		}

		public static string DumpAsText(object obj, int maxDepth)
		{
			return DumpAsText(obj, DEFAULT_IGNORE_MEMBERS, maxDepth);
		}

		public static string DumpAsText(object obj, string[] ignoreMembers)
		{
			return DumpAsText(obj, ignoreMembers, DEFAULT_MAX_DEPTH);
		}

		public static string DumpAsText(object obj, string[] ignoreMembers, int maxDepth)
		{
			StringBuilder Builder = new StringBuilder();
			using(TextWriter TX = new StringWriter(Builder))
			{
				DumpAsText(obj, TX, ignoreMembers, maxDepth);
				TX.Flush();
				return Builder.ToString();
			}
		}

		public static void DumpAsText(object obj, TextWriter writer)
		{
			DumpAsText(obj, writer, DEFAULT_IGNORE_MEMBERS, DEFAULT_MAX_DEPTH);
		}

		public static void DumpAsText(object obj, TextWriter writer, string[] ignoreMembers)
		{
			DumpAsText(obj, writer, ignoreMembers, DEFAULT_MAX_DEPTH);
		}

		public static void DumpAsText(object obj, TextWriter writer, int maxDepth)
		{
			DumpAsText(obj, writer, DEFAULT_IGNORE_MEMBERS, maxDepth);
		}

		public static void DumpAsText(object obj, TextWriter writer, string[] ignoreMembers, int maxDepth)
		{
			DumpWithOptions(obj, writer, ObjectDumperFormatting.PlainText, ignoreMembers, maxDepth);
		}

		public static string DumpAsHtml(object obj)
		{
			return DumpAsHtml(obj, DEFAULT_IGNORE_MEMBERS, DEFAULT_MAX_DEPTH);
		}

		public static string DumpAsHtml(object obj, int maxDepth)
		{
			return DumpAsHtml(obj, DEFAULT_IGNORE_MEMBERS, maxDepth);
		}

		public static string DumpAsHtml(object obj, string[] ignoreMembers)
		{
			return DumpAsHtml(obj, ignoreMembers, DEFAULT_MAX_DEPTH);
		}

		public static string DumpAsHtml(object obj, string[] ignoreMembers, int maxDepth)
		{
			StringBuilder Builder = new StringBuilder();
			using(TextWriter TX = new StringWriter(Builder))
			{
				DumpAsHtml(obj, TX, ignoreMembers, maxDepth);
				TX.Flush();
				return Builder.ToString();
			}
		}

		public static void DumpAsHtml(object obj, TextWriter writer)
		{
			DumpAsHtml(obj, writer, DEFAULT_IGNORE_MEMBERS, DEFAULT_MAX_DEPTH);
		}

		public static void DumpAsHtml(object obj, TextWriter writer, int maxDepth)
		{
			DumpAsHtml(obj, writer, DEFAULT_IGNORE_MEMBERS, maxDepth);
		}

		public static void DumpAsHtml(object obj, TextWriter writer, string[] ignoreMembers)
		{
			DumpAsHtml(obj, writer, ignoreMembers, DEFAULT_MAX_DEPTH);
		}

		public static void DumpAsHtml(object obj, TextWriter writer, string[] ignoreMembers, int maxDepth)
		{
			DumpWithOptions(obj, writer, ObjectDumperFormatting.Html, ignoreMembers, maxDepth);
		}

		public static void Dump(object obj, TextWriter writer, ObjectDumperFormatting options)
		{
			DumpWithOptions(obj, writer, options, DEFAULT_IGNORE_MEMBERS, DEFAULT_MAX_DEPTH);
		}

		public static void Dump(object obj, TextWriter writer, ObjectDumperFormatting options, string[] ignoreMembers)
		{
			DumpWithOptions(obj, writer, options, ignoreMembers, DEFAULT_MAX_DEPTH);
		}

		public static void Dump(object obj, TextWriter writer, ObjectDumperFormatting options, int maxDepth)
		{
			DumpWithOptions(obj, writer, options, DEFAULT_IGNORE_MEMBERS, maxDepth);
		}

		public static void Dump(object obj, TextWriter writer, ObjectDumperFormatting options, string[] ignoreMembers, int maxDepth)
		{
			DumpWithOptions(obj, writer, options, ignoreMembers, maxDepth);
		}

		private static void DumpWithOptions(object obj, TextWriter writer, ObjectDumperFormatting options, string[] ignoreMembers, int maxDepth)
		{
			ObjectDumper dumper = new ObjectDumper(writer, options, ignoreMembers);
			dumper._maxDepth = maxDepth;
			writer.WriteLine(options.DumperHeader);
			dumper.Dump(obj);
		}

		private class DumpTable
		{
			public string[] Header { get; set; }
			public string[][] RowData { get; set; }
		}

	}

}

namespace System
{

	public static class ObjectExtensions
	{
		public static void DumpAsHtml(this object obj, TextWriter writer)
		{
			ObjectDumper.DumpAsHtml(obj, writer);
		}
		public static void DumpAsHtml(this object obj, TextWriter writer, int maxDepth)
		{
			ObjectDumper.DumpAsHtml(obj, writer, maxDepth);
		}
		public static void DumpAsHtml(this object obj, TextWriter writer, string[] ignoreMembers)
		{
			ObjectDumper.DumpAsHtml(obj, writer, ignoreMembers);
		}
		public static void DumpAsHtml(this object obj, TextWriter writer, string[] ignoreMembers, int maxDepth)
		{
			ObjectDumper.DumpAsHtml(obj, writer, ignoreMembers, maxDepth);
		}


		public static string DumpAsHtml(this object obj)
		{
			return ObjectDumper.DumpAsHtml(obj);
		}
		public static string DumpAsHtml(this object obj, int maxDepth)
		{
			return ObjectDumper.DumpAsHtml(obj, maxDepth);
		}
		public static string DumpAsHtml(this object obj, string[] ignoreMembers)
		{
			return ObjectDumper.DumpAsHtml(obj, ignoreMembers);
		}
		public static string DumpAsHtml(this object obj, string[] ignoreMembers, int maxDepth)
		{
			return ObjectDumper.DumpAsHtml(obj, ignoreMembers, maxDepth);
		}


		public static void DumpAsText(this object obj, TextWriter writer)
		{
			ObjectDumper.DumpAsText(obj, writer);
		}
		public static void DumpAsText(this object obj, TextWriter writer, int maxDepth)
		{
			ObjectDumper.DumpAsText(obj, writer, maxDepth);
		}
		public static void DumpAsText(this object obj, TextWriter writer, string[] ignoreMembers)
		{
			ObjectDumper.DumpAsText(obj, writer, ignoreMembers);
		}
		public static void DumpAsText(this object obj, TextWriter writer, string[] ignoreMembers, int maxDepth)
		{
			ObjectDumper.DumpAsText(obj, writer, ignoreMembers, maxDepth);
		}

		public static string DumpAsText(this object obj)
		{
			return ObjectDumper.DumpAsText(obj);
		}
		public static string DumpAsText(this object obj, int maxDepth)
		{
			return ObjectDumper.DumpAsText(obj, maxDepth);
		}
		public static string DumpAsText(this object obj, string[] ignoreMembers)
		{
			return ObjectDumper.DumpAsText(obj, ignoreMembers);
		}
		public static string DumpAsText(this object obj, string[] ignoreMembers, int maxDepth)
		{
			return ObjectDumper.DumpAsText(obj, ignoreMembers, maxDepth);
		}


		public static void Dump(this object obj, TextWriter writer, ObjectDumperFormatting options)
		{
			ObjectDumper.Dump(obj, writer, options);
		}
		public static void Dump(this object obj, TextWriter writer, ObjectDumperFormatting options, int maxDepth)
		{
			ObjectDumper.Dump(obj, writer, options, maxDepth);
		}
		public static void Dump(this object obj, TextWriter writer, ObjectDumperFormatting options, string[] ignoreMembers)
		{
			ObjectDumper.Dump(obj, writer, options, ignoreMembers);
		}
		public static void Dump(this object obj, TextWriter writer, ObjectDumperFormatting options, string[] ignoreMembers, int maxDepth)
		{
			ObjectDumper.Dump(obj, writer, options, ignoreMembers, maxDepth);
		}

	}

}