using System;
using System.Collections.Generic;

public class MainThread : Singleton<MainThread>
{
	private volatile bool idle;

	private List<Action> actionsQueue;

	private List<Action> actionsQueueCopy;

	private void Update()
	{
		if (!idle)
		{
			actionsQueueCopy.Clear();
			lock (actionsQueue)
			{
				actionsQueueCopy.AddRange(actionsQueue);
				actionsQueue.Clear();
				idle = true;
			}
			for (int i = 0; i < actionsQueueCopy.Count; i++)
			{
				actionsQueueCopy[i]();
			}
		}
	}

	public void Init()
	{
		idle = true;
		actionsQueue = new List<Action>();
		actionsQueueCopy = new List<Action>();
	}

	public void Execute(Action action)
	{
		if (action == null)
		{
			return;
		}
		lock (actionsQueue)
		{
			actionsQueue.Add(action);
			idle = false;
		}
	}
}
