using System;
using UnityEngine;

namespace SFS.Parts.Modules;

[Serializable]
public class AdvancedCut
{
	[Serializable]
	public class Cut
	{
		[Range(0f, 1f)]
		public float left;

		[Range(0f, 1f)]
		public float right = 1f;
	}

	public Cut[] cuts;
}
