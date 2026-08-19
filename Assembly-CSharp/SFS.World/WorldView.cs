using System;
using SFS.Cameras;
using SFS.Variables;
using SFS.World.PlanetModules;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World;

public class WorldView : MonoBehaviour
{
	public static WorldView main;

	public const float ScaledSpaceScale = 10000f;

	private const float ScaledSpaceThreshold = 50000f;

	public Vector2_Local framing = new Vector2_Local
	{
		Value = new Vector2(0.5f, 0.5f)
	};

	private Vector2 framingVelocity;

	public Float_Local viewDistance;

	[Space]
	public Bool_Local scaledSpace;

	public Bool_Local canVelocityOffset;

	[Space]
	public Double2_Local positionOffset;

	public Double2_Local velocityOffset;

	public CameraManager worldCamera;

	public AudioListener audioListener;

	public Action<Location, Location> onViewLocationChange_Before;

	public Action<Location, Location> onViewLocationChange_After;

	public Action<Vector2> onPositionOffset;

	public Action<Vector2> onVelocityOffset;

	public RectTransform mapTransform;

	public Action onUpdate;

	public PostProcessing[] postProcessing;

	public Location ViewLocation { get; private set; } = new Location(null, Double2.zero);

	private void Awake()
	{
		main = this;
	}

	public static Double2 ToGlobalPosition(Vector2 localPosition)
	{
		return main.positionOffset.Value + localPosition;
	}

	public static Double2 ToGlobalVelocity(Vector2 localVelocity)
	{
		return main.velocityOffset.Value + localVelocity;
	}

	public static Vector2 ToLocalPosition(Double2 globalPosition)
	{
		return globalPosition - main.positionOffset;
	}

	public static Vector2 ToLocalVelocity(Double2 globalVelocity)
	{
		return globalVelocity - main.velocityOffset;
	}

	private void Start()
	{
		viewDistance.OnChange += new Action(UpdateIsScaledSpace);
		viewDistance.OnChange += new Action(PositionCamera);
		framing.OnChange += new Action(PositionCamera);
		canVelocityOffset.OnChange += new Action(CalculateVelocityOffset);
	}

	private void UpdateIsScaledSpace()
	{
		scaledSpace.Value = (float)viewDistance > 50000f;
	}

	private void PositionCamera()
	{
		if (!scaledSpace.Value)
		{
			Vector2 vector = ToLocalPosition(ViewLocation.position);
			Vector2 value = framing.Value;
			value = Vector2.Lerp(value, new Vector2(0.5f, 0.5f), Mathf.InverseLerp(Mathf.Log(12f), Mathf.Log(50000f), Mathf.Log(viewDistance.Value)));
			(Vector2, Vector2) framingData = worldCamera.GetFramingData();
			Vector2 item = framingData.Item1;
			Vector2 item2 = framingData.Item2;
			Vector2 vector2 = (item * (value.x - 0.5f) + item2 * (value.y - 0.5f)) * viewDistance.Value;
			worldCamera.position.Value = vector + vector2;
			worldCamera.distance.Value = viewDistance;
			audioListener.transform.position = new Vector3(vector.x, vector.y, 0f - (float)viewDistance);
		}
	}

	public void SetViewLocation(Location newValue)
	{
		Location viewLocation = ViewLocation;
		onViewLocationChange_Before?.Invoke(viewLocation, newValue);
		ViewLocation = newValue;
		CalculatePositionOffset();
		CalculateVelocityOffset();
		PositionCamera();
		onViewLocationChange_After?.Invoke(viewLocation, newValue);
	}

	private void CalculatePositionOffset()
	{
		Double2 offset = GetOffset((Double2)ToLocalPosition(ViewLocation.position), 1000.0);
		if (!(offset.sqrMagnitude < 0.01))
		{
			positionOffset.Value += offset;
			onPositionOffset?.Invoke(-offset);
		}
	}

	private void CalculateVelocityOffset()
	{
		Double2 @double = (canVelocityOffset ? GetOffset((Double2)ToLocalVelocity(ViewLocation.velocity), 4.0) : (-velocityOffset.Value));
		if (@double.sqrMagnitude < 0.01)
		{
			return;
		}
		velocityOffset.Value += @double;
		onVelocityOffset?.Invoke(-@double);
		if ((bool)WorldTime.main.realtimePhysics)
		{
			worldCamera.position.Value -= @double.ToVector2 * Time.fixedDeltaTime;
			if (audioListener != null)
			{
				audioListener.transform.position -= (Vector3)@double.ToVector2 * Time.fixedDeltaTime;
			}
		}
	}

	public static Double2 GetOffset(Double2 a, double b)
	{
		return new Double2(Math.Round(a.x / b) * b, Math.Round(a.y / b) * b);
	}

	private void FixedUpdate()
	{
		if (velocityOffset.Value.sqrMagnitude > 0.0 && (bool)WorldTime.main.realtimePhysics)
		{
			positionOffset.Value += velocityOffset.Value * Time.fixedDeltaTime;
		}
	}

	private void Update()
	{
		onUpdate?.Invoke();
		UpdatePostProcessing();
		if (GameManager.main == null)
		{
			return;
		}
		Player value = PlayerController.main.player.Value;
		float target = 0.5f;
		if (!(value is Rocket))
		{
			if (!(value is Astronaut_EVA))
			{
				if (value is Flag)
				{
					target = 0.42f;
				}
			}
			else
			{
				target = Mathf.Lerp(0.42f, 0.5f, Mathf.InverseLerp(0f, 100f, (float)value.location.Value.TerrainHeight));
			}
		}
		float smoothTime = Mathf.Min(PlayerController.main.switchOffsetSmoothTime, 1f);
		float y = Mathf.SmoothDamp(framing.Value.y, target, ref framingVelocity.y, smoothTime);
		framing.Value = new Vector2(0.5f, y);
	}

	private void UpdatePostProcessing()
	{
		PlanetData data = ViewLocation.planet.data;
		float num = (float)ViewLocation.Height;
		PostProcessingModule.Key ambient = (data.hasPostProcessing ? data.postProcessing.Evaluate(num + viewDistance.Value) : new PostProcessingModule.Key(0f, 1.75f, 1f, 0f, 1f, 1.1f, 1f, 1f, 1f));
		float maxStarIntensity = 1f;
		if (Base.planetLoader.solarSystemSettings.hideStarsInAtmosphere && data.hasAtmosphereVisuals)
		{
			float value = (num + viewDistance.Value / 2500f) / (float)data.atmosphereVisuals.GRADIENT.height;
			maxStarIntensity = Mathf.InverseLerp(1.2f, 1.8f, value);
		}
		PostProcessing[] array = postProcessing;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetAmbient(ambient, maxStarIntensity);
		}
	}
}
