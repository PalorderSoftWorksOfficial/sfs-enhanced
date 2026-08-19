using UnityEngine;

namespace SFS.UI;

public interface I_Raycastable
{
	bool SkipRaycast { get; }

	RectTransform GetRect();
}
