using System;

namespace Common.Diagnostics
{
	public class CompletionComputer
	{
		private DateTime _startTime;
		private int _totalItems;

		public CompletionComputer()
		{
		}

		public void Start(int totalItems)
		{
			_startTime = DateTime.Now;
			_totalItems = totalItems;
		}

		public object GetStatus(int currentItemIndex)
		{
			throw new NotImplementedException();
			//var progress = new CompletionProgress();
			//progress.Current = currentItemIndex;
			//	return progress;
		}

		/// <summary>
		/// Gets a timespan of the estimated completion time of all items in the set.
		/// </summary>
		/// <param name="currentItemIndex">Zero based index of the current item.</param>
		/// <returns></returns>
		public TimeSpan GetEstimatedCompletionTimespan(int currentItemIndex)
		{
			if (currentItemIndex < 0)
			{
				throw new InvalidOperationException("Current item index must be at least 0.");
			}
			currentItemIndex++;
			var elapsed = DateTime.Now - _startTime;
			var avgItemSeconds = elapsed.TotalSeconds / (double)currentItemIndex;
			var totalRemainingSeconds = avgItemSeconds * (double)(_totalItems - currentItemIndex);
			return TimeSpan.FromSeconds(totalRemainingSeconds);
		}

		public DateTime GetEstimatedCompletionTime(int currentItemIndex)
		{
			return DateTime.Now.Add(GetEstimatedCompletionTimespan(currentItemIndex));
		}
	}
}