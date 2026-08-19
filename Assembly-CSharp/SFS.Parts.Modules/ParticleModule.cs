using SFS.World;
using UnityEngine;

namespace SFS.Parts.Modules;

public class ParticleModule : MonoBehaviour, Rocket.INJ_Rocket
{
	public WorldParticle particle;

	[Space]
	public float velocityRange;

	public int particleCount;

	public bool atStart;

	private WorldParticle[] Options => ResourcesLoader.GetFiles_Array<WorldParticle>("");

	public Rocket Rocket { get; set; }

	private void Start()
	{
		if (atStart)
		{
			Spawn();
		}
	}

	public void Spawn()
	{
		if (DevSettings.HasNewParticles)
		{
			Vector3 position = base.transform.position;
			Vector3 vector = ((Rocket != null) ? Rocket.rb2d.velocity : Vector2.zero);
			(Vector3, Vector3)[] array = new(Vector3, Vector3)[particleCount];
			for (int i = 0; i < array.Length; i++)
			{
				Vector2 vector2 = Random.insideUnitCircle * Random.Range(0.5f, 1f);
				array[i] = (position + base.transform.TransformVector(vector2 * new Vector2(1.3f, 0.1f)), vector + base.transform.TransformVector(vector2 * new Vector2(1f, 0.7f) * velocityRange));
			}
			particle.Spawn(array);
		}
	}
}
