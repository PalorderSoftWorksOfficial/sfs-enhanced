using System;
using System.Collections.Generic;
using SFS.Variables;

namespace SFS.Input;

[Serializable]
public class KeysNode
{
	private Dictionary<I_Key, List<Action>> onKeyDownActions = new Dictionary<I_Key, List<Action>>();

	private Dictionary<I_Key, List<Action>> onKeyActions = new Dictionary<I_Key, List<Action>>();

	private Dictionary<I_Key, List<Action>> onKeyUpActions = new Dictionary<I_Key, List<Action>>();

	private Dictionary<Float_Local, Axis> axis = new Dictionary<Float_Local, Axis>();

	public void AddOnKeyDown(I_Key key, Action action)
	{
		if (!onKeyDownActions.ContainsKey(key))
		{
			onKeyDownActions.Add(key, new List<Action>());
		}
		onKeyDownActions[key].Add(action);
	}

	public void AddOnKey(I_Key key, Action action)
	{
		if (!onKeyActions.ContainsKey(key))
		{
			onKeyActions.Add(key, new List<Action>());
		}
		onKeyActions[key].Add(action);
	}

	public void AddOnKeyUp(I_Key key, Action action)
	{
		if (!onKeyUpActions.ContainsKey(key))
		{
			onKeyUpActions.Add(key, new List<Action>());
		}
		onKeyUpActions[key].Add(action);
	}

	public Float_Local AddAxis((I_Key, I_Key) axisKeys, ref Float_Local output)
	{
		if (!this.axis.ContainsKey(output))
		{
			this.axis.Add(output, new Axis());
		}
		Axis axis = this.axis[output];
		var (negativeKey, positiveKey) = axisKeys;
		axis.AddPositiveKey(positiveKey);
		axis.AddNegativeKey(negativeKey);
		if ((object)output != null)
		{
			axis.output = output;
		}
		return axis.output;
	}

	public void ProcessInput(Dictionary<I_Key, Action> additionalKeys)
	{
		if (additionalKeys != null)
		{
			foreach (KeyValuePair<I_Key, Action> additionalKey in additionalKeys)
			{
				if (additionalKey.Key.IsKeyDown())
				{
					additionalKey.Value?.Invoke();
				}
			}
		}
		foreach (KeyValuePair<I_Key, List<Action>> onKeyDownAction in onKeyDownActions)
		{
			if (!onKeyDownAction.Key.IsKeyDown())
			{
				continue;
			}
			foreach (Action item in onKeyDownAction.Value)
			{
				item();
			}
		}
		foreach (KeyValuePair<I_Key, List<Action>> onKeyAction in onKeyActions)
		{
			if (!onKeyAction.Key.IsKeyStay())
			{
				continue;
			}
			foreach (Action item2 in onKeyAction.Value)
			{
				item2();
			}
		}
		foreach (KeyValuePair<I_Key, List<Action>> onKeyUpAction in onKeyUpActions)
		{
			if (!onKeyUpAction.Key.IsKeyUp())
			{
				continue;
			}
			foreach (Action item3 in onKeyUpAction.Value)
			{
				item3();
			}
		}
		foreach (Axis value in axis.Values)
		{
			value.UpdateValue();
		}
	}
}
