using System;
using System.Collections.Generic;
using UnityEngine;

namespace SFS.Input;

public abstract class Screen_Base : MonoBehaviour
{
	protected static Dictionary<I_Key, Action> keys = new Dictionary<I_Key, Action>();

	public abstract bool PauseWhileOpen { get; }

	public static void AddOnKeyDown(I_Key key, Action action)
	{
		if (keys.ContainsKey(key))
		{
			Dictionary<I_Key, Action> dictionary = keys;
			dictionary[key] = (Action)Delegate.Combine(dictionary[key], action);
		}
		else
		{
			keys.Add(key, action);
		}
	}

	public abstract void ProcessInput();

	public abstract void OnOpen();

	public abstract void OnClose();
}
