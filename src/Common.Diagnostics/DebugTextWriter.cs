using System;
using System.Text;

namespace Common.Diagnostics
{
	/// <summary>
	/// Provides System.Diagnostics.Debug writing functionality for types expecting a TextWriter.
	/// </summary>
	public class DebugTextWriter : System.IO.TextWriter
	{
		private readonly string _category = null;

		public DebugTextWriter()
		{
		}

		public DebugTextWriter(string category)
		{
			_category = category;
		}

		public override void Write(char[] buffer, int index, int count)
		{
			System.Diagnostics.Debug.Write(new String(buffer, index, count), _category);
		}

		public override void Write(string value)
		{
			System.Diagnostics.Debug.Write(value, _category);
		}

		public override Encoding Encoding
		{
			get { return System.Text.Encoding.Default; }
		}
	}
}