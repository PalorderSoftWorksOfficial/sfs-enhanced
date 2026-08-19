using System;
using UnityEngine;

namespace SFS.Variables;

[Serializable]
public abstract class Obs<T>
{
	[SerializeField]
	[HideInInspector]
	private T value;

	private bool hasFilter;

	private Func<T, T, T> filter;

	public T Value
	{
		get
		{
			return value;
		}
		set
		{
			if (hasFilter)
			{
				value = filter(this.value, value);
			}
			if (!IsEqual(this.value, value))
			{
				T arg = this.value;
				this.value = value;
				onChange?.Invoke();
				onChangeNew?.Invoke(value);
				onChangeOldNew?.Invoke(arg, value);
			}
		}
	}

	public Func<T, T, T> Filter
	{
		set
		{
			hasFilter = true;
			filter = value;
		}
	}

	public Obs<T> OnChange
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

	private event Action<T> onChangeNew;

	private event Action<T, T> onChangeOldNew;

	protected abstract bool IsEqual(T a, T b);

	public static Obs<T> operator +(Obs<T> a, Action b)
	{
		b();
		a.onChange += b;
		return a;
	}

	public static Obs<T> operator -(Obs<T> a, Action b)
	{
		a.onChange -= b;
		return a;
	}

	public static Obs<T> operator +(Obs<T> a, Action<T> b)
	{
		b(a.value);
		a.onChangeNew += b;
		return a;
	}

	public static Obs<T> operator -(Obs<T> a, Action<T> b)
	{
		a.onChangeNew -= b;
		return a;
	}

	public static Obs<T> operator +(Obs<T> a, Action<T, T> b)
	{
		b(a.value, a.value);
		a.onChangeOldNew += b;
		return a;
	}

	public static Obs<T> operator -(Obs<T> a, Action<T, T> b)
	{
		a.onChangeOldNew -= b;
		return a;
	}

	public static implicit operator T(Obs<T> a)
	{
		return a.Value;
	}

	[Obsolete("Use Value instead", true)]
	public static bool operator ==(Obs<T> a, Obs<T> b)
	{
		throw new Exception("Use variable.Value");
	}

	[Obsolete("Use Value instead", true)]
	public static bool operator !=(Obs<T> a, Obs<T> b)
	{
		throw new Exception("Use variable.Value");
	}
}
