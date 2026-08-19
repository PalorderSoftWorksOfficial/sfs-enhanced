using System;
using SFS;
using SFS.Cameras;
using UnityEngine;

public class Stars : MonoBehaviour
{
	private class Star
	{
		public float size;

		public Transform star;

		public Star(Transform star)
		{
			size = star.localScale.x;
			this.star = star;
		}
	}

	private Star[] stars = new Star[0];

	private void PositionStars(float radius)
	{
		Transform[] componentsInChildren = base.transform.GetComponentsInChildren<Transform>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].localPosition = UnityEngine.Random.insideUnitCircle * radius;
		}
		base.transform.position = Vector3.zero;
	}

	private void Start()
	{
		ActiveCamera.main.activeCamera.OnChange += (Action)delegate
		{
			if (!Base.sceneLoader.isUnloading)
			{
				base.transform.SetParent(ActiveCamera.Camera.camera.transform, worldPositionStays: false);
			}
		};
		Transform[] componentsInChildren = base.transform.GetComponentsInChildren<Transform>();
		stars = new Star[componentsInChildren.Length - 1];
		for (int num = 0; num < stars.Length; num++)
		{
			stars[num] = new Star(componentsInChildren[num + 1]);
		}
	}

	private void OnEnable()
	{
		Update();
	}

	private void Update()
	{
		for (int i = 0; i < stars.Length; i++)
		{
			Star star = stars[i];
			float num = Mathf.LerpUnclamped(0.6f, 1.8f, Mathf.PerlinNoise(Time.unscaledTime * 0.3f, i));
			star.star.localScale = Vector2.one * (num * star.size);
		}
	}

	private void LateUpdate()
	{
		base.transform.eulerAngles = Vector3.zero;
	}
}
