using UnityEngine;
using UnityEngine.UI;

public class ColorAnimation : MonoBehaviour
{
	public Image image;

	public Gradient color;

	public float animationTime = 0.1f;

	private float t;

	private int direction;

	private void OnEnable()
	{
		RunForwards();
	}

	public void RunForwards()
	{
		direction = 1;
		t = 0f;
		base.enabled = true;
	}

	public void RunBackwards()
	{
		direction = -1;
		t = animationTime;
		base.enabled = true;
	}

	private void Update()
	{
		t += (float)direction * Time.unscaledDeltaTime;
		image.color = color.Evaluate(t / animationTime);
		if (t < 0f)
		{
			base.gameObject.SetActive(value: false);
		}
		if (t > animationTime)
		{
			base.enabled = false;
		}
	}
}
