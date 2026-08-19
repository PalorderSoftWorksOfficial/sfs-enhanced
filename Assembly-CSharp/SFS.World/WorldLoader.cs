using System;
using UnityEngine;

namespace SFS.World;

public class WorldLoader : MonoBehaviour
{
	public WorldLocation location;

	public GameObject holder;

	public double loadDistance;

	public bool Loaded { get; private set; }

	public event Action<bool, bool> onLoadedChange_Before;

	public event Action<bool, bool> onLoadedChange_After;

	private void Start()
	{
		WorldView main = WorldView.main;
		main.onViewLocationChange_Before = (Action<Location, Location>)Delegate.Combine(main.onViewLocationChange_Before, new Action<Location, Location>(OnLocationChange_Before));
		WorldView main2 = WorldView.main;
		main2.onViewLocationChange_After = (Action<Location, Location>)Delegate.Combine(main2.onViewLocationChange_After, new Action<Location, Location>(OnLocationChange_After));
		location.planet.OnChange += (Action)delegate
		{
			UpdateLoading(WorldView.main.ViewLocation);
		};
		location.position.OnChange += (Action)delegate
		{
			UpdateLoading(WorldView.main.ViewLocation);
		};
		SetLoaded(Loaded);
	}

	private void OnDestroy()
	{
		WorldView main = WorldView.main;
		main.onViewLocationChange_Before = (Action<Location, Location>)Delegate.Remove(main.onViewLocationChange_Before, new Action<Location, Location>(OnLocationChange_Before));
		WorldView main2 = WorldView.main;
		main2.onViewLocationChange_After = (Action<Location, Location>)Delegate.Remove(main2.onViewLocationChange_After, new Action<Location, Location>(OnLocationChange_After));
	}

	private void OnLocationChange_Before(Location oldValue, Location newValue)
	{
		if (Loaded)
		{
			UpdateLoading(newValue);
		}
	}

	private void OnLocationChange_After(Location oldValue, Location newValue)
	{
		if (!Loaded)
		{
			UpdateLoading(newValue);
		}
	}

	private void UpdateLoading(Location newValue)
	{
		bool flag = location.Value != null && location.planet.Value == newValue.planet && (location.Value.position - newValue.position).Mag_LessThan(loadDistance * (Loaded ? 1.2 : 0.8));
		if (flag != Loaded)
		{
			SetLoaded(flag);
		}
	}

	private void SetLoaded(bool newValue)
	{
		bool arg = newValue;
		onLoadedChange_Before?.Invoke(arg, newValue);
		Loaded = newValue;
		holder.SetActive(newValue);
		onLoadedChange_After?.Invoke(arg, newValue);
	}
}
