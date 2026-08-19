using System.Collections.Generic;
using UnityEngine;

namespace SFS.UI;

public class Raycastable : MonoBehaviour, I_Raycastable
{
	public static List<Raycastable> raycastables = new List<Raycastable>();

	public bool skipRaycast;

	public bool SkipRaycast => skipRaycast;

	private void OnEnable()
	{
		raycastables.Add(this);
	}

	private void OnDisable()
	{
		raycastables.Remove(this);
	}

	RectTransform I_Raycastable.GetRect()
	{
		return GetComponent<RectTransform>();
	}
}
