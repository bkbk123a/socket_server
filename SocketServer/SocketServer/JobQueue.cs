using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core;

public interface IJobQueue
{
	public void Push(Action job);
}

public class JobQueue : IJobQueue
{
	private Queue<Action> _jobQueue = new Queue<Action>();
	private object _lock = new object();
	private bool _flush = false;

	public void Push(Action job)
	{
		bool flush = false;

		lock (_lock)
		{
			_jobQueue.Enqueue(job);
			if (!_flush)
            {
				flush = _flush = true;
			}
		}

		if (flush)
        {
			Flush();
		}
	}

	private void Flush()
	{
		while (true)
		{
			Action action = Pop();
			if (action is null)
            {
				return;
			}

			action.Invoke();
		}
	}

	private Action Pop()
	{
		lock (_lock)
		{
			if (_jobQueue.Count == 0)
			{
				_flush = false;
				return null;
			}
			return _jobQueue.Dequeue();
		}
	}
}
