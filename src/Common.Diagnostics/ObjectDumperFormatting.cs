using System.IO;
using System.Reflection;
using System;
using System.Web;

namespace Common.Diagnostics
{
	/// <summary>
	/// Defines the formatting options used for dumper output.
	/// </summary>
	public class ObjectDumperFormatting
	{
		public bool CanDrawAsTable { get; set; }
		public string DumperHeader { get; set; }
		public string ErrorFormat { get; set; }
		public string IndentString { get; set; }
		public string ListHeaderFormat { get; set; }
		public string ListItemPrefix { get; set; }
		public string ListItemSuffix { get; set; }
		public string ListPrefix { get; set; }
		public string ListSuffix { get; set; }
		public string NonValueFormat { get; set; }
		public string ObjectHeaderFormat { get; set; }
		public string ObjectPrefix { get; set; }
		public string ObjectSuffix { get; set; }
		public string PropNamePrefix { get; set; }
		public string PropNameSuffix { get; set; }
		public string PropValuePrefix { get; set; }
		public string PropValueSuffix { get; set; }

		/// <summary>
		/// The prefix for a table header row.
		/// </summary>
		public string TableHeaderPrefix { get; set; }
		/// <summary>
		/// The suffix for a table header row.
		/// </summary>
		public string TableHeaderSuffix { get; set; }

		public string TableHeaderFormat { get; set; }

		/// <summary>
		/// The format for one member in the header row.
		/// </summary>
		public string TableHeaderMemberFormat { get; set; }
		/// <summary>
		/// The prefix for each item row in the table.
		/// </summary>
		public string TableItemPrefix { get; set; }
		/// <summary>
		/// The suffix for each item in the table.
		/// </summary>
		public string TableItemSuffix { get; set; }

		public string TableItemValuePrefix { get; set; }

		public string TableItemValueSuffix { get; set; }

		public Func<string, string> ValueEscaper { get; set; }

		public ObjectDumperFormatting()
		{
			ValueEscaper = x => x; // default behavior is just to return value as-is;
		}

		internal string MakePropValueString(object value)
		{
			return string.Format("{0}{1}{2}", PropValuePrefix, ValueEscaper(value.ToString()), PropValueSuffix);
		}

		internal string MakePropNameString(string propName)
		{
			return string.Format("{0}{1}{2}", PropNamePrefix, propName, PropNameSuffix);
		}

		internal string MakeNonValueString(string nonValue)
		{
			return string.Format(NonValueFormat, nonValue);
		}

		internal string MakeNonValuePropString(string nonValue)
		{
			return MakePropValueString(MakeNonValueString(nonValue));
		}

		internal string MakeErrorString(string error)
		{
			return string.Format(ErrorFormat, error);
		}

		internal string MakeTableHeader(string headerText, int memberCount)
		{
			return string.Format(TableHeaderFormat, headerText, memberCount);
		}

		/// <summary>
		/// Returns the option set for default formatting (PlainText)
		/// </summary>
		public static ObjectDumperFormatting Default { get { return ObjectDumperFormatting.PlainText; } }

		/// <summary>
		/// Returns the option set for plain text formatting.
		/// </summary>
		public static ObjectDumperFormatting PlainText
		{
			get
			{
				return new ObjectDumperFormatting()
				{
					CanDrawAsTable = false,
					IndentString = "   ",
					ErrorFormat = "[*** {0} ***]",
					ListHeaderFormat = "{{{0}}}",
					ListItemPrefix = "",
					ListItemSuffix = "",
					ListPrefix = "",
					ListSuffix = "",
					NonValueFormat = "[{0}]",
					ObjectHeaderFormat = "{{{0}}}",
					ObjectPrefix = "",
					ObjectSuffix = "",
					PropNamePrefix = "",
					PropNameSuffix = ": ",
					PropValuePrefix = "",
					PropValueSuffix = "",
				};
			}
		}

		/// <summary>
		/// Returns the option set for Square Bracket formatting.
		/// </summary>
		public static ObjectDumperFormatting SquareBracketMarkup
		{
			get
			{
				return new ObjectDumperFormatting()
				{
					CanDrawAsTable = false,
					IndentString = "   ",
					ErrorFormat = "[*** {0} ***]",
					ListHeaderFormat = "",
					ListItemPrefix = "[ITEM {{{0}}}]",
					ListItemSuffix = "[/ITEM]",
					ListPrefix = "[LIST {{{0}}}]",
					ListSuffix = "[/LIST]",
					NonValueFormat = "[{0}]",
					ObjectHeaderFormat = "",
					ObjectPrefix = "[OBJECT {{{0}}}]",
					ObjectSuffix = "[/OBJECT]",
					PropNamePrefix = "[PROPNAME]",
					PropNameSuffix = "[/PROPNAME]",
					PropValuePrefix = "[VALUE]",
					PropValueSuffix = "[/VALUE]"
				};
			}
		}

		private const string HttpItemKey = "objectDumper-headerWritten";

		/// <summary>
		/// Returns the option set for html formatting.
		/// </summary>
		public static ObjectDumperFormatting Html
		{
			get
			{
				ObjectDumperFormatting options = new ObjectDumperFormatting()
				{
					CanDrawAsTable = true,
					IndentString = "",
					ErrorFormat = "<span class=\"dump-error\">[*** {0} ***]</span>",
					ListHeaderFormat = "<tr><th class=\"dump-header\">{0}</th></tr>",
					ListItemPrefix = "<tr><td class=\"dump-item\">",
					ListItemSuffix = "</td></tr>",
					ListPrefix = "<table class=\"dump\" cellspacing=\"0\">",
					ListSuffix = "</table>",
					NonValueFormat = "<span class=\"dump-non-value\">[{0}]</span>",
					ObjectHeaderFormat = "<tr><th colspan=\"2\" class=\"dump-header\">{0}</th></tr>",
					ObjectPrefix = "<table class=\"dump\" cellspacing=\"0\">",
					ObjectSuffix = "</table>",
					PropNamePrefix = "<tr><th class=\"dump-prop-name\">",
					PropNameSuffix = "</th>",
					PropValuePrefix = "<td class=\"dump-prop-val\">",
					PropValueSuffix = "</td></tr>",
					TableHeaderPrefix = "<tr>",
					TableHeaderSuffix = "</tr>",
					TableHeaderFormat = "<tr><th class=\"dump-header\" colspan=\"{1}\">{0}</th></tr>",
					TableHeaderMemberFormat = "<th class=\"dump-prop-name\">{0}</th>",
					TableItemPrefix = "<tr>",
					TableItemSuffix = "</tr>",
					TableItemValuePrefix = "<td class=\"dump-prop-val\">",
					TableItemValueSuffix = "</td>",
					// do a simple HTML encode
					ValueEscaper = x => x.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") 
				};

				var headerWritten = false;
				var context = HttpContext.Current;
				if(context != null)
				{
					if(context.Items[HttpItemKey] != null)
					{
						headerWritten = true;
					}
					else
					{
						context.Items[HttpItemKey] = true;
					}
				}
				if(!headerWritten)
				{
					StreamReader reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(ObjectDumperFormatting), "ObjectDumper.css"));
					options.DumperHeader = string.Format("<style type=\"text/css\">{0}</style>", reader.ReadToEnd());
					reader.Close();
				}

				return options;
			}
		}

	}
}
