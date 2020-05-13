using System;
using System.Diagnostics;
using System.Threading;
using System.IO;
using NLog;
using System.Text;

namespace Common.Diagnostics
{
	/// <summary>
	/// Provides the ability to run a process and receive asynchronous output messages.
	/// </summary>
	public class EmbeddedProcessExecutor
	{
		private Logger _log = LogManager.GetCurrentClassLogger();
		private EmbeddedProcessStartInfo _startInfo;
		private Process _process;
		private StringBuilder _standardOutput;
		private StringBuilder _standardError;

		/// <summary>
		/// Gets the process exit code.
		/// </summary>
		public int ExitCode { get; set; }

		public EmbeddedProcessExecutor()
		{
			StandardOutputMessageReceived += CaptureStandardOutput;
			StandardErrorMessageReceived += CaptureStandardError;
		}

		public event EventHandler<EventArgs<string>> StandardOutputMessageReceived;
		public event EventHandler<EventArgs<string>> StandardErrorMessageReceived;
		public event EventHandler<EventArgs<string>> DebugMessage;

		string DoDebugMessage(string format, params object[] args)
		{
			string message = string.Format(format, args);
			if (DebugMessage != null)
			{
				DebugMessage(this, new EventArgs<string>(message));
			}
			return message;
		}

		public string Run(string workingDir, string command, string arguments, string standardOutFile = null)
		{
			return Run(new EmbeddedProcessStartInfo
			{
				WorkingDirectory = workingDir,
				FileName = command,
				Arguments = arguments,
				StandardOutFile = standardOutFile
			});
		}

		public string Run(EmbeddedProcessStartInfo startInfo)
		{
			_standardOutput = new StringBuilder();
			_standardError = new StringBuilder();
			_startInfo = startInfo;

			Thread thdOutput = null;
			Thread thdError = null;
			ProcessStartInfo objStartInfo = new ProcessStartInfo()
			{
				FileName = _startInfo.FileName,
				WorkingDirectory = _startInfo.WorkingDirectory,
				Arguments = _startInfo.Arguments,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};

			_process = new Process();
			_process.StartInfo = objStartInfo;

			_log.Trace(DoDebugMessage("embedded process file name: {0}", objStartInfo.FileName));
			_log.Trace(DoDebugMessage("embedded process command line: {0}", objStartInfo.Arguments));
			_log.Trace(DoDebugMessage("embedded process working dir: {0}", objStartInfo.WorkingDirectory));

			var result = new EmbeddedProcessResult();
			try
			{
				thdOutput = new Thread(new ParameterizedThreadStart(ReadStream));
				thdError = new Thread(new ParameterizedThreadStart(ReadStream));

				_log.Trace(DoDebugMessage("calling process start"));
				_process.Start();

				_log.Trace(DoDebugMessage("starting stream reader threads"));
				thdOutput.Start(new StreamReaderThreadStart(_process.StandardOutput, false, startInfo.StandardOutFile));
				thdError.Start(new StreamReaderThreadStart(_process.StandardError, true));

				//wait for process to finish
				_log.Trace(DoDebugMessage("waiting for external process to exit"));
				_process.WaitForExit();

				//join the stream reader threads to ensure we capture everything
				_log.Trace(DoDebugMessage("joining stream reader threads"));
				thdError.Join(2000);
				thdOutput.Join(2000);

				_log.Trace(DoDebugMessage("process call exited with code {0}", _process.ExitCode));
				ExitCode = _process.ExitCode;

				if (!_process.HasExited)
				{
					try
					{
						_process.Kill();
					}
					catch { /* swallow kill exceptions */ }

					throw new Exception(string.Format(
							"External process for file '{0}' in directory '{1} failed to complete within the specified timeout.",
							_startInfo.FileName,
							_startInfo.WorkingDirectory));
				}
			}
			catch (Exception ex)
			{
				_log.Error(ex, "unexpected exception occurred during external process");
				throw;
			}
			finally
			{
				_log.Trace(DoDebugMessage("closing and disposing process"));
				_process.Close();
				_process.Dispose();
			}



			return _standardOutput.ToString();
		}

		private void CaptureStandardOutput(object sender, EventArgs<string> e)
		{
			_standardOutput.AppendLine(e.Data);
		}

		private void CaptureStandardError(object sender, EventArgs<string> e)
		{
			_standardError.AppendLine(e.Data);
		}


		/// <summary>
		/// Aborts the running process.
		/// </summary>
		/// <returns></returns>
		public bool Abort()
		{
			_log.Trace(DoDebugMessage("external process abort requested"));
			_log.Trace(DoDebugMessage("killing external process ID {0}", _process.Id));
			_process.Kill();
			return true;
		}

		private class StreamReaderThreadStart
		{
			public StreamReaderThreadStart(StreamReader stream, bool isError, string outFile = null)
			{
				Stream = stream;
				IsError = isError;
				OutFile = outFile;
			}
			public StreamReader Stream { get; set; }
			public bool IsError { get; set; }
			public string OutFile { get; set; }
		}

		/// <summary>
		/// Reads from the stream until the external program is ended.
		/// </summary>
		private void ReadStream(object startData)
		{
			StreamReaderThreadStart objStartData = (StreamReaderThreadStart)startData;

			//if (objStartData.OutFile != null)
			//{
			//	var fileStream = File.Create(objStartData.OutFile); // FileStream:Stream
				
			//	using (var writer = new BinaryWriter(fileStream))
			//	using (var reader = new BinaryReader(_process.StandardOutput.BaseStream))
			//	{
			//		//writer.AutoFlush = true;
			//		while(true)
			//		{
			//			reader.
			//			byte nextByte = reader.ReadByte();

			//			if (nextByte == null)
			//				break;

			//			writer.WriteLine(textLine);
			//		}
			//	}

			//	if (File.Exists(_standardOutputFileName))
			//	{
			//		FileInfo info = new FileInfo(_standardOutputFileName);

			//		// if the error info is empty or just contains eof etc.

			//		if (info.Length < 4)
			//			info.Delete();
			//	}
			//}



			while (true)
			{
				string strLine = objStartData.Stream.ReadLine();
				if (strLine == null) break;
				if (objStartData.IsError)
				{
					SendMessage(strLine, StandardErrorMessageReceived);
				}
				else
				{
					SendMessage(strLine, StandardOutputMessageReceived);
				}
			}
		}

		private void SendMessage(string message, EventHandler<EventArgs<string>> handler)
		{
			if (handler != null)
			{
				handler(this, new EventArgs<string>(message));
			}
		}

		/// <summary>
		/// Runs the provided command with args.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static int Run(string command, string args)
		{
			return Run(command, args, null, null, null);
		}

		/// <summary>
		/// Runs the provided command with args with optional message callbacks.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static int Run(string command, string args,
			EventHandler<EventArgs<string>> stdOutCallback,
			EventHandler<EventArgs<string>> stdErrCallback,
			EventHandler<EventArgs<string>> debugCallback
		)
		{
			EmbeddedProcessExecutor proc = new EmbeddedProcessExecutor();
			var startInfo = new EmbeddedProcessStartInfo();
			startInfo.FileName = command;
			startInfo.Arguments = args;
			if(stdOutCallback != null)
			{
				proc.StandardOutputMessageReceived += stdOutCallback;
			}
			if(debugCallback != null)
			{
				proc.DebugMessage += debugCallback;
			}
			if(stdErrCallback != null)
			{
				proc.StandardErrorMessageReceived += stdErrCallback;
			}
			proc.Run(startInfo);
			return proc.ExitCode;
		}

		/// <summary>
		/// Runs the provided command with args for the console with basic console output of standard output and error.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static int RunForConsole(string command, string args)
		{
			return Run(command, args, proc_Message, proc_StandardOutputMessageReceived, proc_Message);
		}

		private static void proc_StandardOutputMessageReceived(object sender, EventArgs<string> e)
		{
			Console.Error.WriteLine(e.Data);
		}

		private static void proc_Message(object sender, EventArgs<string> e)
		{
			Console.WriteLine(e.Data);
		}
	}

}
