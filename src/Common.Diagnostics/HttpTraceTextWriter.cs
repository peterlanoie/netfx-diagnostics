using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Common.Diagnostics
{
	public class HttpTraceTextWriter : System.IO.TextWriter
	{
		private HttpTraceWriter _trace;

		public HttpTraceTextWriter() : this(null) { }

		public HttpTraceTextWriter(string traceCategory)
		{
			_trace = new HttpTraceWriter(traceCategory);
		}

		public override Encoding Encoding
		{
			get { throw new NotImplementedException(); }
		}

		public override void Write(string value)
		{
			_trace.WriteTrace(value);
			base.Write(value);
		}

		public override void WriteLine(string value)
		{
			_trace.WriteTrace(value);
			base.WriteLine(value);
		}

	}
}
