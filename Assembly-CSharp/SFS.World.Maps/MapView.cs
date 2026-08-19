using System;
using System.Collections;
using System.Linq;
using SFS.Input;
using SFS.Variables;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World.Maps;

public class MapView : MonoBehaviour
{
	[Serializable]
	public class View
	{
		public MapObject_Local target = new MapObject_Local();

		public Double2_Local position = new Double2_Local();

		public Double_Local distance = new Double_Local();

		public View(SelectableObject target, Double2 position, double distance)
		{
			this.target.Value = target;
			this.position.Value = position;
			this.distance.Value = distance;
		}

		public static View Lerp(View a, View b, double t)
		{
			t = ToLogarithmicLerp(a.distance, b.distance, t);
			return new View(a.target.Value, Double2.Lerp(a.position, b.position, t), Math_Utility.Lerp(a.distance, b.distance, t));
		}

		public static double ToLogarithmicLerp(double distance_A, double distance_B, double t)
		{
			double a = Math.Log10(distance_A);
			double b = Math.Log10(distance_B);
			double y = Math_Utility.Lerp(a, b, t);
			double value = Math.Pow(10.0, y);
			return Math_Utility.InverseLerp(distance_A, distance_B, value);
		}
	}

	[Serializable]
	public class MapObject_Local : Obs_Destroyable<SelectableObject>
	{
		protected override bool IsEqual(SelectableObject a, SelectableObject b)
		{
			return a == b;
		}
	}

	private const double MinViewDistance = 500.0;

	public Transform viewTransform;

	public View view;

	public AnimationCurve transitionCurve;

	private static double MaxViewDistance => Math.Max(Base.planetLoader.planets.Values.Where((Planet planet) => planet.orbit != null).Max((Planet planet) => planet.orbit.periapsis) * 25.0, 120000.0);

	public Double3 GetCameraPosition()
	{
		return ((view.target.Value == null) ? Double3.zero : ((Double3)view.target.Value.Location.GetSolarSystemPosition(WorldTime.main.worldTime))) + new Double3(view.position.Value.x, view.position.Value.y, 0.0 - (double)view.distance);
	}

	public float ToConstantSize(float a)
	{
		return a * (float)((double)view.distance / 1000.0);
	}

	private void Start()
	{
		Screen_Game map_Input = GameManager.main.map_Input;
		map_Input.onDrag = (Action<DragData>)Delegate.Combine(map_Input.onDrag, new Action<DragData>(OnDrag));
		Screen_Game map_Input2 = GameManager.main.map_Input;
		map_Input2.onZoom = (Action<ZoomData>)Delegate.Combine(map_Input2.onZoom, new Action<ZoomData>(OnZoom));
		view.distance.Filter = ViewDistanceFilter;
		view.target.OnChange += new Action<SelectableObject, SelectableObject>(OnTargetChange);
		view.distance.OnChange += new Action(OnViewChange);
		view.position.OnChange += new Action(OnViewChange);
	}

	private static double ViewDistanceFilter(double oldValue, double newValue)
	{
		return Math_Utility.Clamp(Math.Abs(newValue), 500.0, MaxViewDistance);
	}

	private void OnTargetChange(SelectableObject valueOld, SelectableObject valueNew)
	{
		if (valueNew == null && valueOld != null && valueOld.Location.planet != null)
		{
			SwitchTarget(valueOld, valueOld.Location.planet.mapPlanet);
		}
	}

	private void OnViewChange()
	{
		TryTrackParent();
		TryTrackSatellites();
	}

	private void OnDrag(DragData data)
	{
		view.position.Value += data.DeltaWorld((float)(double)view.distance);
	}

	private void OnZoom(ZoomData data)
	{
		view.position.Value += Double2.ToDouble2(data.zoomPosition.World(1f)) * (view.distance.Value * (double)(1f - data.zoomDelta));
		view.distance.Value *= data.zoomDelta;
	}

	public void ToggleFocus(SelectableObject a, float zoom = 0.8f)
	{
		if (!(a == null))
		{
			if (a is MapPlanet { FocusDistance: var focusDistance })
			{
				SetViewSmooth(new View(a, Double2.zero, (view.distance.Value > focusDistance) ? (focusDistance * 0.7) : (focusDistance / 0.7)));
			}
			else if (a.IsViewTarget())
			{
				SelectableObject mapPlanet2 = view.target.Value.Location.planet.mapPlanet;
				Double2 solarSystemPosition = view.target.Value.Location.GetSolarSystemPosition(WorldTime.main.worldTime);
				Double2 solarSystemPosition2 = mapPlanet2.Location.GetSolarSystemPosition(WorldTime.main.worldTime);
				Double2 @double = solarSystemPosition - solarSystemPosition2;
				SetViewSmooth(new View(mapPlanet2, view.position + @double, (double)view.distance / (double)zoom));
			}
			else
			{
				SetViewSmooth(new View(a, Double2.zero, (double)view.distance * (double)zoom));
			}
		}
	}

	public void FocusForTimewarpTo(SelectableObject a)
	{
		SetViewSmooth(new View(a, Double2.zero, (double)view.distance * 0.8));
	}

	private void TryTrackParent()
	{
		if (!(view.target.Value == null) && !view.target.Value.Focus_FocusConditions(view.position, view.distance))
		{
			SwitchTarget(view.target.Value, view.target.Value.Location.planet.mapPlanet);
		}
	}

	private void TryTrackSatellites()
	{
		if (!(view.target.Value is MapPlanet mapPlanet))
		{
			return;
		}
		Planet[] satellites = mapPlanet.planet.satellites;
		foreach (Planet planet in satellites)
		{
			if (planet.mapPlanet.Focus_FocusConditions(planet.mapPlanet.Location.position - view.position.Value, view.distance))
			{
				SwitchTarget(view.target.Value, planet.mapPlanet);
				break;
			}
		}
	}

	private void SwitchTarget(SelectableObject oldTarget, SelectableObject newTarget)
	{
		if (!(newTarget == null))
		{
			Double2 obj = ((oldTarget != null) ? oldTarget.Location.GetSolarSystemPosition(WorldTime.main.worldTime) : Double2.zero);
			Double2 solarSystemPosition = newTarget.Location.GetSolarSystemPosition(WorldTime.main.worldTime);
			Double2 @double = obj - solarSystemPosition;
			view.target.Value = newTarget;
			view.position.Value += @double;
		}
	}

	public void SetViewSmooth(View newView)
	{
		SetViewSmooth(view, newView, transitionCurve);
	}

	private void SetViewSmooth(View oldView, View newView, AnimationCurve curve)
	{
		Double2 obj = ((oldView.target.Value != null) ? (oldView.target.Value.Location.GetSolarSystemPosition(WorldTime.main.worldTime) + oldView.position) : Double2.zero);
		Double2 solarSystemPosition = newView.target.Value.Location.GetSolarSystemPosition(WorldTime.main.worldTime);
		Double2 position = obj - solarSystemPosition;
		oldView = new View(newView.target.Value, position, oldView.distance);
		if (base.gameObject.activeInHierarchy)
		{
			StartCoroutine(TransitionView(oldView, newView, 0.5f + Mathf.Abs(Mathf.Log10((float)(double)oldView.distance) - Mathf.Log10((float)(double)newView.distance)) * 0.25f, curve));
			return;
		}
		view.target.Value = newView.target.Value;
		view.position.Value = newView.position;
		view.distance.Value = newView.distance;
	}

	private IEnumerator TransitionView(View oldView, View newView, float transitionTime, AnimationCurve curve)
	{
		float startTime = Time.unscaledTime;
		float endTime = startTime + transitionTime;
		while (endTime > Time.unscaledTime)
		{
			float num = curve.Evaluate(Mathf.InverseLerp(startTime, endTime, Time.unscaledTime));
			View view = View.Lerp(oldView, newView, num);
			this.view.target.Value = view.target.Value;
			this.view.position.Value = view.position;
			this.view.distance.Value = view.distance;
			yield return new WaitForEndOfFrame();
		}
		this.view.target.Value = newView.target.Value;
		this.view.position.Value = newView.position;
		this.view.distance.Value = newView.distance;
	}
}
