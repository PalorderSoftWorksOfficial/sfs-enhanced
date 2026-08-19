using System;
using SFS.Cameras;
using SFS.Translations;
using SFS.UI;
using SFS.World.Maps;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.World;

public class VelocityArrowDrawer : MonoBehaviour
{
	[Serializable]
	public class Arrow
	{
		public RectTransform holder;

		public RectTransform holder_Shadow;

		public Image line;

		public Image line_Shadow;

		public Text text;

		public Text text_Shadow;

		public void Position(Func<Color, string> t, float lineLength, Vector2 position, Vector2 directionNormal)
		{
			SetActive(active: true);
			holder.position = position;
			holder_Shadow.localPosition = (Vector2)holder.localPosition + new Vector2(2f, -2f);
			RectTransform rectTransform = holder;
			Quaternion localRotation = (holder_Shadow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(directionNormal.y, directionNormal.x) * 57.29578f));
			rectTransform.localRotation = localRotation;
			RectTransform rectTransform2 = line.rectTransform;
			Vector2 sizeDelta = (line_Shadow.rectTransform.sizeDelta = new Vector2(lineLength, line.rectTransform.sizeDelta.y));
			rectTransform2.sizeDelta = sizeDelta;
			text.text = t(Color.white);
			text_Shadow.text = t(new Color(0f, 0f, 0f, 0.2f));
			RectTransform rectTransform3 = text.rectTransform;
			localRotation = (text_Shadow.rectTransform.rotation = Quaternion.identity);
			rectTransform3.rotation = localRotation;
			RectTransform rectTransform4 = text.rectTransform;
			Vector3 localScale = (text_Shadow.rectTransform.localScale = Vector3.one * 0.25f);
			rectTransform4.localScale = localScale;
			Vector3 localPosition = text.rectTransform.localPosition;
			Vector2 sizeDelta2 = text.rectTransform.sizeDelta;
			Vector2 vector3 = line.rectTransform.InverseTransformPoint(text.rectTransform.TransformPoint(new Vector3((0f - sizeDelta2.x) / 2f, sizeDelta2.y / 2f)));
			Vector2 vector4 = line.rectTransform.InverseTransformPoint(text.rectTransform.TransformPoint(new Vector3(sizeDelta2.x / 2f, sizeDelta2.y / 2f)));
			Vector2 vector5 = line.rectTransform.InverseTransformPoint(text.rectTransform.TransformPoint(new Vector3((0f - sizeDelta2.x) / 2f, (0f - sizeDelta2.y) / 2f)));
			Vector2 vector6 = line.rectTransform.InverseTransformPoint(text.rectTransform.TransformPoint(new Vector3(sizeDelta2.x / 2f, (0f - sizeDelta2.y) / 2f)));
			float num = Mathf.Min(vector3.y, vector4.y, vector5.y, vector6.y);
			RectTransform rectTransform5 = text.rectTransform;
			localScale = (text_Shadow.rectTransform.localPosition = localPosition + new Vector3(0f, 20f - num, 0f));
			rectTransform5.localPosition = localScale;
		}

		public void SetActive(bool active)
		{
			if (holder.gameObject.activeSelf != active)
			{
				holder.gameObject.SetActive(active);
				holder_Shadow.gameObject.SetActive(active);
			}
		}
	}

	public RectTransform safeArea;

	public Arrow velocity_X;

	public Arrow velocity_Y;

	private void Start()
	{
		WorldView main = WorldView.main;
		main.onViewLocationChange_After = (Action<Location, Location>)Delegate.Combine(main.onViewLocationChange_After, new Action<Location, Location>(OnLocationChange));
		PlayerController.main.player.OnChange += new Action(OnPlayerChange);
	}

	private void OnDestroy()
	{
		PlayerController.main.player.OnChange -= new Action(OnPlayerChange);
	}

	private void OnPlayerChange()
	{
		if (!(PlayerController.main.player.Value is Rocket))
		{
			DisableAll();
		}
	}

	private void DisableAll()
	{
		velocity_X.SetActive(active: false);
		velocity_Y.SetActive(active: false);
	}

	private void OnLocationChange(Location _, Location location)
	{
		if (!(PlayerController.main.player.Value is Rocket rocket) || (bool)Map.manager.mapMode)
		{
			DisableAll();
			return;
		}
		float sizeRadius = PlayerController.main.player.Value.GetSizeRadius();
		if ((float)WorldView.main.viewDistance > sizeRadius * 50f + 100f)
		{
			DisableAll();
			return;
		}
		CameraManager world_Camera = GameCamerasManager.main.world_Camera;
		float num = sizeRadius * (world_Camera.camera.WorldToScreenPoint(Vector3.zero) - world_Camera.camera.WorldToScreenPoint(Vector3.right)).magnitude;
		Vector2 origin = world_Camera.camera.WorldToScreenPoint(WorldView.ToLocalPosition(location.position));
		float num2 = 0f - world_Camera.CameraRotationRadians;
		SelectableObject target = Map.navigation.target;
		double velocity;
		double velocitySide;
		string d;
		string v;
		if (target is MapRocket mapRocket && (location.position - target.Location.position).Mag_LessThan(10000.0))
		{
			Double2 @double = location.position - target.Location.position;
			if (@double.Mag_LessThan(rocket.GetSizeRadius() + mapRocket.rocket.GetSizeRadius() + 20f))
			{
				DisableAll();
				return;
			}
			Double2 double2 = location.velocity - target.Location.velocity;
			Double2 double3 = -@double.normalized;
			velocity = double2.magnitude;
			Double2 double4 = double3.Rotate(Math.PI / 2.0).Rotate(num2);
			double num3 = Double2.Dot(double2.normalized, double3) * velocity;
			velocitySide = Double2.Dot(double2.normalized, double4) * velocity;
			if (velocity > 25.0)
			{
				float num4 = GetArrowLength((float)velocity, roundToZero: true) * 6f;
				Double2 double5 = double2.Rotate(num2) / velocity;
				UI_Raycaster.RaycastScreenClamped(origin, double5 * (num + num4 + 30f), safeArea, 10f, out var hitPos);
				velocity_X.Position(GetText, num4, hitPos, double5);
				velocity_Y.SetActive(active: false);
				return;
			}
			float num5 = GetArrowLength((float)Math.Abs(velocitySide), roundToZero: true) * 10f;
			float num6 = GetArrowLength((float)Math.Abs(num3), roundToZero: false) * 7f + 20f;
			if (num5 > 0f)
			{
				UI_Raycaster.RaycastScreenClamped(origin, double4 * Math.Sign(velocitySide) * (num + num5 + 30f), safeArea, 10f, out var hitPos2);
				velocity_Y.Position(GetText2, num5, hitPos2, double4.normalized * Math.Sign(velocitySide));
			}
			else
			{
				velocity_Y.SetActive(active: false);
			}
			UI_Raycaster.RaycastScreenClamped(origin, double3 * (num + num6 + 30f), safeArea, 10f, out var hitPos3);
			d = @double.magnitude.ToDistanceString();
			v = ((num3 > 0.0) ? "+" : "") + num3.ToVelocityString();
			velocity_X.Position(GetText_Forward, num6, hitPos3, double3);
			return;
		}
		bool flag = !double.IsNaN(location.planet.data.basics.velocityArrowsHeight);
		double num7 = (flag ? location.planet.data.basics.velocityArrowsHeight : location.planet.data.basics.timewarpHeight);
		if (location.planet.codeName != Base.planetLoader.spaceCenter.address && (flag ? location.TerrainHeight : location.Height) < num7 && location.VerticalVelocity < 25.0)
		{
			if (rocket.stats.tracker.state_Landed)
			{
				DisableAll();
				return;
			}
			Vector2 velocity2 = location.velocity.Rotate(num2);
			float num8 = GetArrowLength(Mathf.Abs(velocity2.x), roundToZero: true) * 6f;
			float num9 = GetArrowLength(Mathf.Abs(velocity2.y), roundToZero: true) * 6f;
			if (num8 > 0f)
			{
				UI_Raycaster.RaycastScreenClamped(origin, Vector2.right * (Mathf.Sign(velocity2.x) * (num + num8 + 50f)), safeArea, 10f, out var hitPos4);
				velocity_X.Position((Color color) => Math.Abs((double)velocity2.x).ToVelocityString(), num8, hitPos4, Vector2.right * Mathf.Sign(velocity2.x));
			}
			else
			{
				velocity_X.SetActive(active: false);
			}
			if (num9 > 0f)
			{
				UI_Raycaster.RaycastScreenClamped(origin, Vector2.up * (Mathf.Sign(velocity2.y) * (num + num9 + 50f)), safeArea, 10f, out var hitPos5);
				velocity_Y.Position((Color color) => Math.Abs((double)velocity2.y).ToVelocityString(), num9, hitPos5, Vector2.up * Mathf.Sign(velocity2.y));
			}
			else
			{
				velocity_Y.SetActive(active: false);
			}
			return;
		}
		float num10 = (float)location.velocity.magnitude;
		float num11 = GetArrowLength(num10, roundToZero: true) * 3f;
		if (num11 > 0f)
		{
			Double2 double6 = location.velocity.Rotate(num2) / num10;
			UI_Raycaster.RaycastScreenClamped(origin, double6 * 1000.0, safeArea, 10f, out var hitPos6);
			velocity_X.Position((Color color) => "", num11, hitPos6, double6);
		}
		else
		{
			velocity_X.SetActive(active: false);
		}
		velocity_Y.SetActive(active: false);
		static string BigWhite(string s, Color c)
		{
			return WrapSizeAndColor(s, 105f, c);
		}
		string GetText(Color c)
		{
			return SmallGray(Loc.main.Relative_Velocity_Arrow.Inject(BigWhite(velocity.ToVelocityString(), c), "velocity"), c);
		}
		string GetText_Forward(Color c)
		{
			return SmallGray(Loc.main.Forward_Velocity_Arrow.Inject(BigWhite(d, c), "distance").Inject(BigWhite(v, c), "velocity"), c);
		}
		string GetText2(Color c)
		{
			return SmallGray(Loc.main.Side_Velocity_Arrow.Inject(BigWhite(Math.Abs(velocitySide).ToVelocityString(), c), "velocity"), c);
		}
		static string SmallGray(string s, Color c)
		{
			return WrapSizeAndColor(s, 85f, c * new Color(1f, 1f, 1f, 0.7f));
		}
		static string WrapSizeAndColor(string s, float size, Color c)
		{
			return $"<size={size}><color=#{ColorUtility.ToHtmlStringRGBA(c)}>{s}</color></size>";
		}
	}

	private static float GetArrowLength(float velocity, bool roundToZero)
	{
		if ((velocity < 0.1f) & roundToZero)
		{
			return 0f;
		}
		if (velocity < 1f)
		{
			return velocity + 1f;
		}
		return Mathf.Log(velocity, 2f) + 2f;
	}
}
