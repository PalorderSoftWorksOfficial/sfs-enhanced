using System;
using System.Collections.Generic;
using SFS.Input;
using UnityEngine;

namespace SFS.Core;

public class ActionQueue : MonoBehaviour
{
	public static ActionQueue main;

	private Queue<Action> toCall = new Queue<Action>();

	public Transform holder;

	public void Awake()
	{
		main = this;
	}

	private void Start()
	{
	}

	public void QueueMenu(Func<Screen_Base> toOpen)
	{
		QueueAction(delegate
		{
			ScreenManager.main.OpenScreen(toOpen);
		});
	}

	public void QueueAction(Action action)
	{
		lock (toCall)
		{
			toCall.Enqueue(action);
		}
	}

	public void Update()
	{
		lock (toCall)
		{
			if (toCall.Count > 0)
			{
				toCall.Dequeue()();
			}
		}
	}
}
