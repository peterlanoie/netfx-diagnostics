using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Diagnostics.Test.Cli
{
	class Program
	{
		static void Main(string[] args)
		{
			var computer = new CompletionComputer();
			var total = 30;
			computer.Start(total);
			var rand = new Random();
			for (int i = 0; i < total; i++)
			{
				Thread.Sleep(rand.Next(500, 1000));
				//var status = computer.GetStatus(i + 1);
				Console.WriteLine(computer.GetEstimatedCompletionTime(i + 1));
			}
		}
	}
}
