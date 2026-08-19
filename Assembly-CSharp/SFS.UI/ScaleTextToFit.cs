using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

public class ScaleTextToFit : NewElement
{
	public ContentSizeFitter dependency;

	public Vector2 initialScale;

	public Vector2 scaledSelfPadding;

	[HideInInspector]
	public Vector2 latestScale;

	private bool hasForcedRebuild;

	private bool updating;

	private string lastText;

	private Vector2 lastSize;

	private TextAdapter text;

	private Vector2 RectSize
	{
		get
		{
			if (!hasForcedRebuild)
			{
				hasForcedRebuild = true;
				if (dependency != null)
				{
					LayoutRebuilder.ForceRebuildLayoutImmediate(dependency.GetRect());
				}
			}
			return GetRectSize();
		}
	}

	private void Reset()
	{
		initialScale = base.transform.localScale;
		dependency = GetComponent<ContentSizeFitter>();
	}

	private void Awake()
	{
		lastSize = GetRectSize();
		text = GetComponent<TextAdapter>();
		if (text == null)
		{
			text = base.transform.GetOrAddComponent<TextAdapter>();
		}
	}

	private void UpdateTextSize()
	{
		if ((lastSize != GetRectSize() || lastText != text.Text) && !updating)
		{
			lastSize = GetRectSize();
			lastText = text.Text;
			updating = true;
			UpdateHierarchy();
			updating = false;
		}
	}

	private void LateUpdate()
	{
		UpdateTextSize();
	}

	protected override Vector2 GetPreferredSize_Internal()
	{
		return RectSize * initialScale + scaledSelfPadding;
	}

	protected override void SetSize_Internal(Vector2 size)
	{
		Vector2 vector = size / (RectSize + scaledSelfPadding);
		if (!float.IsNaN(vector.x) && !float.IsNaN(vector.y) && !float.IsNegativeInfinity(vector.x) && !float.IsNegativeInfinity(vector.y))
		{
			float num = Mathf.Min(Mathf.Min(initialScale.x, vector.x) / initialScale.x, Mathf.Min(initialScale.y, vector.y) / initialScale.y);
			vector = initialScale * num;
			base.transform.localScale = vector;
			latestScale = vector;
		}
	}
}
