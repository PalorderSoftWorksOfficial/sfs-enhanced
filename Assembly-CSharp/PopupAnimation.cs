using System.Linq;
using UnityEngine;

public class PopupAnimation : MonoBehaviour
{
	public Vector3 size;

	public AnimationCurve curve;

	public float popupDelay;

	private float t;

	private bool disable;

	private void Awake()
	{
		if (size == Vector3.zero)
		{
			size = base.transform.localScale;
		}
	}

	public void OnEnable()
	{
		t = 0f - popupDelay;
		disable = false;
		Update();
	}

	public void Disable()
	{
		t = curve.keys.Last().time;
		disable = true;
		Update();
	}

	public void SkipAnimation()
	{
		t = 10f;
		Update();
	}

	private void Update()
	{
		t += (disable ? (0f - Time.unscaledDeltaTime) : Time.unscaledDeltaTime);
		base.transform.localScale = size * curve.Evaluate(t);
		if (disable && t < 0f)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	private void OnDisable()
	{
		base.transform.localScale = size;
	}
}
