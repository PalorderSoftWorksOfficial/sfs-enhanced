using System;
using System.Collections.Generic;
using UnityEngine;

namespace SFS.Audio;

public class SoundPlayer : MonoBehaviour
{
	public static SoundPlayer main;

	public SoundEffect clickSound;

	public SoundEffect pickupSound;

	public SoundEffect dropSound;

	public SoundEffect denySound;

	private Dictionary<int, AudioSource> sources = new Dictionary<int, AudioSource>();

	private int lastID = -1;

	private List<PlayingSoundEffect2D> playing_2D = new List<PlayingSoundEffect2D>();

	private List<PlayingSoundEffect3D> playing_3D = new List<PlayingSoundEffect3D>();

	private void Awake()
	{
		main = this;
	}

	private void Start()
	{
		AudioSettings.main.soundVolume.OnChange += new Action(OnVolumeChange);
	}

	private void OnVolumeChange()
	{
		foreach (PlayingSoundEffect2D item in playing_2D)
		{
			item.ApplyVolume();
		}
		foreach (PlayingSoundEffect3D item2 in playing_3D)
		{
			item2.ApplyVolume();
		}
	}

	public PlayingSoundEffect3D PlaySound_3D(SoundEffect soundEffect, Transform holder, float volume, bool loop)
	{
		Tuple<Sound, AudioSource>[] array = new Tuple<Sound, AudioSource>[soundEffect.sounds.Count];
		for (int i = 0; i < soundEffect.sounds.Count; i++)
		{
			Sound sound = soundEffect.sounds[i];
			AudioSource audioSource = holder.gameObject.AddComponent<AudioSource>();
			audioSource.loop = loop;
			audioSource.clip = sound.clip;
			audioSource.pitch = sound.pitch;
			audioSource.dopplerLevel = 0f;
			audioSource.playOnAwake = false;
			audioSource.maxDistance = sound.range;
			audioSource.rolloffMode = AudioRolloffMode.Linear;
			audioSource.minDistance = 1f;
			audioSource.spatialBlend = 1f;
			audioSource.panStereo = 0f;
			array[i] = new Tuple<Sound, AudioSource>(sound, audioSource);
		}
		PlayingSoundEffect3D playingSoundEffect3D = new PlayingSoundEffect3D(array, volume);
		playingSoundEffect3D.Play();
		playingSoundEffect3D.ApplyVolume();
		playing_3D.Add(playingSoundEffect3D);
		return playingSoundEffect3D;
	}

	public void Clear3DSounds()
	{
		playing_3D.Clear();
	}

	public void PlaySound_2D(SoundEffect soundEffect, float volume)
	{
		Tuple<Sound, AudioSource>[] array = new Tuple<Sound, AudioSource>[soundEffect.sounds.Count];
		for (int i = 0; i < soundEffect.sounds.Count; i++)
		{
			Sound sound = soundEffect.sounds[i];
			AudioSource freeSource = GetFreeSource(out var _);
			freeSource.Stop();
			freeSource.clip = sound.clip;
			freeSource.pitch = sound.pitch;
			freeSource.dopplerLevel = 0f;
			freeSource.playOnAwake = false;
			if (sound.delay > 0f)
			{
				freeSource.PlayDelayed(sound.delay);
			}
			else
			{
				freeSource.Play();
			}
			array[i] = new Tuple<Sound, AudioSource>(sound, freeSource);
		}
		PlayingSoundEffect2D playingSoundEffect2D = new PlayingSoundEffect2D(array, volume);
		playingSoundEffect2D.ApplyVolume();
		playing_2D.Add(playingSoundEffect2D);
	}

	private AudioSource GetFreeSource(out int id)
	{
		AudioSource audioSource = null;
		id = -1;
		foreach (KeyValuePair<int, AudioSource> source in sources)
		{
			AudioSource value = source.Value;
			if (!value.isPlaying)
			{
				value.clip = null;
				if (audioSource == null)
				{
					audioSource = value;
					id = source.Key;
				}
			}
		}
		if (audioSource == null)
		{
			audioSource = base.gameObject.AddComponent<AudioSource>();
		}
		else
		{
			sources.Remove(id);
		}
		lastID++;
		id = lastID;
		sources.Add(id, audioSource);
		return audioSource;
	}
}
