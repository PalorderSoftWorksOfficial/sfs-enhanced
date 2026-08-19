using System;
using System.Collections.Generic;
using UnityEngine;

namespace SFS.Tutorials;

public abstract class Tutorial_Base : MonoBehaviour
{
	private class TimeDelay
	{
		private float delay;

		private float startTime;

		private bool activated;

		private void Activate()
		{
			if (!activated)
			{
				startTime = Time.realtimeSinceStartup;
			}
			activated = true;
		}

		public bool Ended()
		{
			Activate();
			return Time.realtimeSinceStartup >= startTime + delay;
		}

		public TimeDelay(float delay)
		{
			this.delay = delay;
		}
	}

	private Queue<Func<bool>> steps = new Queue<Func<bool>>();

	protected void Add_ShowPopup(GameObject popup, Func<bool> a)
	{
		Add_Action(delegate
		{
			popup.SetActive(value: true);
		});
		Add_Check(a);
		Add_Action(delegate
		{
			PopupAnimation component = popup.GetComponent<PopupAnimation>();
			if (component != null)
			{
				component.Disable();
			}
			else
			{
				popup.SetActive(value: false);
			}
		});
	}

	protected void Add_Action(Action a)
	{
		steps.Enqueue(delegate
		{
			a();
			return true;
		});
	}

	protected void Add_Check(Func<bool> a)
	{
		steps.Enqueue(a);
	}

	protected void Add_Delay(float delayTime)
	{
		TimeDelay timeDelay = new TimeDelay(delayTime);
		steps.Enqueue(() => timeDelay.Ended());
	}

	private void LateUpdate()
	{
		if (steps.Count == 0)
		{
			base.enabled = false;
		}
		else if (steps.Peek()())
		{
			steps.Dequeue();
		}
	}
}
