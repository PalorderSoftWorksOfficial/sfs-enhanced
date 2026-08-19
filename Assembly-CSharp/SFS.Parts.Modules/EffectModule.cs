using SFS.World;
using UnityEngine;

namespace SFS.Parts.Modules;

public class EffectModule : MonoBehaviour
{
	public GameObject effectPrefab;

	public bool attach;

	public float lifetime = 10f;

	public void Spawn(Transform holder)
	{
		Transform transform = EffectManager.CreateEffect(effectPrefab.transform, holder.position, lifetime);
		transform.rotation = holder.rotation;
		if (attach)
		{
			transform.transform.SetParent(holder);
		}
	}
}
