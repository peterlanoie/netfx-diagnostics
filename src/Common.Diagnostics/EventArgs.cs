using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Diagnostics
{
	/// <summary>
	/// Defines a generic event args type that includes the Data property of type <typeparamref name="DataType"/>.
	/// </summary>
	/// <typeparam name="DataType"></typeparam>
	public class EventArgs<DataType> : EventArgs
	{
		public DataType Data { get; set; }

		public EventArgs()
		{
		}

		public EventArgs(DataType data)
		{
			Data = data;
		}

	}
}
