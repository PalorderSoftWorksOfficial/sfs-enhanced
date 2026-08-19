using System;

namespace SFS.Variables;

[Serializable]
public class Local_GenericTarget : Obs_Destroyable<ObservableMonoBehaviour>
{
	protected override bool IsEqual(ObservableMonoBehaviour a, ObservableMonoBehaviour b)
	{
		return a == b;
	}
}
