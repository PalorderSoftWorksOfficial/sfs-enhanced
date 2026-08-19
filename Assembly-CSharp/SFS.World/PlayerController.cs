using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Input;
using SFS.Parts.Modules;
using SFS.Translations;
using SFS.UI;
using SFS.Variables;
using SFS.World.Drag;
using UnityEngine;

namespace SFS.World;

public class PlayerController : MonoBehaviour
{
	private class ShakeEffect
	{
		public float intensity;

		public float duration;

		public float spawnTime;

		public ShakeEffect(float intensity, float duration, float spawnTime)
		{
			this.intensity = intensity;
			this.duration = duration;
			this.spawnTime = spawnTime;
		}
	}

	public static PlayerController main;

	public Player_Local player;

	public Bool_Local hasControl;

	public Float_Local cameraDistance;

	public Vector2_Local cameraOffset;

	private List<ShakeEffect> shakeEffects = new List<ShakeEffect>();

	public Vector2 switchOffset;

	public float switchOffsetSmoothTime = 0.25f;

	private Vector2 switchOffsetVelocity;

	private void Awake()
	{
		main = this;
	}

	public void CreateShakeEffect(float intensity, float duration, float range, Vector2 position)
	{
		float magnitude = (position - WorldView.main.worldCamera.position.Value).magnitude;
		float intensity2 = intensity * Mathf.Pow(Mathf.InverseLerp(range, 0f, magnitude), 2f);
		shakeEffects.Add(new ShakeEffect(intensity2, duration, Time.time));
	}

	public void ClearShakeEffects()
	{
		shakeEffects.Clear();
	}

	public void SetOffset(Vector2 offset, float smoothTime)
	{
		switchOffset += offset;
		switchOffsetSmoothTime = smoothTime;
		TrackPlayer();
	}

	private void LateUpdate()
	{
		if (switchOffset.sqrMagnitude > 0f)
		{
			switchOffset = Vector2.SmoothDamp(switchOffset, Vector2.zero, ref switchOffsetVelocity, switchOffsetSmoothTime);
		}
		if (player.Value != null && Time.timeScale > 0f)
		{
			TrackPlayer();
		}
	}

	private void Start()
	{
		Screen_Game world_Input = GameManager.main.world_Input;
		world_Input.onZoom = (Action<ZoomData>)Delegate.Combine(world_Input.onZoom, new Action<ZoomData>(OnZoom));
		world_Input.onDrag = (Action<DragData>)Delegate.Combine(world_Input.onDrag, new Action<DragData>(OnDrag));
		world_Input.onInputEnd = (Action<OnInputEndData>)Delegate.Combine(world_Input.onInputEnd, new Action<OnInputEndData>(OnInputEnd));
		cameraOffset.Filter = ClampTrackingOffset;
		cameraDistance.Filter = ClampCameraDistance;
		player.OnChange += new Action<Player, Player>(OnPlayerChange);
		cameraDistance.OnChange += new Action(OnCameraDistanceChange);
		world_Input.keysNode.AddOnKeyDown(KeybindingsPC.keys.Switch_Rocket[0], delegate
		{
			CyclePlayable(-1);
		});
		world_Input.keysNode.AddOnKeyDown(KeybindingsPC.keys.Switch_Rocket[1], delegate
		{
			CyclePlayable(1);
		});
	}

	private void CyclePlayable(int offset)
	{
		List<Player> list = new List<Player>();
		list.AddRange(GameManager.main.rockets.Where((Rocket rocket) => rocket.physics.loader.Loaded));
		list.AddRange(AstronautManager.main.eva.Where((Astronaut_EVA astronaut) => astronaut.physics.loader.Loaded));
		list.AddRange(AstronautManager.main.flags.Where((Flag flag) => flag.loader.Loaded));
		if (list.Contains(player.Value))
		{
			list.Sort((Player a, Player b) => (int)Mathf.Sign(GetX(a) - GetX(b)));
			int num = list.IndexOf(player.Value) + offset;
			if (num < 0)
			{
				num = list.Count - 1;
			}
			if (num == list.Count)
			{
				num = 0;
			}
			SmoothChangePlayer(list[num]);
		}
		else if (player.Value == null && list.Count > 0)
		{
			float cameraX = GetX_Base(WorldView.main.ViewLocation.position);
			Player best = list.GetBest(delegate(Player a)
			{
				float num2 = (GetX(a) - cameraX) * (float)offset;
				return 0f - (Mathf.Abs(num2) + (float)((!(num2 > 0f)) ? 1000000 : 0));
			}, out var _);
			if (best != null)
			{
				SmoothChangePlayer(best);
			}
		}
		static float GetX(Player a)
		{
			return GetX_Base(a.location.position);
		}
		static float GetX_Base(Double2 pos)
		{
			return WorldView.main.worldCamera.camera.WorldToScreenPoint(WorldView.ToLocalPosition(pos)).x;
		}
	}

	private void OnZoom(ZoomData data)
	{
		cameraDistance.Value = (float)cameraDistance * data.zoomDelta;
	}

	private void OnDrag(DragData data)
	{
		if (player.Value != null)
		{
			cameraOffset.Value += data.DeltaWorld(0f);
		}
	}

	private void OnInputEnd(OnInputEndData data)
	{
		if (!WorldView.main.scaledSpace && (!data.LeftClick || !TrySelect(data.position, new Player[2][]
		{
			((IEnumerable<Astronaut_EVA>)AstronautManager.main.eva).Select((Func<Astronaut_EVA, Player>)((Astronaut_EVA a) => a)).ToArray(),
			((IEnumerable<Flag>)AstronautManager.main.flags).Select((Func<Flag, Player>)((Flag a) => a)).ToArray()
		}.Collapse())) && (!(player.Value != null) || !player.Value.OnInputEnd_AsPlayer(data)) && data.LeftClick && !TrySelect(data.position, ((IEnumerable<Rocket>)GameManager.main.rockets).Select((Func<Rocket, Player>)((Rocket a) => a)).ToList()))
		{
			Deselect();
		}
	}

	private static bool TrySelect(TouchPosition position, List<Player> players)
	{
		players.RemoveAll((Player p) => p.isPlayer.Value);
		if (players.Count == 0)
		{
			return false;
		}
		Player best = players.GetBest((Player player) => 0f - player.TryWorldSelect(position), out var bestScore);
		if (best != null && 0f - bestScore < (float)main.cameraDistance * 0.03f)
		{
			SelectableObject component = best.GetComponent<SelectableObject>();
			GameSelector.main.selected_World.Value = ((GameSelector.main.selected_World.Value == component) ? null : component);
			return true;
		}
		return false;
	}

	private static void Deselect()
	{
		GameSelector.main.selected_World.Value = null;
		StagingDrawer.main.SetSelected(null);
		if (!DevSettings.DisableAstronauts)
		{
			UnityEngine.Object.FindObjectOfType<AttachableStatsMenu>(includeInactive: true).Close();
		}
	}

	private void OnPlayerChange(Player oldValue, Player newValue)
	{
		if (oldValue != null)
		{
			oldValue.isPlayer.Value = false;
			oldValue.hasControl.OnChange -= new Action(UpdateHasControl);
			oldValue.location.position.OnChange -= new Action(TrackPlayer);
			cameraOffset.OnChange -= new Action(TrackPlayer);
		}
		cameraOffset.Value = Vector2.zero;
		UpdateHasControl();
		if (newValue != null)
		{
			newValue.isPlayer.Value = true;
			newValue.hasControl.OnChange += new Action(UpdateHasControl);
			newValue.location.position.OnChange += new Action(TrackPlayer);
			cameraOffset.OnChange += new Action(TrackPlayer);
			WorldTime.main.CheckStopTimewarp_ForChangedPlayer();
		}
	}

	private void OnCameraDistanceChange()
	{
		WorldView.main.viewDistance.Value = cameraDistance;
		cameraOffset.Value = cameraOffset;
	}

	private void UpdateHasControl()
	{
		hasControl.Value = player.Value != null && (bool)player.Value.hasControl;
	}

	private Vector2 ClampTrackingOffset(Vector2 oldValue, Vector2 newValue)
	{
		if (player.Value != null)
		{
			player.Value.ClampTrackingOffset(ref newValue, cameraDistance);
		}
		return newValue;
	}

	private static float ClampCameraDistance(float oldValue, float newValue)
	{
		return Mathf.Clamp(newValue, 12f, 2.5E+10f);
	}

	public bool HasControl(I_MsgLogger logger)
	{
		if (hasControl.Value)
		{
			return true;
		}
		logger.Log(Loc.main.No_Control_Msg);
		return false;
	}

	public void TrackPlayer()
	{
		Location value = player.Value.location.Value;
		value.position += cameraOffset + switchOffset;
		if (VideoSettingsPC.main.settings.cameraShake)
		{
			float num = 0f;
			if (shakeEffects.Any((ShakeEffect a) => a.spawnTime + a.duration < Time.time))
			{
				shakeEffects = shakeEffects.Where((ShakeEffect a) => a.spawnTime + a.duration > Time.time).ToList();
			}
			foreach (ShakeEffect shakeEffect in shakeEffects)
			{
				float num2 = Mathf.InverseLerp(0f, shakeEffect.duration, Time.time - shakeEffect.spawnTime);
				num += (1f / (1f + num2 * 9f) - 0.1f * num2) * shakeEffect.intensity;
			}
			if (player.Value is Rocket rocket)
			{
				num += rocket.partHolder.GetModules<EngineModule>().Sum((EngineModule engine) => engine.thrust.Value * engine.throttle_Out.Value) * 0.00015f;
				num += rocket.partHolder.GetModules<BoosterModule>().Sum((BoosterModule booster) => (!booster.enabled) ? 0f : booster.thrustVector.Value.magnitude) * 0.00015f;
				num += (rocket.partHolder.GetModules<RcsModule>().Any((RcsModule rcs) => rcs.thrusters.Any((RcsModule.Thruster t) => t.effect.targetTime.Value > 0f)) ? 0.003f : 0f);
			}
			if (player.Value.location.planet.Value.IsInsideAtmosphere(player.Value.location.position.Value))
			{
				AeroModule.GetTemperatureAndShockwave(player.Value.location.Value, out var Q, out var shockOpacity, out var temperature);
				num += Q * 0.0003f + shockOpacity * 0.02f + AeroModule.GetIntensity(temperature, 3000f) * 0.2f;
			}
			if (num > 0f)
			{
				value.position += UnityEngine.Random.insideUnitCircle * num;
			}
		}
		WorldView.main.SetViewLocation(value);
	}

	public void SmoothChangePlayer(Player newPlayer, float smoothTime = 0.25f)
	{
		SetOffset(WorldView.main.ViewLocation.position - newPlayer.location.position.Value, smoothTime);
		player.Value = newPlayer;
	}
}
