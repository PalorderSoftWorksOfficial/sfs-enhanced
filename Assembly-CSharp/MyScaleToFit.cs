using UnityEngine;

public class MyScaleToFit : MonoBehaviour
{
	public float maxScale = float.PositiveInfinity;

	public float horizontalPadding;

	public bool horizontal = true;

	public bool vertical = true;

	private void Start()
	{
		LateUpdate();
	}

	private void LateUpdate()
	{
		if (horizontal || vertical)
		{
			Vector2 vector = ((RectTransform)base.transform.parent).rect.size - new Vector2(horizontalPadding * 2f, 0f);
			Vector2 size = ((RectTransform)base.transform).rect.size;
			float num = Mathf.Min(horizontal ? (vector.x / size.x) : 1000f, vertical ? (vector.y / size.y) : 1000f);
			if (num > maxScale)
			{
				num = maxScale;
			}
			base.transform.localScale = new Vector3(num, num, 1f);
		}
	}
}
