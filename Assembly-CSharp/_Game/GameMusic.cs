using System;
using SFS;
using SFS.Audio;
using SFS.World;
using UnityEngine;

namespace _Game;

public class GameMusic : MonoBehaviour
{
	public MusicPlaylistPlayer spaceMusic;

	private bool playingSpaceMusic;

	private void Start()
	{
		WorldView main = WorldView.main;
		main.onUpdate = (Action)Delegate.Combine(main.onUpdate, new Action(UpdateMusic));
	}

	private void UpdateMusic()
	{
		bool flag = !MuteMusic();
		if (flag && !playingSpaceMusic)
		{
			spaceMusic.StartPlaying(8f);
			playingSpaceMusic = true;
		}
		else if (!flag && playingSpaceMusic)
		{
			spaceMusic.StopPlaying(4f);
			playingSpaceMusic = false;
		}
	}

	private bool MuteMusic()
	{
		if (WorldView.main.ViewLocation.planet.codeName == Base.planetLoader.spaceCenter.address && WorldView.main.ViewLocation.planet.IsInsideAtmosphere(WorldView.main.ViewLocation.position))
		{
			return true;
		}
		Player value = PlayerController.main.player.Value;
		if (!(value is Rocket rocket))
		{
			if (value is Astronaut_EVA astronaut_EVA && astronaut_EVA.aero.airflowSound.volume.Value > 0.01f)
			{
				goto IL_00b1;
			}
		}
		else if (rocket.aero.airflowSound.volume.Value > 0.01f)
		{
			goto IL_00b1;
		}
		return false;
		IL_00b1:
		return true;
	}
}
