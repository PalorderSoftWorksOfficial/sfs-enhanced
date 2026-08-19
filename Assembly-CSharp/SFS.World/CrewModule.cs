using System;
using System.Linq;
using SFS.Builds;
using SFS.Career;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.Stats;
using SFS.Translations;
using SFS.UI;
using SFS.Variables;
using UnityEngine;

namespace SFS.World;

public class CrewModule : MonoBehaviour, Rocket.INJ_Rocket, I_PartMenu, I_InitializePartModule
{
	[Serializable]
	public class Seat
	{
		public String_Reference astronaut;

		public Vector2 hatchPosition;

		public bool externalSeat;

		public GameObject astronautModel;

		public EVA_Resources resources;

		public bool HasAstronaut => !string.IsNullOrEmpty(astronaut.Value);

		public void OnStart()
		{
			if (!HasAstronaut)
			{
				return;
			}
			if (AstronautState.main.GetAstronautState(astronaut.Value) == AstronautState.State.Available)
			{
				AstronautState.main.AddCrew(astronaut.Value);
				AddSeatedAstronaut();
				return;
			}
			Debug.Log("Astronaut " + astronaut.Value + " is not available");
			astronaut.Value = "";
			if (externalSeat)
			{
				resources.fuelPercent.Value = -1.0;
			}
		}

		public void OnDestroy()
		{
			if (HasAstronaut)
			{
				AstronautState.main.RemoveCrew(astronaut.Value);
				if (GameManager.main != null)
				{
					AstronautState.main.GetAstronautByName(astronaut.Value).alive = false;
				}
			}
		}

		public void Board(string astronautName, double fuelPercent, float temperature)
		{
			AstronautState.main.AddCrew(astronautName);
			astronaut.Value = astronautName;
			if (externalSeat)
			{
				AddSeatedAstronaut();
				resources.fuelPercent.Value = fuelPercent;
				resources.temperature.Value = temperature;
			}
		}

		public void Exit()
		{
			if (!DevSettings.DisableAstronauts)
			{
				AstronautState.main.RemoveCrew(astronaut.Value);
				astronaut.Value = "";
				if (externalSeat)
				{
					RemoveSeatedAstronaut();
				}
			}
		}

		private void AddSeatedAstronaut()
		{
			if (externalSeat)
			{
				astronautModel.SetActive(value: true);
			}
		}

		private void RemoveSeatedAstronaut()
		{
			if (externalSeat)
			{
				astronautModel.SetActive(value: false);
			}
		}
	}

	public float baseMass;

	public Bool_Reference hasControl;

	public Bool_Reference needsCrewForControl;

	public Seat[] seats;

	public GameObject interior;

	public GameObject hatch;

	private Part part;

	public bool HasCrew => seats.Any((Seat s) => !string.IsNullOrEmpty(s.astronaut.Value));

	public Rocket Rocket { get; set; }

	int I_InitializePartModule.Priority => 0;

	void I_InitializePartModule.Initialize()
	{
		part = GetComponentInParent<Part>();
		needsCrewForControl.OnChange += new Action(OnSeatChange);
		Seat[] array = seats;
		foreach (Seat obj in array)
		{
			obj.OnStart();
			obj.astronaut.OnChange += new Action(OnSeatChange);
		}
		if (DevSettings.DisableAstronauts && !Application.isEditor)
		{
			needsCrewForControl.Variable.Save = false;
			array = seats;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].astronaut.Variable.Save = false;
			}
		}
	}

	private void OnSeatChange()
	{
		bool flag = DevSettings.DisableAstronauts || seats.Any((Seat s) => s.HasAstronaut) || !needsCrewForControl.Value;
		hasControl.Value = flag;
		if (hatch != null)
		{
			hatch.SetActive(flag);
		}
		if (interior != null)
		{
			interior.SetActive(!flag);
		}
		part.mass.Value = baseMass + seats.Sum((Seat s) => (!s.HasAstronaut) ? 0f : 0.2f);
	}

	private void OnDestroy()
	{
		Seat[] array = seats;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnDestroy();
		}
	}

	public void OpenPartMenu_Seats()
	{
		if (!DevSettings.DisableAstronauts)
		{
			OpenPartMenu(canBoardWorld: false);
		}
	}

	public void OpenPartMenu(bool canBoardWorld)
	{
		PartDrawSettings settings = ((BuildManager.main != null) ? PartDrawSettings.BuildSettings : PartDrawSettings.WorldSettings);
		AttachableStatsMenu menu = ((BuildManager.main != null) ? BuildManager.main.buildMenus.partMenu : UnityEngine.Object.FindObjectOfType<AttachableStatsMenu>(includeInactive: true));
		if (canBoardWorld)
		{
			settings.canBoardWorld = true;
		}
		menu.Open(() => true, delegate(StatsMenu drawer)
		{
			part.DrawPartStats(null, drawer, settings);
		}, AttachWithArrow.FollowPart(part), dontUpdateOnZoomChange: false, skipAnimation: false, delegate
		{
			Part obj = part;
			obj.onPartDestroyed = (Action<Part>)Delegate.Combine(obj.onPartDestroyed, new Action<Part>(Close));
		}, delegate
		{
			Part obj = part;
			obj.onPartDestroyed = (Action<Part>)Delegate.Remove(obj.onPartDestroyed, new Action<Part>(Close));
		});
		void Close(Part _)
		{
			menu.Close();
		}
	}

	void I_PartMenu.Draw(StatsMenu drawer, PartDrawSettings settings)
	{
		if (DevSettings.DisableAstronauts)
		{
			return;
		}
		bool drawSeats = settings.build || settings.game;
		drawer.DrawStat(90, delegate
		{
			string value = (drawSeats ? (seats.Count((Seat seat) => seat.HasAstronaut) + " / " + seats.Length) : seats.Length.ToString());
			return Loc.main.Crew_Count.Inject(value, "count");
		}, null, delegate(Action update)
		{
			Seat[] array = seats;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].astronaut.OnChange += update;
			}
		}, delegate(Action update)
		{
			Seat[] array = seats;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].astronaut.OnChange -= update;
			}
		});
		if (drawSeats)
		{
			DrawSeats(drawer, settings);
		}
	}

	private void DrawSeats(StatsMenu drawer, PartDrawSettings settings)
	{
		Seat[] array = seats;
		foreach (Seat a in array)
		{
			Seat seat = a;
			Action update = null;
			drawer.DrawButton(-1, () => (!seat.HasAstronaut) ? "" : seat.astronaut.Value, () => (!(BuildManager.main != null)) ? (seat.HasAstronaut ? Loc.main.EVA_Exit : Loc.main.EVA_Board) : (seat.HasAstronaut ? Loc.main.Crew_Remove : Loc.main.Crew_Assign), OnClick, () => BuildManager.main != null || seat.HasAstronaut || settings.canBoardWorld, delegate(Action u)
			{
				update = u;
			}, null);
			void OnClick()
			{
				if (BuildManager.main != null)
				{
					if (!seat.HasAstronaut)
					{
						AstronautMenu.main.OpenMenu(seat, update);
					}
					else
					{
						seat.Exit();
						update?.Invoke();
					}
				}
				else
				{
					if (!seat.HasAstronaut)
					{
						EVA_Board(a);
					}
					else
					{
						EVA_Exit(a);
					}
					drawer.Close();
				}
			}
		}
	}

	private void EVA_Board(Seat seat)
	{
		if (!seat.HasAstronaut && PlayerController.main.player.Value is Astronaut_EVA astronaut_EVA && astronaut_EVA.hasControl.Value)
		{
			if ((base.transform.TransformPoint(seat.hatchPosition) - (Vector3)astronaut_EVA.rb2d.worldCenterOfMass).sqrMagnitude > 400f)
			{
				MsgDrawer.main.Log("Cannot board from this far away");
				return;
			}
			seat.Board(astronaut_EVA.astronaut.astronautName, astronaut_EVA.resources.fuelPercent.Value, astronaut_EVA.resources.temperature.Value);
			StatsRecorder.OnMerge(Rocket.stats, astronaut_EVA.stats);
			AstronautManager.DestroyEVA(astronaut_EVA, death: false);
			PlayerController.main.SmoothChangePlayer(Rocket, 1f);
		}
	}

	private void EVA_Exit(Seat seat)
	{
		Location location = new Location(Rocket.location.planet, WorldView.ToGlobalPosition(base.transform.TransformPoint(seat.hatchPosition)), Rocket.location.velocity);
		if (location.TerrainHeight < 100.0)
		{
			int num = ((location.position.AngleRadians < Rocket.location.position.Value.AngleRadians) ? 1 : (-1));
			double angleRadians = location.position.AngleRadians - (double)(3 * num) / location.position.magnitude;
			Location location2 = new Location(location.planet, Double2.CosSin(angleRadians, location.Radius));
			Astronaut_EVA.GetGroundRadius(location, out var _, out var groundRadius);
			location2.position = Double2.CosSin(angleRadians, groundRadius);
			if ((location2.position - location.position).Mag_LessThan(10.0) && location.planet.data.basics.gravity > 5.0)
			{
				location = location2;
			}
		}
		string value = seat.astronaut.Value;
		double fuelPercent = (seat.externalSeat ? seat.resources.fuelPercent.Value : 1.0);
		float temperature = (seat.externalSeat ? seat.resources.temperature.Value : float.NegativeInfinity);
		seat.Exit();
		Astronaut_EVA astronaut_EVA = AstronautManager.main.SpawnEVA(value, location, Astronaut_EVA.GetTargetAngle(location), 0f, ragdoll: false, fuelPercent, temperature);
		StatsRecorder.OnSplit(Rocket.stats, astronaut_EVA.stats);
		astronaut_EVA.stats.OnLeaveCapsule(value);
		PlayerController.main.SmoothChangePlayer(astronaut_EVA, 1f);
	}
}
