using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

public class ReadTextContainer : NewElement
{
	public ContentSizeFitter dependency;

	private bool hasForcedRebuild;

	private bool updating;

	private string lastText;

	private Vector2 lastSize;

	private Text text;

	private TextAdapter textAdapter;

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
		dependency = GetComponent<ContentSizeFitter>();
	}

	private void Awake()
	{
		lastSize = GetRectSize();
		text = GetComponent<Text>();
		textAdapter = GetComponent<TextAdapter>();
	}

	private void LateUpdate()
	{
		if ((lastSize != GetRectSize() || (text != null && lastText != text.text) || (textAdapter != null && lastText != textAdapter.Text)) && !updating)
		{
			lastSize = GetRectSize();
			lastText = ((textAdapter != null) ? textAdapter.Text : text.text);
			updating = true;
			UpdateHierarchy();
			updating = false;
		}
	}

	protected override Vector2 GetPreferredSize_Internal()
	{
		return RectSize * base.transform.localScale;
	}

	protected override void SetSize_Internal(Vector2 availableSize)
	{
		availableSize /= (Vector2)base.transform.localScale;
		Vector2 applySize = LayoutUtility.GetApplySize(GetRectSize(), availableSize, apply_X: true, apply_Y: false);
		SetRectSize(applySize);
	}
}
