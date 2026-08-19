using System.Collections.Generic;
using UnityEngine;

namespace SFS.Audio;

[CreateAssetMenu]
public class MusicPlaylist : ScriptableObject
{
	public enum OnStart
	{
		First,
		Random
	}

	public enum OnEnd
	{
		Stop,
		Loop
	}

	public OnStart onStart;

	public List<MusicTrack> tracks = new List<MusicTrack>();

	public OnEnd onEnd;

	public int GetStartTrack()
	{
		if (onStart == OnStart.First)
		{
			return 0;
		}
		return GetRandomTrack();
	}

	public int GetRandomTrack()
	{
		return Random.Range(0, tracks.Count);
	}
}
