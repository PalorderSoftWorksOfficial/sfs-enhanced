using System;
using UnityEngine;

namespace SFS.Audio;

[Serializable]
public class Sound
{
	public AudioClip clip;

	[Range(0f, 1f)]
	public float volume = 1f;

	[Range(0f, 3f)]
	public float pitch = 1f;

	[Range(0f, 10f)]
	public float delay;

	[Tooltip("Range is only used if the sound effect is played in 3D")]
	[Range(0f, 5000f)]
	public int range = 5000;
}
