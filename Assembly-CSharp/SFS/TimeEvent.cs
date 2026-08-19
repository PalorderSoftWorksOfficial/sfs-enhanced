using System;
using UnityEngine;

namespace SFS;

public class TimeEvent : MonoBehaviour
{
	public static TimeEvent main;

	public Action on_100Ms;

	public Action on_10000Ms;

	private void Awake()
	{
		main = this;
	}

	private void LateUpdate()
	{
		float unscaledTime = Time.unscaledTime;
		float num = Time.unscaledTime - Time.unscaledDeltaTime;
		if ((int)(unscaledTime * 10f) > (int)(num * 10f))
		{
			on_100Ms?.Invoke();
		}
		if ((int)(unscaledTime * 0.1f) > (int)(num * 0.1f))
		{
			on_10000Ms?.Invoke();
		}
	}
}
