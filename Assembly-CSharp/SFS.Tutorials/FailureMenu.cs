using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Input;
using SFS.Stats;
using SFS.Translations;
using SFS.UI;
using SFS.World;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.Tutorials;

public class FailureMenu : Screen_Menu
{
	public static FailureMenu main;

	public GameObject holder;

	public TextAdapter reasonText;

	public Button revertToLaunch;

	public Button revertToBuild;

	public Button revertTo_30_Sec;

	public Button revertTo_3_Min;

	public Button revertToSave;

	private DestructionReason reason;

	protected override CloseMode OnEscape => CloseMode.Current;

	private void Awake()
	{
		main = this;
	}

	private void Start()
	{
		holder.SetActive(value: false);
		revertToLaunch.onClick += (Action)delegate
		{
			GameManager.main.RevertToLaunch(skipConfirmation: true);
		};
		revertToBuild.onClick += (Action)delegate
		{
			GameManager.main.RevertToBuild(skipConfirmation: true);
		};
		revertTo_30_Sec.onClick += new Action(Revert_30_Sec);
		revertTo_3_Min.onClick += new Action(Revert_3_Min);
		revertToSave.onClick += (Action)delegate
		{
			GameManager.main.OpenLoad();
		};
	}

	public void Revert_3_Min()
	{
		if (Revert.TryGetRevert_3_Min(out var save))
		{
			LoadRevert(save);
		}
	}

	public void Revert_30_Sec()
	{
		if (Revert.TryGetRevert_30_Sec(out var save))
		{
			LoadRevert(save);
		}
	}

	private void LoadRevert(WorldSave save)
	{
		GameManager.main.LoadSave(save, forLaunch: false, MsgDrawer.main);
		Close();
		MsgDrawer.main.Log(Loc.main.Loading_In_Progress);
	}

	public void OnPlayerDestroyed(Rocket rocket, DestructionReason reason)
	{
		if (reason == DestructionReason.Intentional)
		{
			return;
		}
		Dictionary<int, int> parents = GetParents(rocket);
		List<Rocket> list = new List<Rocket>();
		foreach (Rocket rocket3 in GameManager.main.rockets)
		{
			if (rocket3.physics.loader.Loaded && rocket3 != rocket && GetParents(rocket3).Any((KeyValuePair<int, int> a) => parents.ContainsKey(a.Key)))
			{
				list.Add(rocket3);
			}
		}
		if (list.Count > 0)
		{
			Double2 globalPosition = WorldView.ToGlobalPosition(rocket.rb2d.worldCenterOfMass + PlayerController.main.cameraOffset.Value);
			Rocket.SetPlayerToBestControllable(list.ToArray());
			Rocket rocket2 = (Rocket)PlayerController.main.player.Value;
			rocket2.location.position.Value = WorldView.ToGlobalPosition(rocket2.physics.PhysicsObject.LocalPosition);
			PlayerController.main.SetOffset(WorldView.ToLocalPosition(globalPosition) - rocket2.rb2d.worldCenterOfMass, 0.5f);
		}
		else
		{
			this.reason = reason;
			Invoke("ShowFailure", 3f * Time.timeScale);
		}
		static Dictionary<int, int> GetParents(Rocket r)
		{
			int key = r.stats.branch;
			Dictionary<int, int> dictionary = new Dictionary<int, int>();
			Branch value;
			while (LogManager.main.branches.TryGetValue(key, out value) && value.startTime + 5.0 > WorldTime.main.worldTime)
			{
				dictionary.Add(key, -1);
				key = value.parentA;
			}
			return dictionary;
		}
	}

	private void ShowFailure()
	{
		if (!(PlayerController.main.player.Value != null))
		{
			Open();
			switch (reason)
			{
			case DestructionReason.RocketCollision:
				reasonText.Text = string.Concat(Loc.main.Failure_Cause, "\n", Loc.main.Failure_Crash_Into_Rocket);
				break;
			case DestructionReason.TerrainCollision:
				reasonText.Text = string.Concat(Loc.main.Failure_Cause, "\n", Loc.main.Failure_Crash_Into_Terrain.InjectField(WorldView.main.ViewLocation.planet.DisplayName, "planet"));
				break;
			case DestructionReason.Overheat:
				reasonText.Text = string.Concat(Loc.main.Failure_Cause, "\n", Loc.main.Failure_Burn_Up);
				break;
			}
			bool allowQuicksaves = Base.worldBase.settings.mode.allowQuicksaves;
			revertToLaunch.SetEnabled(allowQuicksaves);
			revertToBuild.SetEnabled(allowQuicksaves);
			revertTo_30_Sec.SetEnabled(allowQuicksaves && Revert.HasRevert_30_Sec());
			revertTo_3_Min.SetEnabled(allowQuicksaves && Revert.HasRevert_3_Min());
			revertToSave.SetEnabled(allowQuicksaves);
		}
	}

	public override void OnOpen()
	{
		holder.SetActive(value: true);
	}

	public override void OnClose()
	{
		holder.SetActive(value: false);
	}
}
