using System;

namespace SFS.Variables;

[Serializable]
public class Event_Local
{
	private event Action onChange;

	public static Event_Local operator +(Event_Local a, Action b)
	{
		b();
		a.onChange += b;
		return a;
	}

	public static Event_Local operator -(Event_Local a, Action b)
	{
		a.onChange -= b;
		return a;
	}

	public void Invoke()
	{
		onChange?.Invoke();
	}
}
