using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace Common.Diagnostics
{
	public class HttpTraceSqlInfoMessageHandler
	{
		private HttpTraceWriter _trace;

		public HttpTraceSqlInfoMessageHandler() : this(null) { }

		public HttpTraceSqlInfoMessageHandler(string traceCategory)
		{
			_trace = new HttpTraceWriter(traceCategory);
		}

		/// <summary>
		/// Handles Sql connection info messages.  
		/// Bind it to the SqlConnection.InfoMessage event.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void OnInfoMessage(object sender, SqlInfoMessageEventArgs e)
		{
			_trace.WriteTrace("Source: {0}; Message: {1}", e.Source, e.Message);
			foreach(SqlError error in e.Errors)
			{
				_trace.WriteTrace("ERROR: Line: {0}; Number: {1}; Procedure: {2}; Message: {3}", error.LineNumber, error.Number, error.Procedure, error.Message);
			}
		}

		public void OnStateChange(object sender, StateChangeEventArgs e)
		{
			_trace.WriteTrace("SQL connection state changed from {0} to {1}", e.OriginalState, e.CurrentState);
		}


	}
}
