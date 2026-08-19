using System;
using UnityEngine;

namespace SFS.Variables;

[Serializable]
public abstract class ReferenceVariable<T>
{
	[SerializeField]
	private string variableName = "";

	[HideInInspector]
	public VariablesModule referenceToVariables;

	[HideInInspector]
	public T localValue;

	private bool initialized;

	private VariableList<T>.Variable variable;

	public VariableList<T>.Variable Variable
	{
		get
		{
			if (!initialized)
			{
				variable = GetVariable(variableName);
				initialized = true;
			}
			return variable;
		}
	}

	public bool Local
	{
		get
		{
			if (variableName.Length != 0 && !(referenceToVariables == null))
			{
				return Variable == null;
			}
			return true;
		}
	}

	public virtual T Value
	{
		get
		{
			if (!Local)
			{
				return Variable.Value;
			}
			return localValue;
		}
		set
		{
			if (Local)
			{
				if (!IsEqual(localValue, value))
				{
					T arg = localValue;
					localValue = value;
					onLocalValueChange?.Invoke();
					onLocalValueChangeNew?.Invoke(value);
					onLocalValueChangeOldNew?.Invoke(arg, value);
				}
			}
			else
			{
				Variable.Value = value;
			}
		}
	}

	public ReferenceVariable<T> OnChange
	{
		get
		{
			return this;
		}
		set
		{
		}
	}

	private event Action onLocalValueChange;

	private event Action<T> onLocalValueChangeNew;

	private event Action<T, T> onLocalValueChangeOldNew;

	public abstract VariableList<T>.Variable GetVariable(string variableName);

	public abstract VariableList<T> GetVariableList();

	protected abstract bool IsEqual(T a, T b);

	public static ReferenceVariable<T> operator +(ReferenceVariable<T> a, Action b)
	{
		b();
		if (a.Local)
		{
			a.onLocalValueChange += b;
		}
		else
		{
			VariableList<T>.Variable obj = a.Variable;
			obj.onValueChange = (Action)Delegate.Combine(obj.onValueChange, b);
		}
		return a;
	}

	public static ReferenceVariable<T> operator +(ReferenceVariable<T> a, Action<T> b)
	{
		b(a.Value);
		if (a.Local)
		{
			a.onLocalValueChangeNew += b;
		}
		else
		{
			VariableList<T>.Variable obj = a.Variable;
			obj.onValueChangeNew = (Action<T>)Delegate.Combine(obj.onValueChangeNew, b);
		}
		return a;
	}

	public static ReferenceVariable<T> operator +(ReferenceVariable<T> a, Action<T, T> b)
	{
		b(a.Value, a.Value);
		if (a.Local)
		{
			a.onLocalValueChangeOldNew += b;
		}
		else
		{
			VariableList<T>.Variable obj = a.Variable;
			obj.onValueChangeOldNew = (Action<T, T>)Delegate.Combine(obj.onValueChangeOldNew, b);
		}
		return a;
	}

	public static ReferenceVariable<T> operator -(ReferenceVariable<T> a, Action b)
	{
		if (a.Local)
		{
			a.onLocalValueChange -= b;
		}
		else
		{
			VariableList<T>.Variable obj = a.Variable;
			obj.onValueChange = (Action)Delegate.Remove(obj.onValueChange, b);
		}
		return a;
	}

	public static ReferenceVariable<T> operator -(ReferenceVariable<T> a, Action<T> b)
	{
		if (a.Local)
		{
			a.onLocalValueChangeNew -= b;
		}
		else
		{
			VariableList<T>.Variable obj = a.Variable;
			obj.onValueChangeNew = (Action<T>)Delegate.Remove(obj.onValueChangeNew, b);
		}
		return a;
	}

	public static ReferenceVariable<T> operator -(ReferenceVariable<T> a, Action<T, T> b)
	{
		if (a.Local)
		{
			a.onLocalValueChangeOldNew -= b;
		}
		else
		{
			VariableList<T>.Variable obj = a.Variable;
			obj.onValueChangeOldNew = (Action<T, T>)Delegate.Remove(obj.onValueChangeOldNew, b);
		}
		return a;
	}
}
