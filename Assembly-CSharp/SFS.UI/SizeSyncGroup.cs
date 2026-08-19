using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SFS.UI;

public class SizeSyncGroup : MonoBehaviour
{
	public List<RelativeSizeFitter> elements = new List<RelativeSizeFitter>();

	public Vector2 size;

	public void Update()
	{
		size.x = ((elements.Count > 0) ? elements.Max((RelativeSizeFitter e) => e.currentSize.x) : 0f);
		size.y = ((elements.Count > 0) ? elements.Max((RelativeSizeFitter e) => e.currentSize.y) : 0f);
	}
}
