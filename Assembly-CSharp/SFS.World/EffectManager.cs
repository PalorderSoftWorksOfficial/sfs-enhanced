using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SFS.World;

public class EffectManager : MonoBehaviour
{
	private class Effect
	{
		private readonly Transform effect;

		public Coroutine lifecycleCoroutine;

		public Effect(Transform effect)
		{
			this.effect = effect;
		}

		public void Destroy()
		{
			if (effect != null)
			{
				Object.Destroy(effect.gameObject);
			}
			main.StopCoroutine(lifecycleCoroutine);
			main.effects.Remove(this);
		}
	}

	private static EffectManager main;

	public Transform explosionPrefab;

	public Transform explosionSoundPrefab;

	private List<Effect> effects = new List<Effect>();

	private void Awake()
	{
		main = this;
	}

	public static void CreateExplosion(Vector3 position, float size)
	{
		CreateEffect(main.explosionPrefab, position, 8f).localScale = Vector3.one * size;
		PlayerController.main.CreateShakeEffect(0.05f + size * 0.01f, 0.5f + size * 0.005f, 5000f, position);
	}

	public static void CreatePartOverheatEffect(Vector3 position, float size)
	{
		CreateEffect(main.explosionSoundPrefab, position, 8f);
		PlayerController.main.CreateShakeEffect(0.05f + size * 0.01f, 0.5f + size * 0.005f, 5000f, position);
	}

	public static Transform CreateEffect(Transform prefab, Vector3 position, float duration)
	{
		Transform obj = Object.Instantiate(prefab, position, Quaternion.identity).transform;
		EffectLifecycle(new Effect(obj), duration);
		return obj;
	}

	public static void ClearEffects()
	{
		Effect[] array = main.effects.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Destroy();
		}
	}

	private static void EffectLifecycle(Effect effect, float duration)
	{
		effect.lifecycleCoroutine = main.StartCoroutine(Coroutine());
		IEnumerator Coroutine()
		{
			main.effects.Add(effect);
			yield return new WaitForSeconds(duration);
			effect.Destroy();
		}
	}
}
