using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Stats;
using SFS.Translations;
using SFS.UI;
using SFS.WorldBase;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.Logs;

[Serializable]
public class Challenge
{
	[HideInInspector]
	public int displayPriority;

	public string id;

	[HideInInspector]
	public Planet owner;

	[HideInInspector]
	public Sprite icon;

	public Func<string> title;

	public Func<string> description;

	[HideInInspector]
	public Difficulty difficulty;

	public List<ChallengeStep> steps;

	[HideInInspector]
	public bool returnSafely;

	public Challenge(int displayPriority, string id, Planet owner, Sprite icon, Func<string> title, Func<string> description, Difficulty difficulty, bool returnSafely, List<ChallengeStep> steps)
	{
		this.displayPriority = displayPriority;
		this.id = id;
		this.owner = owner;
		this.icon = icon;
		this.title = title;
		this.description = description;
		this.difficulty = difficulty;
		this.returnSafely = returnSafely;
		this.steps = steps;
		if (returnSafely && Base.planetLoader.planets.TryGetValue("Earth", out var value))
		{
			steps.Add(new Step_Land
			{
				planet = value
			});
		}
	}

	public static List<Challenge> CollectChallenges()
	{
		PlanetLoader planetLoader = Base.planetLoader;
		planetLoader.planets.TryGetValue("Sun", out var _);
		planetLoader.planets.TryGetValue("Mercury", out var value2);
		planetLoader.planets.TryGetValue("Venus", out var value3);
		planetLoader.planets.TryGetValue("Earth", out var value4);
		planetLoader.planets.TryGetValue("Moon", out var value5);
		planetLoader.planets.TryGetValue("Near_Earth_Asteroid", out var value6);
		planetLoader.planets.TryGetValue("Mars", out var value7);
		planetLoader.planets.TryGetValue("Phobos", out var value8);
		planetLoader.planets.TryGetValue("Deimos", out var value9);
		if (Application.isEditor)
		{
			ResourcesLoader.main = UnityEngine.Object.FindObjectOfType<ResourcesLoader>();
		}
		ResourcesLoader.ChallengeIcons challengeIcons = ResourcesLoader.main.challengeIcons;
		SFS_Translation t = Loc.main;
		List<Challenge> challenges = new List<Challenge>();
		Add(0, "Liftoff_0", value4, challengeIcons.firstFlight, () => t.Liftoff_Title, () => t.Liftoff, Difficulty.Easy, returnSafely: true, new ChallengeStep[1]
		{
			new Step_Height
			{
				height = 100,
				checkVelocity = true,
				planet = value4
			}
		});
		Add(0, "Reach_10km", value4, challengeIcons.icon_10Km, () => t.Reach10km_Title, () => t.Reach10km, Difficulty.Easy, returnSafely: true, new ChallengeStep[1]
		{
			new Step_Height
			{
				height = 10000,
				planet = value4
			}
		});
		SFS.WorldBase.Difficulty.DifficultyType difficultyType = Base.worldBase.settings.difficulty.difficulty;
		Sprite sprite = difficultyType switch
		{
			SFS.WorldBase.Difficulty.DifficultyType.Normal => challengeIcons.icon_30Km, 
			SFS.WorldBase.Difficulty.DifficultyType.Hard => challengeIcons.icon_50Km, 
			_ => challengeIcons.icon_100Km, 
		};
		double height = 1000 * difficultyType switch
		{
			SFS.WorldBase.Difficulty.DifficultyType.Normal => 30, 
			SFS.WorldBase.Difficulty.DifficultyType.Hard => 50, 
			_ => 100, 
		};
		Add(0, "Reach_30km", value4, sprite, () => t.ReachSpace_Title, () => t.ReachSpace.Inject(height.ToDistanceString(decimals: false), "height"), Difficulty.Medium, returnSafely: true, new ChallengeStep[1]
		{
			new Step_Height
			{
				height = (int)height,
				planet = value4
			}
		});
		Add(0, "Reach_Downrange", value4, challengeIcons.icon_Downrange, () => t.Land100kmDownrange_Title, () => t.Land100kmDownrange, Difficulty.Medium, returnSafely: true, new ChallengeStep[1]
		{
			new Step_Downrange
			{
				downrange = 100000,
				planet = value4
			}
		});
		Add(0, "Reach_Orbit", value4, challengeIcons.icon_Reach_Orbit, () => t.ReachLowEarthOrbit_Title, () => t.ReachLowEarthOrbit, Difficulty.Hard, returnSafely: true, new ChallengeStep[1]
		{
			new Step_Orbit
			{
				orbit = StatsRecorder.Tracker.State_Orbit.Low,
				planet = value4
			}
		});
		Add(0, "Orbit_High", value4, challengeIcons.icon_Orbit_High, () => t.ReachHighEarthOrbit_Title, () => t.ReachHighEarthOrbit, Difficulty.Hard, returnSafely: true, new ChallengeStep[1]
		{
			new Step_Orbit
			{
				orbit = StatsRecorder.Tracker.State_Orbit.High,
				planet = value4
			}
		});
		Add(1, "Moon_Orbit", value5, challengeIcons.icon_Capture, () => t.MoonOrbit_Title, () => t.MoonOrbit, Difficulty.Medium, returnSafely: true, new ChallengeStep[1]
		{
			new Step_Orbit
			{
				orbit = StatsRecorder.Tracker.State_Orbit.Low,
				planet = value5
			}
		});
		Add(-1, "Moon_Tour", value5, challengeIcons.icon_Tour, () => t.MoonTour_Title, () => t.MoonTour, Difficulty.Extreme, returnSafely: true, new ChallengeStep[1]
		{
			new Step_Any_Landmarks
			{
				count = 3,
				planet = value5
			}
		});
		Add(1, "Asteroid_Crash", value6, challengeIcons.icon_Crash, () => t.AsteroidImpact_Title, () => t.AsteroidImpact, Difficulty.Medium, returnSafely: false, new ChallengeStep[1]
		{
			new Step_Impact
			{
				planet = value6,
				impactVelocity = 200
			}
		});
		if (value8 != null && value9 != null)
		{
			Add(-2, "Mars_Tour", value7, challengeIcons.icon_Tour, () => t.MarsGrandTour_Title, () => t.MarsGrandTour, Difficulty.Hard, returnSafely: true, new ChallengeStep[1]
			{
				new MultiStep
				{
					steps = new List<ChallengeStep>
					{
						new Step_Land
						{
							planet = value7
						},
						new Step_Land
						{
							planet = value8
						},
						new Step_Land
						{
							planet = value9
						}
					}
				}
			});
		}
		Add(0, "Venus_One_Way", value3, challengeIcons.icon_UnmannedLanding, () => t.VenusLanding_Title, () => t.VenusLanding, Difficulty.Medium, returnSafely: false, new ChallengeStep[1]
		{
			new Step_Land
			{
				planet = value3
			}
		});
		Add(0, "Venus_Landing", value3, challengeIcons.icon_MannedLanding, () => t.VenusReturn_Title, () => t.VenusReturn, Difficulty.Extreme, returnSafely: true, new ChallengeStep[1]
		{
			new Step_Land
			{
				planet = value3
			}
		});
		Add(0, "Mercury_One_Way", value2, challengeIcons.icon_UnmannedLanding, () => t.MercuryLanding_Title, () => t.MercuryLanding, Difficulty.Hard, returnSafely: false, new ChallengeStep[1]
		{
			new Step_Land
			{
				planet = value2
			}
		});
		Add(0, "Mercury_Landing", value2, challengeIcons.icon_MannedLanding, () => t.MercuryReturn_Title, () => t.MercuryReturn, Difficulty.Extreme, returnSafely: true, new ChallengeStep[1]
		{
			new Step_Land
			{
				planet = value2
			}
		});
		foreach (Planet planet in planetLoader.planets.Values)
		{
			if (!(planet == planetLoader.spaceCenter.Planet) && planet.data.hasTerrain && planet.data.terrain.collider && planet != value3 && planet != value2)
			{
				Add(0, "Land_" + planet.name, planet, challengeIcons.icon_MannedLanding, () => t.LandAndReturn_Title.InjectField(planet.DisplayName, "planet"), () => t.LandAndReturn.InjectField(planet.DisplayName, "planet"), Difficulty.Hard, returnSafely: true, new ChallengeStep[1]
				{
					new Step_Land
					{
						planet = planet
					}
				});
			}
		}
		challenges.Sort((Challenge a, Challenge b) => b.displayPriority.CompareTo(a.displayPriority));
		return challenges;
		void Add(int priority, string id, Planet planet2, Sprite icon, Func<string> title, Func<string> description, Difficulty difficulty, bool returnSafely, ChallengeStep[] steps)
		{
			if (planet2 != null)
			{
				challenges.Add(new Challenge(priority * 100 - challenges.Count, id, planet2, icon, title, description, difficulty, returnSafely, steps.ToList()));
			}
		}
	}

	public static RectTransform CreateChallengeUI(Challenge challenge, bool complete, RectTransform prefab, Transform holder, bool showDifficulty)
	{
		RectTransform rectTransform = UnityEngine.Object.Instantiate(prefab, holder);
		rectTransform.gameObject.SetActive(value: true);
		bool flag = true;
		Image component = rectTransform.GetChild(0).GetChild(0).GetChild(0)
			.GetComponent<Image>();
		component.sprite = challenge.icon;
		component.color = (complete ? Color.white : new Color(1f, 1f, 1f, 0.25f));
		string text = challenge.description();
		text = "<size=65>" + (flag ? challenge.title() : "???") + "</size><size=15>\n\n</size>" + (flag ? text : "?????");
		rectTransform.GetChild(1).GetComponent<TextAdapter>().Text = Wrap(text);
		string text2 = (showDifficulty ? (challenge.difficulty switch
		{
			Difficulty.Easy => "Easy", 
			Difficulty.Medium => "Medium", 
			Difficulty.Hard => "Hard", 
			Difficulty.Extreme => "Extreme", 
			_ => "", 
		}) : "");
		string a = text2;
		rectTransform.GetChild(2).GetComponent<TextAdapter>().Text = Wrap(a);
		return rectTransform;
		string Wrap(string text3)
		{
			if (!complete)
			{
				return "<color=#ffffff30>" + text3 + "</color>";
			}
			return text3;
		}
	}
}
