using System;
using UnityEngine;

namespace SFS.UI;

public class ClampSize : MonoBehaviour
{
	[Serializable]
	public class OptionalFloat
	{
		public bool enabled;

		public float value;

		public float? Value
		{
			get
			{
				if (!enabled)
				{
					return null;
				}
				return value;
			}
			set
			{
				enabled = value.HasValue;
				if (value.HasValue)
				{
					this.value = value.Value;
				}
			}
		}
	}

	public OptionalFloat minWidth;

	public OptionalFloat maxWidth;

	public OptionalFloat minHeight;

	public OptionalFloat maxHeight;

	public Vector2 ApplyClamp(Vector2 input)
	{
		if (minWidth.enabled)
		{
			input.x = Mathf.Max(minWidth.value, input.x);
		}
		if (maxWidth.enabled)
		{
			input.x = Mathf.Min(maxWidth.value, input.x);
		}
		if (minHeight.enabled)
		{
			input.y = Mathf.Max(minHeight.value, input.y);
		}
		if (maxHeight.enabled)
		{
			input.y = Mathf.Min(maxHeight.value, input.y);
		}
		return input;
	}
}
