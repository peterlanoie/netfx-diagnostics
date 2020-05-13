using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Common.Diagnostics
{
	internal class HttpTraceWriter
	{
		private string _category;

		public HttpTraceWriter() { }

		public HttpTraceWriter(string traceCategory)
		{
			_category = traceCategory;
		}

		public void WriteTrace(string format, params object[] args)
		{
			WriteTrace(string.Format(format, args));
		}

		public void WriteTrace(string value)
		{
			HttpContext context = HttpContext.Current;
			if(context != null)
			{
				context.Trace.Write(_category, value);
			}
		}

	}
}
