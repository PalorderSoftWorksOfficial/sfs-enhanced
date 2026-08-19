using System;
using UnityEngine;

namespace SFS.Variables;

[Serializable]
public abstract class Composed<T>
{
	[SerializeField]
	protected VariablesModule variables;

	private bool initialized;

	private T value;

	public T Value
	{
		get
		{
			CheckInitialize();
			return value;
		}
		set
		{
			if (!Equals(this.value, value))
			{
				T arg = this.value;
				this.value = value;
				onChange?.Invoke();
				onChangeOldNew?.Invoke(arg, value);
			}
		}
	}

	public Composed<T> OnChange
	{
		get
		{
			return this;
		}
		set
		{
		}
	}

	private event Action onChange;

	private event Action<T, T> onChangeOldNew;

	public static Composed<T> operator +(Composed<T> a, Action b)
	{
		a.CheckInitialize();
		b();
		a.onChange += b;
		return a;
	}

	public static Composed<T> operator -(Composed<T> a, Action b)
	{
		a.onChange -= b;
		return a;
	}

	public static Composed<T> operator +(Composed<T> a, Action<T, T> b)
	{
		a.CheckInitialize();
		b(a.Value, a.Value);
		a.onChangeOldNew += b;
		return a;
	}

	public static Composed<T> operator -(Composed<T> a, Action<T, T> b)
	{
		a.onChangeOldNew -= b;
		return a;
	}

	private void CheckInitialize()
	{
		if (!initialized)
		{
			initialized = Application.isPlaying;
			Value = GetResult(initialized);
		}
	}

	protected void Recalculate()
	{
		Value = GetResult(initialize: false);
	}

	protected abstract T GetResult(bool initialize);

	protected abstract bool Equals(T a, T b);
}
