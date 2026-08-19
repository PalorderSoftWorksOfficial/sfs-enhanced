using System;

namespace SFS.Variables;

[Serializable]
public abstract class Obs_Destroyable<T> : Obs<T> where T : I_ObservableMonoBehaviour
{
	public new T Value
	{
		get
		{
			return base.Value;
		}
		set
		{
			T val = base.Value;
			base.Value = value;
			if (val != null)
			{
				Action onDestroy = (Action)Delegate.Remove(val.OnDestroy, new Action(OnDestroy));
				val.OnDestroy = onDestroy;
			}
			if (value != null)
			{
				Action onDestroy2 = (Action)Delegate.Combine(value.OnDestroy, new Action(OnDestroy));
				value.OnDestroy = onDestroy2;
			}
		}
	}

	private void OnDestroy()
	{
		Value = default(T);
	}
}
