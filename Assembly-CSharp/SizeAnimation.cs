using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SizeAnimation : MonoBehaviour
{
	public Vector2 startSize;

	public Vector2 endSize;

	public AnimationCurve curve;

	public RectTransform forceUpdate;

	private RectTransform rectTransform;

	private float t;

	private int direction;

	private void Awake()
	{
		rectTransform = this.GetRect();
	}

	private void OnEnable()
	{
		RunForwards();
	}

	private void OnDisable()
	{
		rectTransform.sizeDelta = startSize;
	}

	public void RunForwards()
	{
		base.enabled = true;
		t = 0f;
		direction = 1;
		Update();
	}

	public void RunBackwards()
	{
		base.enabled = true;
		t = curve.keys.Last().time;
		direction = -1;
		Update();
	}

	private void Update()
	{
		t += (float)direction * Time.unscaledDeltaTime;
		t = Mathf.Clamp(t, 0f, curve.keys.Last().time);
		rectTransform.sizeDelta = startSize + (endSize - startSize) * curve.Evaluate(t);
		if (forceUpdate != null)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(forceUpdate);
		}
		base.enabled = (direction != -1 || t != 0f) && (direction != 0 || t != curve.keys.Last().time);
	}
}
