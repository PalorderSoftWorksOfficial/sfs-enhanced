using UnityEngine;

namespace SFS;

public class ShootingStar : MonoBehaviour
{
	public float speed = 1f;

	private Vector3 destination = Vector3.zero;

	private void Update()
	{
		if (base.transform.position == destination)
		{
			Object.Destroy(base.gameObject);
		}
		else
		{
			base.transform.position = Vector3.MoveTowards(base.transform.position, destination, speed * Time.unscaledDeltaTime);
		}
	}

	public void UpdatePath(Vector3 origin, Vector3 destination)
	{
		base.transform.rotation = Quaternion.Euler(0f, 0f, 57.29578f * Mathf.Atan2(destination.y - origin.y, destination.x - origin.x));
		base.transform.position = origin;
		this.destination = destination;
	}
}
