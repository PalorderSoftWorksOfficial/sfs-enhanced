using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFS.Career;
using SFS.Parts.Modules;
using SFS.Translations;
using SFS.Variables;
using SFS.World;
using SFS.World.Drag;
using UnityEngine;

namespace SFS.Parts;

public class Part : HeatModuleBase, Rocket.INJ_Rocket
{
	public TranslationVariable displayName = new TranslationVariable();

	public TranslationVariable description = new TranslationVariable();

	public Composed_Float mass;

	public Composed_Vector2 centerOfMass;

	public OrientationModule orientation;

	public VariablesModule variablesModule;

	public Variants[] variants = Array.Empty<Variants>();

	public UsePartUnityEvent onPartUsed;

	[NonSerialized]
	public BurnMark burnMark;

	private Dictionary<string, object> modules = new Dictionary<string, object>();

	private Dictionary<string, int> moduleCount = new Dictionary<string, int>();

	public float temperature = float.NegativeInfinity;

	public Action<Part> aboutToDestroy;

	public Action<Part> onPartDestroyed;

	public Rocket Rocket { get; set; }

	public Vector2 Position
	{
		get
		{
			return base.transform.localPosition;
		}
		set
		{
			base.transform.localPosition = value;
		}
	}

	public override string Name => GetDisplayName();

	public override bool IsHeatShield => false;

	public override float Temperature
	{
		get
		{
			return temperature;
		}
		set
		{
			temperature = value;
		}
	}

	public override int LastAppliedIndex { get; set; } = -1;

	public override float ExposedSurface { get; set; }

	public override float HeatTolerance => AeroModule.GetHeatTolerance(SFS.World.Drag.HeatTolerance.Low);

	public void DrawPartStats(Part[] allParts, StatsMenu drawer, PartDrawSettings settings)
	{
		if (settings.showTitle)
		{
			drawer.DrawTitle(GetDisplayName());
		}
		if ((settings.build || settings.game) && HasModule<CrewModule>() && !DevSettings.DisableAstronauts)
		{
			((I_PartMenu)GetModules<CrewModule>()[0]).Draw(drawer, settings);
			return;
		}
		drawer.DrawStat(100, () => mass.Value.ToMassString(forceDecimal: false), null, delegate(Action update)
		{
			mass.OnChange += update;
		}, delegate(Action update)
		{
			mass.OnChange -= update;
		});
		I_PartMenu[] array = GetModules<I_PartMenu>().ToArray();
		for (int num = 0; num < array.Length; num++)
		{
			array[num].Draw(drawer, settings);
		}
		DrawMultiple<ResourceModule>((ResourceModule x) => x.showDescription, (ResourceModule x) => x.resourceType.GetInstanceID().ToString(), delegate(ResourceModule a, List<ResourceModule> b, StatsMenu c, PartDrawSettings d)
		{
			a.Draw(b, c, d);
		});
		DrawMultiple<ToggleModule>((ToggleModule x) => true, (ToggleModule x) => x.label.Field, delegate(ToggleModule a, List<ToggleModule> b, StatsMenu c, PartDrawSettings d)
		{
			a.Draw(b, c, d);
		});
		DrawMultiple<EngineModule>((EngineModule x) => true, (EngineModule x) => "", delegate(EngineModule a, List<EngineModule> b, StatsMenu c, PartDrawSettings d)
		{
			a.Draw(b, c, d);
		});
		DrawMultiple<SeparatorBase>((SeparatorBase x) => x.ShowDescription, (SeparatorBase x) => "", delegate(SeparatorBase a, List<SeparatorBase> b, StatsMenu c, PartDrawSettings d)
		{
			a.Draw(b, c, d);
		});
		DrawMultiple<WheelModule>((WheelModule x) => true, (WheelModule x) => "", delegate(WheelModule a, List<WheelModule> b, StatsMenu c, PartDrawSettings d)
		{
			a.Draw(b, c, d);
		});
		DrawMultiple<DockingPortModule>((DockingPortModule x) => true, (DockingPortModule x) => "", delegate(DockingPortModule a, List<DockingPortModule> b, StatsMenu c, PartDrawSettings d)
		{
			a.Draw(b, c, d);
		});
		DrawMultiple<LES_Module>((LES_Module x) => true, (LES_Module x) => "", delegate(LES_Module a, List<LES_Module> b, StatsMenu c, PartDrawSettings d)
		{
			a.Draw(b, c, d);
		});
		DrawMultiple<VariablesDrawer>((VariablesDrawer x) => true, delegate(VariablesDrawer x)
		{
			StringBuilder stringBuilder = new StringBuilder();
			VariablesDrawer.DrawElement[] elements = x.elements;
			foreach (VariablesDrawer.DrawElement drawElement in elements)
			{
				stringBuilder.Append(drawElement.variableType.ToString());
				stringBuilder.Append(drawElement.label.Field);
			}
			return stringBuilder.ToString();
		}, delegate(VariablesDrawer a, List<VariablesDrawer> b, StatsMenu c, PartDrawSettings d)
		{
			a.Draw(b, c, d);
		});
		if (settings.showDescription)
		{
			string text = description.Field;
			if (!string.IsNullOrEmpty(text))
			{
				drawer.DrawText(-1000, text);
			}
		}
		void DrawMultiple<T>(Func<T, bool> drawn, Func<T, string> getID, Action<T, List<T>, StatsMenu, PartDrawSettings> draw)
		{
			Dictionary<string, List<T>> dictionary = new Dictionary<string, List<T>>();
			T[] array2 = GetModules<T>().Where(drawn.Invoke).ToArray();
			if (array2.Length == 0)
			{
				return;
			}
			for (int i = 0; i < array2.Length; i++)
			{
				T arg = array2[i];
				dictionary.Add(i + getID(arg), new List<T>());
			}
			Part[] array3 = allParts;
			for (int j = 0; j < array3.Length; j++)
			{
				T[] array4 = array3[j].GetModules<T>().Where(drawn.Invoke).ToArray();
				for (int k = 0; k < array4.Length; k++)
				{
					T val = array4[k];
					string key = k + getID(val);
					if (dictionary.ContainsKey(key))
					{
						dictionary[key].Add(val);
					}
				}
			}
			foreach (List<T> value in dictionary.Values)
			{
				draw(value[0], value, drawer, settings);
			}
		}
	}

	public T[] GetModules<T>()
	{
		string key = typeof(T).Name;
		if (!modules.ContainsKey(key))
		{
			modules.Add(key, GetComponentsInChildren<T>(includeInactive: true));
		}
		return (T[])modules[key];
	}

	public int GetModuleCount<T>()
	{
		string key = typeof(T).Name;
		if (!moduleCount.ContainsKey(key))
		{
			moduleCount.Add(key, GetComponentsInChildren<T>(includeInactive: true).Length);
		}
		return moduleCount[key];
	}

	public bool HasModule<T>()
	{
		return GetModuleCount<T>() > 0;
	}

	public Field GetDisplayName()
	{
		return displayName.Field;
	}

	public List<PolygonData> GetClickPolygons()
	{
		return (from x in GetModules<PolygonData>()
			where x.Click
			select x).ToList();
	}

	public (ConvexPolygon[], bool isFront) GetBuildColliderPolygons(bool forAttach = false)
	{
		List<ConvexPolygon> list = new List<ConvexPolygon>();
		PolygonData[] array = GetModules<PolygonData>();
		foreach (PolygonData polygonData in array)
		{
			if (polygonData.BuildCollider && (!forAttach || polygonData.AttachByOverlap))
			{
				list.AddRange(polygonData.polygon.GetConvexPolygonsWorld(polygonData.transform));
			}
		}
		return (list.ToArray(), isFront: IsFront());
	}

	public bool IsFront()
	{
		return false;
	}

	public Line2[] GetAttachmentSurfacesWorld()
	{
		List<Line2> list = new List<Line2>();
		SurfaceData[] array = GetModules<SurfaceData>();
		foreach (SurfaceData surfaceData in array)
		{
			if (!surfaceData.Attachment)
			{
				continue;
			}
			foreach (Surfaces surface in surfaceData.surfaces)
			{
				list.AddRange(surface.GetSurfacesWorld());
			}
		}
		return list.ToArray();
	}

	public OwnershipState GetOwnershipState()
	{
		if (!GetModules<OwnModule>().All((OwnModule a) => a.IsOwned || !a.IsPremium))
		{
			return OwnershipState.NotOwned;
		}
		if (!CareerState.main.HasPart(this))
		{
			return OwnershipState.NotUnlocked;
		}
		return OwnershipState.OwnedAndUnlocked;
	}

	public void InitializePart()
	{
		List<I_InitializePartModule> list = new List<I_InitializePartModule>(GetComponentsInChildren<I_InitializePartModule>(includeInactive: true));
		list.Sort((I_InitializePartModule a, I_InitializePartModule b) => b.Priority.CompareTo(a.Priority));
		list.ForEach(delegate(I_InitializePartModule a)
		{
			a.Initialize();
		});
	}

	public void SetSortingLayer(string sortingLayer)
	{
		BaseMesh[] array = GetModules<BaseMesh>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetSortingLayer(sortingLayer);
		}
		ModelSetup[] array2 = GetModules<ModelSetup>();
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].SetSortingLayer(sortingLayer);
		}
	}

	public void RegenerateMesh()
	{
		BaseMesh[] array = GetModules<BaseMesh>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].GenerateMesh();
		}
		ModelSetup[] array2 = GetModules<ModelSetup>();
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].SetMesh();
		}
		SeparatorPanel[] componentsInChildren = GetComponentsInChildren<SeparatorPanel>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].FlipToLight();
		}
	}

	public override void OnOverheat(bool breakup)
	{
		OnOverheat(this, breakup);
	}

	public void OnOverheat(HeatModuleBase module, bool breakup)
	{
		List<PartJoint> connectedJoints = Rocket.jointsGroup.GetConnectedJoints(this);
		if (breakup && connectedJoints.Count > 0 && !module.IsHeatShield)
		{
			Rocket rocket = Rocket;
			JointGroup.DestroyJoint(connectedJoints[0], Rocket, out var split, out var newRocket);
			EffectManager.CreatePartOverheatEffect(base.transform.TransformPoint(centerOfMass.Value), mass.Value * 2f + 0.5f);
			if (split)
			{
				rocket.EnableCollisionImmunity(1.5f);
				newRocket.EnableCollisionImmunity(1.5f);
				if ((bool)rocket.isPlayer)
				{
					Rocket.SetPlayerToBestControllable(rocket, newRocket);
				}
				module.Temperature *= 0.8f;
			}
		}
		else
		{
			EffectManager.CreatePartOverheatEffect(base.transform.TransformPoint(centerOfMass.Value), mass.Value * 2f + 0.5f);
			DestroyPart(!breakup, updateJoints: true, DestructionReason.Overheat);
		}
	}

	public void DestroyPart(bool createExplosion, bool updateJoints, DestructionReason reason)
	{
		if (createExplosion)
		{
			EffectManager.CreateExplosion(base.transform.TransformPoint(centerOfMass.Value), mass.Value * 2f + 0.5f);
		}
		Action<Part> action = onPartDestroyed;
		onPartDestroyed = null;
		action?.Invoke(this);
		if (updateJoints)
		{
			JointGroup.OnPartDestroyed(this, Rocket, reason);
		}
		UnityEngine.Object.Destroy(base.gameObject);
	}
}
