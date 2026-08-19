using System;
using UnityEngine;

namespace SFS.Audio;

public class MusicPlaylistPlayer : MonoBehaviour
{
	public MusicPlaylist playlist;

	public AudioSource source;

	[Space]
	private int currentTrack = -1;

	private float currentFadeVolume;

	private float targetFadeVolume;

	private float fadeTime;

	public void StartPlaying(float fadeTime)
	{
		if (currentTrack == -1)
		{
			PlayTrack(playlist.GetStartTrack());
		}
		this.fadeTime = fadeTime;
		targetFadeVolume = 1f;
	}

	public void StopPlaying(float fadeTime)
	{
		if (fadeTime > 0.01f)
		{
			this.fadeTime = fadeTime;
			targetFadeVolume = 0f;
		}
		else
		{
			StopTrack();
		}
	}

	private void Start()
	{
		AudioSettings.main.musicVolume.OnChange += new Action(UpdateVolume);
	}

	private void OnDestroy()
	{
		AudioSettings.main.musicVolume.OnChange -= new Action(UpdateVolume);
	}

	private void UpdateVolume()
	{
		if (currentTrack != -1)
		{
			source.volume = playlist.tracks[currentTrack].volume * AudioSettings.main.musicVolume.Value * Mathf.Pow(currentFadeVolume, 2f);
		}
	}

	private void Update()
	{
		if (currentTrack == -1)
		{
			return;
		}
		if (!source.isPlaying && Application.isFocused)
		{
			if (playlist.tracks[currentTrack].onTrackEnd == MusicTrack.OnTrackEnd.PlayNext)
			{
				int lastTrack = currentTrack;
				StopTrack();
				PlayNext(lastTrack);
			}
			else if (playlist.tracks[currentTrack].onTrackEnd == MusicTrack.OnTrackEnd.PlayRandom)
			{
				StopTrack();
				PlayRandom();
			}
		}
		if (currentFadeVolume != targetFadeVolume)
		{
			currentFadeVolume = Mathf.MoveTowards(currentFadeVolume, targetFadeVolume, (fadeTime > 0.01f) ? (Time.unscaledDeltaTime / fadeTime) : float.PositiveInfinity);
			UpdateVolume();
			if (currentFadeVolume == 0f && targetFadeVolume == 0f)
			{
				StopTrack();
			}
		}
	}

	private void PlayNext(int lastTrack)
	{
		int num = lastTrack + 1;
		if (num == playlist.tracks.Count)
		{
			if (playlist.onEnd == MusicPlaylist.OnEnd.Loop)
			{
				StartPlaying(0f);
			}
		}
		else
		{
			PlayTrack(num);
		}
	}

	private void PlayRandom()
	{
		int num = currentTrack;
		int randomTrack;
		for (randomTrack = currentTrack; randomTrack == num; randomTrack = playlist.GetRandomTrack())
		{
		}
		PlayTrack(randomTrack);
	}

	private AudioClip LoadByResourceName(string name)
	{
		AudioClip result = Resources.Load<AudioClip>(name);
		Resources.UnloadUnusedAssets();
		return result;
	}

	private void PlayTrack(int newTrack)
	{
		currentTrack = newTrack;
		UpdateVolume();
		source.clip = LoadByResourceName(playlist.tracks[currentTrack].clipName);
		source.pitch = playlist.tracks[currentTrack].pitch;
		source.Play();
		Resources.UnloadUnusedAssets();
	}

	private void StopTrack()
	{
		currentTrack = -1;
		source.Stop();
	}
}
