using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Audio;
using SFS.Input;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.Variables;
using SFS.World;
using UnityEngine;

namespace SFS.Builds;

public class HoldGrid : MonoBehaviour, I_GLDrawer
{
	private class Data
	{
		public float score;

		public Vector2 target;

		public Vector2 anchor;

		public float angleDegrees;

		public Data(Vector2 target, Vector2 anchor, float angleDegrees)
		{
			score = (target - anchor).sqrMagnitude;
			this.target = target;
			this.anchor = anchor;
			this.angleDegrees = angleDegrees;
		}

		public Data(float score, Vector2 target, Vector2 anchor, float angleDegrees)
		{
			this.score = score;
			this.target = target;
			this.anchor = anchor;
			this.angleDegrees = angleDegrees;
		}
	}

	private class GridPointData
	{
		public Line2 line;

		public int checkIndex;
	}

	public PartGrid holdGrid;

	public GameObject blueImportLayer;

	[Space]
	public BuildGrid buildGrid;

	public BuildSelector selector;

	[Space]
	public float magnetIconRadius = 1f;

	public Color magnetIconColor = Color.white;

	[Space]
	public string buildLayer;

	[Space]
	public string holdLayer;

	public RenderSortingManager renderSortingManager;

	public Vector2_Local position;

	private Action takePartsAction;

	public bool importMode;

	public bool forDuplicate;

	private Action<bool> onImportFinished;

	public bool draggingParts;

	private const float SnapDistance = 0.7f;

	private const float SnapDistanceSqrt = 0.48999998f;

	private const float SnapIntervals = 0.25f;

	private const float InterstageIntervals = 0.5f;

	private List<string> GetLayers()
	{
		if (!(renderSortingManager != null))
		{
			return new List<string>();
		}
		return renderSortingManager.layers;
	}

	private void Start()
	{
		GLDrawer.Register(this);
		position.OnChange += new Action(OnPositionChange);
	}

	private void OnDestroy()
	{
		GLDrawer.Unregister(this);
	}

	private void OnPositionChange()
	{
		if (DevSettings.DisableNewBuild)
		{
			holdGrid.transform.position = GetSnapPosition_Old(position.Value);
		}
		else
		{
			GetSnapPosition_New(position.Value);
		}
	}

	public void OnInputStart(OnInputStartData data)
	{
		if (!HasParts(includePreHeld: true))
		{
			SelectForTake_BuildGrid(data.position);
			return;
		}
		if (importMode)
		{
			draggingParts = Part_Utility.RaycastParts(holdGrid.partsHolder.GetArray(), data.position.World(0f), 0.3f, out var _);
		}
		if (draggingParts && forDuplicate)
		{
			importMode = false;
			blueImportLayer.SetActive(value: false);
		}
	}

	public void OnAnotherInputStart()
	{
		if (importMode || forDuplicate)
		{
			draggingParts = false;
		}
		else
		{
			EndHold(destroy: false);
		}
	}

	public void OnInputEnd(OnInputEndData data)
	{
		draggingParts = false;
		Part_Utility.GetBuildColliderBounds_WorldSpace(out var bounds, useLaunchBounds: true, holdGrid.partsHolder.GetArray());
		bool num = BuildManager.main.pickGrid.gameObject.activeInHierarchy && BuildManager.main.pickGrid.IsInsideGrid(bounds.center, data.position.World(0f));
		bool flag = !buildGrid.gridSize.InsideGrid(bounds.center, 0.3f);
		bool flag2 = num | flag;
		if (!importMode || (importMode && (data.click | flag2)))
		{
			EndHold(flag2);
		}
	}

	public void StartImport(Blueprint blueprint, Action<bool> onImportFinished)
	{
		Part[] parts = PartsLoader.CreateParts(blueprint.parts, null, holdGrid.selectedLayer, OnPartNotOwned.Allow, out var _).ToArray();
		if (Part_Utility.GetBuildColliderBounds_WorldSpace(out var bounds, useLaunchBounds: true, parts))
		{
			Part_Utility.OffsetPartPosition(BuildManager.main.buildCamera.cameraManager.position - bounds.center, round: true, parts);
		}
		StartImportOrDuplicate(parts, forDuplicate: false, applyUndo: true, onImportFinished);
	}

	public void StartImportOrDuplicate(Part[] parts, bool forDuplicate, bool applyUndo, Action<bool> onImportFinished)
	{
		position.Value = Vector2.zero;
		int startIndex = holdGrid.partsHolder.parts.Count;
		holdGrid.AddParts(parts);
		if (applyUndo)
		{
			Undo.main.CreateNewStep("Import or duplicate");
			new List<(int, int, int)>();
			Undo.main.RecordPartAction(new Undo.AddPart(parts, parts.Select((Part _, int i) => startIndex + i).ToList(), Undo.GridType.HoldGrid, add: true));
		}
		importMode = true;
		BuildManager.main.buildMenus.UpdateSelectUI();
		this.forDuplicate = forDuplicate;
		this.onImportFinished = onImportFinished;
		blueImportLayer.SetActive(value: true);
	}

	public void TakePart_PickGrid(VariantRef hit, Vector2 worldPickupPosition)
	{
		EndHold(destroy: false);
		position.Value = worldPickupPosition + Vector2.zero;
		Part part = PartsLoader.CreatePart(hit, updateAdaptation: false);
		holdGrid.AddParts(part);
		part.transform.position = Vector3.zero;
		part.transform.localPosition = (Part_Utility.GetBuildColliderBounds_WorldSpace(out var bounds, true, part) ? new Vector2(0f - bounds.center.x, 0f - bounds.center.y).Round(0.5f) : Vector2.zero);
		Undo.main.CreateNewStep("Take from pick");
		new List<(int, int, int)>();
		Undo.main.RecordPartAction(new Undo.AddPart(new Part[1] { part }, new List<int> { holdGrid.partsHolder.parts.IndexOf(part) }, Undo.GridType.HoldGrid, add: true));
	}

	private void SelectForTake_BuildGrid(TouchPosition touchPosition)
	{
		if (buildGrid.RaycastParts(touchPosition.World(0f), out var hit))
		{
			position.Value = buildGrid.transform.position;
			Part[] parts = selector.GetGroup(hit.part, out var tookSelection).ToArray();
			takePartsAction = delegate
			{
				TakeParts_BuildGrid(parts, tookSelection);
			};
			draggingParts = true;
		}
	}

	private void TakeParts_BuildGrid(Part[] parts, bool tookSelection)
	{
		Undo.main.CreateNewStep("Take from grid");
		buildGrid.RemoveParts(enableNonIntersecting: true, applyUndo: true, parts);
		if (BuildManager.main.symmetryMode)
		{
			Part[] mirrorParts = buildGrid.GetMirrorParts(parts);
			Part[] array = mirrorParts;
			foreach (Part part in array)
			{
				part.aboutToDestroy?.Invoke(part);
			}
			buildGrid.RemoveParts(enableNonIntersecting: true, applyUndo: true, mirrorParts);
			selector.Deselect(mirrorParts);
			array = mirrorParts;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].DestroyPart(createExplosion: false, updateJoints: false, DestructionReason.Intentional);
			}
		}
		Vector2 value = position.Value;
		position.Value = buildGrid.transform.position;
		int startIndex = holdGrid.partsHolder.parts.Count;
		holdGrid.AddParts(parts);
		position.Value = value;
		Undo.main.RecordPartAction(new Undo.AddPart(parts, parts.Select((Part _, int num) => startIndex + num).ToList(), Undo.GridType.HoldGrid, add: true));
		if (tookSelection)
		{
			BuildManager.main.buildMenus.attachedPartMenuState.Close();
		}
	}

	public bool ConfirmTakeParts_BuildGrid()
	{
		bool num = takePartsAction != null;
		if (num)
		{
			takePartsAction();
			takePartsAction = null;
			SoundPlayer.main.pickupSound.Play();
		}
		return num;
	}

	public void EndHold(bool destroy)
	{
		Part[] array = holdGrid.partsHolder.GetArray();
		if (!destroy && array.Length != 0)
		{
			OnPositionChange();
		}
		if (destroy && array.Length != 0)
		{
			Part[] array2 = array;
			foreach (Part part in array2)
			{
				part.aboutToDestroy?.Invoke(part);
			}
		}
		Undo.main.RecordPartAction(new Undo.AddPart(array, array.Select((Part _, int result) => result).ToList(), Undo.GridType.HoldGrid, add: false));
		takePartsAction = null;
		importMode = false;
		forDuplicate = false;
		blueImportLayer.SetActive(value: false);
		onImportFinished?.Invoke(!destroy);
		onImportFinished = null;
		draggingParts = false;
		holdGrid.RemoveParts(array);
		if (!destroy && array.Length != 0)
		{
			buildGrid.AddParts(active: true, clamp: true, applyUndo: true, array);
			SoundPlayer.main.dropSound.Play();
			if (BuildManager.main.symmetryMode && ((Part_Utility.GetBuildColliderBounds_WorldSpace(out var bounds, useLaunchBounds: false, array) && (!(bounds.xMax > buildGrid.gridSize.centerX) || !(bounds.xMin < buildGrid.gridSize.centerX))) || SandboxSettings.main.settings.partClipping))
			{
				Part[] parts = PartsLoader.DuplicateParts(buildGrid.activeGrid.selectedLayer, array);
				Part_Utility.ApplyOrientationChange(new Orientation(-1f, 1f, 0f), new Vector2(buildGrid.gridSize.centerX, 0f), parts);
				buildGrid.AddParts(active: true, clamp: true, applyUndo: true, parts);
			}
			Part[] array2 = array;
			foreach (Part part2 in array2)
			{
				part2.orientation.orientation.Value.z = part2.transform.localEulerAngles.z;
			}
		}
		if (destroy && array.Length != 0)
		{
			selector.Deselect(array);
			array.ForEach(delegate(Part p)
			{
				p.DestroyPart(createExplosion: false, updateJoints: false, DestructionReason.Intentional);
			});
			BuildManager.main.destroyPartSound.Play();
		}
	}

	public bool HasParts(bool includePreHeld)
	{
		if (!includePreHeld || takePartsAction == null)
		{
			return holdGrid.partsHolder.parts.Count > 0;
		}
		return true;
	}

	private Vector2 GetSnapPosition_Old(Vector2 position)
	{
		holdGrid.transform.position = position;
		ConvexPolygon[] buildColliders = buildGrid.buildColliders.Select((BuildGrid.PartCollider a) => a.colliders).Collapse().ToArray();
		MagnetModule[] modules = holdGrid.partsHolder.GetModules<MagnetModule>();
		MagnetModule[] modules2 = buildGrid.activeGrid.partsHolder.GetModules<MagnetModule>();
		if (modules.Length <= 8)
		{
			List<Vector2> allSnapOffsets = MagnetModule.GetAllSnapOffsets(modules, modules2, 0.75f);
			if (allSnapOffsets.Count > 0)
			{
				allSnapOffsets.Sort((Vector2 a, Vector2 b) => a.sqrMagnitude.CompareTo(b.sqrMagnitude));
				foreach (Vector2 item in allSnapOffsets)
				{
					if (!TestCollision_Old(position + item, buildColliders))
					{
						return position + item;
					}
				}
			}
		}
		if (SandboxSettings.main.settings.partClipping)
		{
			return position.Round(0.5f);
		}
		if (!TestCollision_Old(position.Round(0.5f), buildColliders))
		{
			return position.Round(0.5f);
		}
		List<Vector2> obj = new List<Vector2>
		{
			(position + Vector2.left * 0.4f).Round(0.5f),
			(position + Vector2.right * 0.4f).Round(0.5f),
			(position + Vector2.up * 0.4f).Round(0.5f),
			(position + Vector2.down * 0.4f).Round(0.5f)
		};
		Vector2 result = position;
		float num = float.PositiveInfinity;
		foreach (Vector2 item2 in obj)
		{
			if ((item2 - position).sqrMagnitude < num && !TestCollision_Old(item2, buildColliders))
			{
				result = item2;
				num = (item2 - position).sqrMagnitude;
			}
		}
		if (num < float.PositiveInfinity)
		{
			return result;
		}
		return position.Round(0.5f);
	}

	private bool TestCollision_Old(Vector2 position, ConvexPolygon[] buildColliders)
	{
		Vector2 vector = holdGrid.transform.position;
		holdGrid.transform.position = position;
		bool result = Polygon.Intersect(Part_Utility.GetBuildColliderPolygons(holdGrid.partsHolder.GetArray()).normal, buildColliders, -0.08f);
		holdGrid.transform.position = vector;
		return result;
	}

	void I_GLDrawer.Draw()
	{
		float buildDepth;
		if (DevSettings.DisableNewBuild && holdGrid.partsHolder.parts.Count > 0)
		{
			buildDepth = renderSortingManager.GetGlobalDepth(0f, buildLayer);
			renderSortingManager.GetGlobalDepth(0f, holdLayer);
			if (!importMode && holdGrid.partsHolder.HasModule<MagnetModule>())
			{
				DrawMagnets(buildGrid.activeGrid.partsHolder);
				DrawMagnets(holdGrid.partsHolder);
			}
		}
		void DrawMagnets(PartHolder a)
		{
			List<Vector2> list = new List<Vector2>();
			MagnetModule[] modules = a.GetModules<MagnetModule>();
			foreach (MagnetModule magnetModule in modules)
			{
				MagnetModule.Point[] points = magnetModule.points;
				foreach (MagnetModule.Point point in points)
				{
					if (!point.occupied)
					{
						list.Add(magnetModule.transform.TransformPoint(point.position.Value));
					}
				}
			}
			GLDrawer.DrawCircles(list, magnetIconRadius, (list.Count > 100) ? 8 : 12, magnetIconColor, buildDepth);
		}
	}

	private void Update()
	{
		if (!DevSettings.DisableNewBuild && Application.isEditor)
		{
			GetSnapPosition_New(position.Value);
		}
	}

	private void GetSnapPosition_New(Vector2 position)
	{
		Position(new Data(0f, position, base.transform.position, 0f));
		List<Data> list = new List<Data>();
		CollectSurfaceSnaps(list);
		CollectInterstageSnaps(list);
		if (list.Count != 0)
		{
			list.Sort((Data a, Data b) => a.score.CompareTo(b.score));
			Part[] array = buildGrid.activeGrid.partsHolder.GetArray();
			ConvexPolygon[] colliders_Build = Part_Utility.GetBuildColliderPolygons(array).normal;
			Data data = list.FirstOrDefault((Data move) => !TestCollision(move, colliders_Build));
			if (data != null)
			{
				Position(data);
			}
		}
	}

	private void CollectInterstageSnaps(List<Data> moves)
	{
		Collect(reverse: false, holdGrid.partsHolder.GetModules<AdaptModule>(), buildGrid.activeGrid.partsHolder.GetModules<AdaptTriggerModule>());
		Collect(reverse: true, buildGrid.activeGrid.partsHolder.GetModules<AdaptModule>(), holdGrid.partsHolder.GetModules<AdaptTriggerModule>());
		void Collect(bool reverse, AdaptModule[] adaptors, AdaptTriggerModule[] triggers)
		{
			foreach (AdaptModule adaptModule in adaptors)
			{
				AdaptModule.Point[] adaptPoints = adaptModule.adaptPoints;
				foreach (AdaptModule.Point point in adaptPoints)
				{
					if (point.reciverType == AdaptModule.ReceiverType.Area)
					{
						Rect value = point.inputArea.Value;
						int num = Mathf.RoundToInt(value.height / 0.5f);
						for (int k = 0; k < num; k++)
						{
							Vector3 vector = new Vector3(value.center.x, value.yMin + 0.5f * (float)k);
							Vector2 vector2 = adaptModule.transform.TransformPoint(vector);
							Vector2 vector3 = adaptModule.transform.TransformVectorUnscaled(point.normal);
							foreach (AdaptTriggerModule adaptTriggerModule in triggers)
							{
								AdaptTriggerPoint[] points = adaptTriggerModule.points;
								foreach (AdaptTriggerPoint adaptTriggerPoint in points)
								{
									Vector2 vector4 = adaptTriggerModule.transform.TransformPoint(point.position.Value);
									Vector2 vector5 = -adaptTriggerModule.transform.TransformVectorUnscaled(adaptTriggerPoint.normal);
									float sqrMagnitude = ((vector2 - vector4) * 0.8f).sqrMagnitude;
									if (!(sqrMagnitude > 0.48999998f) && !(Vector2.Dot(vector3, vector5) < 0.9f))
									{
										float num2 = vector5.AngleDegrees() - vector3.AngleDegrees();
										moves.Add(new Data(sqrMagnitude, reverse ? vector2 : vector4, reverse ? vector4 : vector2, reverse ? (0f - num2) : num2));
									}
								}
							}
						}
					}
				}
			}
		}
	}

	private void CollectSurfaceSnaps(List<Data> moves)
	{
		Dictionary<Vector2Int, List<GridPointData>> collisionsDictionary = GetCollisionsDictionary((from a in buildGrid.activeGrid.partsHolder.GetModules<SurfaceData>()
			where a.Attachment
			select a).ToArray(), 0f, 2f);
		List<Surfaces> list = (from s in (from a in holdGrid.partsHolder.GetModules<SurfaceData>()
				where a.Attachment
				select a).ToArray()
			select s.surfaces).Collapse();
		int num = 0;
		foreach (Surfaces item in list)
		{
			Line2[] surfacesWorld = item.GetSurfacesWorld();
			for (int num2 = 0; num2 < surfacesWorld.Length; num2++)
			{
				Line2 line = surfacesWorld[num2];
				Vector2Int[] colliderCoordinates = GetColliderCoordinates(line, 1.4f, 2f);
				num++;
				line.Flip();
				Vector2Int[] array = colliderCoordinates;
				foreach (Vector2Int key in array)
				{
					if (!collisionsDictionary.ContainsKey(key))
					{
						continue;
					}
					foreach (GridPointData item2 in collisionsDictionary[key])
					{
						if (item2.checkIndex != num)
						{
							ProcessSurfaceSnap(moves, item2.line, line);
							item2.checkIndex = num;
						}
					}
				}
			}
		}
	}

	private static void ProcessSurfaceSnap(List<Data> moves, Line2 line_Build, Line2 line_Hold)
	{
		Debug.DrawLine(line_Hold.start, line_Hold.end, Color.magenta);
		Line hold;
		if (!(Vector2.Dot(line_Build.Size.normalized, line_Hold.Size.normalized) < 0.9f))
		{
			float magnitude = line_Build.Size.magnitude;
			Line line = new Line(0f, magnitude);
			hold = new Line(Math_Utility.GetClosestPointOnLine(line_Build.start, line_Build.end, line_Hold.start) * magnitude, Math_Utility.GetClosestPointOnLine(line_Build.start, line_Build.end, line_Hold.end) * magnitude);
			Line line2 = new Line(Mathf.Max(hold.start, line.start), Mathf.Min(hold.end, line.end));
			float num = line.Center - hold.Center;
			if (line2.Size > 0.125f && !((GetPosOnBuildLine(line2.Center) - GetPosOnHoldLine(line2.Center)).sqrMagnitude > 0.48999998f))
			{
				bool flag = num > 0f;
				bool flag2 = line_Hold.Size.magnitude > magnitude;
				bool num2 = (flag2 ? (!flag) : flag);
				float num3 = (num2 ? (hold.start - line.start) : (hold.end - line.end));
				float b = ((flag2 ? line : hold).Size - 0.15f).Round(0.25f);
				float num4 = ((num2 == flag2) ? 1 : (-1));
				num3 = Mathf.Min(num3 * num4, b) * num4;
				Debug.Log(num3 + num3.Round(0.25f));
				Vector2 vector = (num2 ? line_Build.start : line_Build.end) + line_Build.Size.normalized * num3.Round(0.25f);
				Vector2 vector2 = (num2 ? line_Hold.start : line_Hold.end);
				moves.Add(new Data(vector, vector2, GetAngle()));
				Utility.DrawArrow(vector2, vector, Color.cyan, 10f);
				Debug.DrawLine(line_Hold.start, line_Hold.end, Color.magenta);
			}
		}
		float GetAngle()
		{
			return line_Build.Size.AngleDegrees() - line_Hold.Size.AngleDegrees();
		}
		Vector2 GetPosOnBuildLine(float x)
		{
			return line_Build.start + line_Build.Size.normalized * x;
		}
		Vector2 GetPosOnHoldLine(float x)
		{
			return line_Hold.LerpUnclamped(Math_Utility.InverseLerpUnclamped(hold.start, hold.end, x));
		}
	}

	private bool TestCollision(Data data, ConvexPolygon[] buildColliders)
	{
		Vector2 vector = holdGrid.transform.position;
		float z = holdGrid.transform.eulerAngles.z;
		Position(data);
		bool result = Polygon.Intersect(Part_Utility.GetBuildColliderPolygons(holdGrid.partsHolder.GetArray()).normal, buildColliders, -0.08f);
		holdGrid.transform.position = vector;
		holdGrid.transform.eulerAngles = new Vector3(0f, 0f, z);
		return result;
	}

	private void Position(Data data)
	{
		Vector2 vector = holdGrid.transform.InverseTransformPoint(data.anchor);
		holdGrid.transform.eulerAngles = new Vector3(0f, 0f, data.angleDegrees);
		Vector2 vector2 = data.target - (Vector2)holdGrid.transform.TransformPoint(vector);
		holdGrid.transform.position += (Vector3)vector2;
	}

	private static Dictionary<Vector2Int, List<GridPointData>> GetCollisionsDictionary(SurfaceData[] surfaces, float margin, float gridSize)
	{
		Dictionary<Vector2Int, List<GridPointData>> dictionary = new Dictionary<Vector2Int, List<GridPointData>>();
		for (int i = 0; i < surfaces.Length; i++)
		{
			foreach (Surfaces surface in surfaces[i].surfaces)
			{
				Line2[] surfacesWorld = surface.GetSurfacesWorld();
				foreach (Line2 line in surfacesWorld)
				{
					GridPointData item = new GridPointData
					{
						checkIndex = -1,
						line = line
					};
					Vector2Int[] colliderCoordinates = GetColliderCoordinates(line, margin, gridSize);
					foreach (Vector2Int key in colliderCoordinates)
					{
						if (!dictionary.ContainsKey(key))
						{
							dictionary.Add(key, new List<GridPointData>());
						}
						dictionary[key].Add(item);
					}
				}
			}
		}
		return dictionary;
	}

	private static Vector2Int[] GetColliderCoordinates(Line2 a, float margin, float gridSize)
	{
		Vector2 vector = (new Vector2(Mathf.Min(a.start.x, a.end.x), Mathf.Min(a.start.y, a.end.y)) - Vector2.one * (margin * 0.5f)) / gridSize;
		Vector2 vector2 = (new Vector2(Mathf.Max(a.start.x, a.end.x), Mathf.Max(a.start.y, a.end.y)) + Vector2.one * (margin * 0.5f)) / gridSize;
		Vector2Int vector2Int = new Vector2Int((int)vector.x, (int)vector.y);
		Vector2Int vector2Int2 = new Vector2Int((int)vector2.x, (int)vector2.y);
		int num = vector2Int2.x - vector2Int.x + 1;
		int num2 = vector2Int2.y - vector2Int.y + 1;
		Vector2Int[] array = new Vector2Int[num * num2];
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num2; j++)
			{
				array[i * num2 + j] = new Vector2Int(vector2Int.x + i, vector2Int.y + j);
			}
		}
		return array;
	}
}
