using System.Collections.Generic;
using UnityEngine;

namespace SFS.Audio;

[CreateAssetMenu]
public class SoundEffect : ScriptableObject
{
	public List<Sound> sounds = new List<Sound>();

	public void Play(float volume = 1f)
	{
		Base.sound.PlaySound_2D(this, volume);
	}

	public PlayingSoundEffect3D Play3D(Transform holder, float volume, bool loop)
	{
		return Base.sound.PlaySound_3D(this, holder, volume, loop);
	}
}
