using UnityEngine;

namespace SFS;

public class ShootingStarManager : MonoBehaviour
{
	public ShootingStar shootingStarPrefab;

	public GameObject starHolder;

	public float left;

	public float right;

	public float top;

	public float bottom;

	public float minTime;

	public float maxTime;

	private float timer;

	private void Update()
	{
		if (timer <= 0f)
		{
			SpawnShootingStar();
			timer = Random.Range(minTime, maxTime);
		}
		else
		{
			timer -= Time.unscaledDeltaTime;
		}
	}

	private void SpawnShootingStar()
	{
		ShootingStar shootingStar = Object.Instantiate(shootingStarPrefab, base.transform);
		Vector3 origin;
		Vector3 destination;
		if (Random.Range(0, 2) > 0)
		{
			origin = new Vector3(left, Mathf.Lerp(top, bottom, Random.Range(0f, 1f)), 0f);
			destination = new Vector3(right, Mathf.Lerp(top, bottom, Random.Range(0f, 1f)), 0f);
		}
		else
		{
			origin = new Vector3(right, Mathf.Lerp(top, bottom, Random.Range(0f, 1f)), 0f);
			destination = new Vector3(left, Mathf.Lerp(top, bottom, Random.Range(0f, 1f)), 0f);
		}
		shootingStar.UpdatePath(origin, destination);
	}
}
