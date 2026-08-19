using System;
using SFS.Variables;
using UnityEngine;

public class ObservableMonoBehaviour : MonoBehaviour, I_ObservableMonoBehaviour
{
	private Action onDestroyDelegate;

	Action I_ObservableMonoBehaviour.OnDestroy
	{
		get
		{
			return onDestroyDelegate;
		}
		set
		{
			onDestroyDelegate = value;
		}
	}

	protected void OnDestroy()
	{
		onDestroyDelegate?.Invoke();
	}
}
