using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Diagnostics
{
	public class EmbeddedProcessStartInfo
	{
		/// <summary>
		///	Gets or sets the set of command-line arguments to use when starting the application.
		/// </summary>
		public string Arguments { get; set; }

		/// <summary>
		///		Gets or sets the application or document to start.
		/// </summary>
		public string FileName { get; set; }

		/// <summary>
		/// Gets or sets the initial directory for the process to be started.
		/// </summary>
		public string WorkingDirectory { get; set; }
		public string StandardOutFile { get; internal set; }
	}
}
