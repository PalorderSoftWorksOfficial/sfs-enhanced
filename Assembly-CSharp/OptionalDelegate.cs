using System;

public class OptionalDelegate<T>
{
	private Action emptyDelegate;

	private Action<T> singleDelegate;

	public int CallCount { get; private set; }

	public bool IsEmpty => CallCount < 1;

	public static OptionalDelegate<T> operator +(OptionalDelegate<T> a, Action b)
	{
		a.emptyDelegate = (Action)Delegate.Combine(a.emptyDelegate, b);
		a.CallCount++;
		return a;
	}

	public static OptionalDelegate<T> operator +(OptionalDelegate<T> a, Action<T> b)
	{
		a.singleDelegate = (Action<T>)Delegate.Combine(a.singleDelegate, b);
		a.CallCount++;
		return a;
	}

	public static OptionalDelegate<T> operator -(OptionalDelegate<T> a, Action b)
	{
		a.emptyDelegate = (Action)Delegate.Remove(a.emptyDelegate, b);
		a.CallCount--;
		return a;
	}

	public static OptionalDelegate<T> operator -(OptionalDelegate<T> a, Action<T> b)
	{
		a.singleDelegate = (Action<T>)Delegate.Remove(a.singleDelegate, b);
		a.CallCount--;
		return a;
	}

	public void Invoke(T data)
	{
		emptyDelegate?.Invoke();
		singleDelegate?.Invoke(data);
	}

	public void Clear()
	{
		emptyDelegate = null;
		singleDelegate = null;
		CallCount = 0;
	}
}
