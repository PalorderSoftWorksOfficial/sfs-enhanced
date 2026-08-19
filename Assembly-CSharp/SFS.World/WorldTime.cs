using System;
using System.Globalization;
using System.Linq;
using SFS.Input;
using SFS.Translations;
using SFS.UI;
using SFS.Variables;
using SFS.World.Maps;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World;

public class WorldTime : MonoBehaviour
{
	public static WorldTime main;

	public int timewarpIndex;

	public double worldTime;

	public double timewarpSpeed;

	public Bool_Local realtimePhysics;

	public static double MaxTimewarpSpeed => GetTimewarpSpeed_Rails(MaxIndex - MaxPhysicsIndex);

	private static int MaxIndex => MaxPhysicsIndex + (int)Math.Log(Base.planetLoader.planets.Values.Max((Planet a) => a.orbit?.period ?? 0.0) / 24.0, 4.72);

	private static int MaxPhysicsIndex => Base.worldBase.settings.difficulty.MaxPhysicsTimewarpIndex;

	public float TimeScale
	{
		get
		{
			if (!realtimePhysics.Value)
			{
				return 1f;
			}
			return (float)timewarpSpeed;
		}
	}

	public static float FixedDeltaTime
	{
		get
		{
			if (!main.realtimePhysics.Value)
			{
				return (float)main.timewarpSpeed * Time.fixedDeltaTime;
			}
			return Time.fixedDeltaTime;
		}
	}

	private void Awake()
	{
		main = this;
	}

	public static bool CanTimewarp(bool showMsg, bool showSpeed)
	{
		if (main.realtimePhysics.Value)
		{
			if (PlayerController.main.player.Value != null)
			{
				Player value = PlayerController.main.player.Value;
				I_MsgLogger logger;
				if (!showMsg)
				{
					I_MsgLogger i_MsgLogger = new MsgNone();
					logger = i_MsgLogger;
				}
				else
				{
					I_MsgLogger i_MsgLogger = MsgDrawer.main;
					logger = i_MsgLogger;
				}
				return value.CanTimewarp(logger, showSpeed);
			}
			return false;
		}
		return true;
	}

	public static void ShowCannotTimewarpBelowHeightMsg(double height, I_MsgLogger logger, bool showSpeed)
	{
		Field.Builder builder = (showSpeed ? Loc.main.Cannot_Timewarp_Below : Loc.main.Cannot_Timewarp_Below_Basic).Inject(height.ToDistanceString(decimals: false), "height");
		if (showSpeed)
		{
			builder.Inject(GetTimewarpSpeed_Physics(MaxPhysicsIndex).ToString(CultureInfo.InvariantCulture), "speed");
		}
		logger.Log(builder);
	}

	public static void ShowCannotTimewarpMsg(Field msg, I_MsgLogger logger)
	{
		logger.Log(msg.Inject(GetTimewarpSpeed_Physics(MaxPhysicsIndex).ToString(CultureInfo.InvariantCulture), "speed"));
	}

	private void Start()
	{
		GameManager.AddOnKeyDown(KeybindingsPC.keys.Timewarp[1], AccelerateTime);
		GameManager.AddOnKeyDown(KeybindingsPC.keys.Timewarp[0], DecelerateTime);
	}

	public void SetTimewarpIndex_ForLoad(int timewarpIndex)
	{
		this.timewarpIndex = timewarpIndex;
		ApplyState(showMsg: false);
	}

	public void AccelerateTime()
	{
		if (Map.manager.timewarpTo.warp != null)
		{
			ExitAutomaticTimewarp();
		}
		else if (timewarpIndex != MaxIndex)
		{
			if (CanTimewarp(showMsg: false, showSpeed: false))
			{
				Accelerate_Rails();
			}
			else if (timewarpIndex < MaxPhysicsIndex)
			{
				Accelerate_Physics();
			}
			else
			{
				CanTimewarp(showMsg: true, showSpeed: true);
			}
		}
	}

	private void Accelerate_Rails()
	{
		if (timewarpIndex < MaxPhysicsIndex)
		{
			timewarpIndex = MaxPhysicsIndex + 1;
		}
		else if (MaxPhysicsIndex == 2 && timewarpIndex == MaxPhysicsIndex)
		{
			timewarpIndex = MaxPhysicsIndex + 2;
		}
		else
		{
			timewarpIndex++;
		}
		ApplyState(showMsg: true);
	}

	private void Accelerate_Physics()
	{
		timewarpIndex++;
		ApplyState(showMsg: true);
	}

	public void DecelerateTime()
	{
		if (Map.manager.timewarpTo.warp != null)
		{
			ExitAutomaticTimewarp();
		}
		else if (timewarpIndex != 0)
		{
			if (timewarpIndex == MaxPhysicsIndex + 1)
			{
				timewarpIndex = 0;
			}
			else
			{
				timewarpIndex--;
			}
			ApplyState(showMsg: true);
		}
	}

	private void ApplyState(bool showMsg)
	{
		bool flag = timewarpIndex <= MaxPhysicsIndex;
		double num = (flag ? GetTimewarpSpeed_Physics(timewarpIndex) : GetTimewarpSpeed_Rails(timewarpIndex - MaxPhysicsIndex));
		main.SetState(num, flag, showMsg);
	}

	private static double GetTimewarpSpeed_Physics(int timewarpIndex_Physics)
	{
		return (new int[4] { 1, 2, 3, 5 })[timewarpIndex_Physics];
	}

	private static double GetTimewarpSpeed_Rails(int timewarpIndex_Rails)
	{
		return (double)(new int[3] { 1, 5, 25 })[timewarpIndex_Rails % 3] * Math.Pow(100.0, (int)((float)timewarpIndex_Rails / 3f));
	}

	private void ExitAutomaticTimewarp()
	{
		StopTimewarp(showMsg: false);
	}

	public void CheckStopTimewarp_ForChangedPlayer()
	{
		if (!realtimePhysics && !CanTimewarp(showMsg: true, showSpeed: false))
		{
			StopTimewarp(showMsg: false);
		}
	}

	public void StopTimewarp(bool showMsg)
	{
		Map.manager.timewarpTo.OnStopTimewarp();
		timewarpIndex = 0;
		SetState(1.0, realtimePhysics: true, showMsg);
	}

	private void Update()
	{
		double timeOld = worldTime;
		CheckStopTimewarp_ForUpdate(timeOld, worldTime += (main.realtimePhysics.Value ? Time.deltaTime : ((float)main.timewarpSpeed * Time.deltaTime)));
		Time.fixedDeltaTime = ((Time.timeScale == 0f) ? 0.1f : Mathf.Clamp(Time.deltaTime, 1f / 60f, 1f / 15f));
	}

	private void CheckStopTimewarp_ForUpdate(double timeOld, double timeNew)
	{
		if (realtimePhysics.Value)
		{
			return;
		}
		Player value = PlayerController.main.player.Value;
		if (value == null)
		{
			return;
		}
		double stopTimewarpTime = value.mapPlayer.Trajectory.GetStopTimewarpTime(timeOld, timeNew);
		if (!double.IsPositiveInfinity(stopTimewarpTime))
		{
			Planet value2 = value.location.planet.Value;
			ShowCannotTimewarpBelowHeightMsg(value2.TimewarpRadius_Descend - value2.Radius, MsgDrawer.main, showSpeed: false);
			worldTime = stopTimewarpTime;
			StopTimewarp(showMsg: false);
		}
		TimewarpTo.I_Warp warp = Map.manager.timewarpTo.warp;
		bool flag;
		Location location;
		if (!(warp is TimewarpTo.Warp_Point warp_Point))
		{
			if (!(warp is TimewarpTo.Warp_TransferWindow warp_TransferWindow))
			{
				return;
			}
			bool flag2;
			bool flag3;
			(flag, location, flag2, flag3) = Map.navigation.window;
			bool num;
			if (!warp_TransferWindow.planetWindow)
			{
				num = flag2;
			}
			else
			{
				if (!flag || flag2)
				{
					goto IL_0124;
				}
				num = !flag3;
			}
			if (num)
			{
				worldTime = timeOld + (double)Time.deltaTime;
				StopTimewarp(showMsg: false);
				return;
			}
			goto IL_0124;
		}
		if (!(timeNew <= warp_Point.endTime))
		{
			worldTime = warp_Point.endTime;
			StopTimewarp(showMsg: false);
		}
		return;
		IL_0124:
		if (flag)
		{
			double num2 = Math.Max((location.time - timeNew) / 120.0, 0.0) + 3.0;
			if (timeOld + num2 < timeNew)
			{
				worldTime = timeOld + num2;
			}
		}
	}

	public void SetState(double timewarpSpeed, bool realtimePhysics, bool showMsg)
	{
		this.timewarpSpeed = timewarpSpeed;
		this.realtimePhysics.Value = realtimePhysics;
		Time.timeScale = TimeScale;
		if (showMsg)
		{
			MsgDrawer.main.Log(Loc.main.Msg_Timewarp_Speed.Inject(timewarpSpeed.ToString(CultureInfo.InvariantCulture), "speed"));
		}
	}
}
