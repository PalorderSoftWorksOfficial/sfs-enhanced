using SFS.WorldBase;
using UnityEngine;

namespace SFS.Career;

public class AllowPerMode : MonoBehaviour
{
	public bool careerOnly;

	public bool sandboxOnly;

	public bool Allowed
	{
		get
		{
			if (careerOnly && !Base.worldBase.IsCareer)
			{
				return false;
			}
			if (sandboxOnly && Base.worldBase.settings.mode.mode != WorldMode.Mode.Sandbox)
			{
				return false;
			}
			return true;
		}
	}
}
