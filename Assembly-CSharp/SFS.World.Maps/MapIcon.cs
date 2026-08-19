using System;
using UnityEngine;

namespace SFS.World.Maps;

public class MapIcon : MonoBehaviour
{
	public GameObject mapIcon;

	public WorldLocation location;

	public float shake;

	private void Start()
	{
		location.planet.OnChange += new Action(OnPlanetChange);
		location.position.OnChange += new Action(OnPositionChange);
		Map.view.view.distance.OnChange += new Action(UpdateAlpha);
		Map.view.view.distance.OnChange += new Action(OnPositionChange);
	}

	private void OnDestroy()
	{
		if (mapIcon != null)
		{
			UnityEngine.Object.Destroy(mapIcon.gameObject);
		}
		Map.view.view.distance.OnChange -= new Action(UpdateAlpha);
		Map.view.view.distance.OnChange -= new Action(OnPositionChange);
	}

	private void OnPlanetChange()
	{
		if (!(location.planet.Value == null))
		{
			mapIcon.transform.SetParent(location.planet.Value.mapHolder.transform);
			OnPositionChange();
			UpdateAlpha();
		}
	}

	private void OnPositionChange()
	{
		mapIcon.transform.localPosition = location.position.Value / 1000.0 + ((shake > 0f) ? (UnityEngine.Random.insideUnitCircle * shake * 0.0008f * (float)(double)Map.view.view.distance / 1000f) : Vector2.zero);
	}

	private void UpdateAlpha()
	{
		SpriteRenderer componentInChildren = mapIcon.GetComponentInChildren<SpriteRenderer>();
		if (!(componentInChildren == null) && !(location.planet.Value == null))
		{
			double num = location.position.Value.magnitude * 50.0 + location.planet.Value.SOI * 5.0;
			float fadeOut = MapDrawer.GetFadeOut(Map.view.view.distance, num, num * 1.25);
			componentInChildren.color = new Color(1f, 1f, 1f, fadeOut);
		}
	}

	public void SetRotation(float newRotation)
	{
		mapIcon.transform.rotation = Quaternion.Euler(0f, 0f, newRotation - 90f);
	}
}
