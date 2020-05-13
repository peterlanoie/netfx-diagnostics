using System;
using System.Text;

namespace Common.Diagnostics
{
	/// <summary>
	/// Provides System.Diagnostics.Trace writing functionality for types expecting a TextWriter.
	/// </summary>
	public class TraceTextWriter : System.IO.TextWriter
	{
		private readonly string _category = null;

		public TraceTextWriter()
		{
		}

		public TraceTextWriter(string category)
		{
			_category = category;
		}

		public override void Write(char[] buffer, int index, int count)
		{
			System.Diagnostics.Trace.Write(new String(buffer, index, count), _category);
		}

		public override void Write(string value)
		{
			System.Diagnostics.Trace.Write(value, _category);
		}

		public override Encoding Encoding
		{
			get { return System.Text.Encoding.Default; }
		}
	}
}