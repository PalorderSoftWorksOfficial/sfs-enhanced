using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Audio;
using SFS.Career;
using SFS.Input;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.Builds;

public class PickGridUI : MonoBehaviour
{
	[Serializable]
	public class CategoryParts
	{
		public PickCategory tag;

		public List<(bool owned, VariantRef part)> parts = new List<(bool, VariantRef)>();

		public CategoryParts(PickCategory tag)
		{
			this.tag = tag;
		}
	}

	public GameObject createdPartsHolder;

	public PickCategoriesMenu categoriesMenu;

	public PickGridPositioner positioner;

	[Space]
	public SFS.UI.Button backgroundButton;

	public ScrollElement scrollElement;

	public VerticalLayoutGroup pickIconsHolder;

	public PickGridIcon pickIconPrefab;

	[Space]
	public List<PickCategory> categoryOrder = new List<PickCategory>();

	private List<PickGridIcon> createdIcons = new List<PickGridIcon>();

	private Dictionary<VariantRef, Part> createdParts = new Dictionary<VariantRef, Part>();

	private bool alreadySwitching;

	public OpenTracker partMenuState = new OpenTracker();

	public event Action OnPartMenuOpen;

	private void Start()
	{
		backgroundButton.onHold += (Action<OnInputStayData>)delegate(OnInputStayData a)
		{
			scrollElement.Move(a.delta.deltaPixel / positioner.screenSpaceCanvas.scaleFactor);
		};
		Initialize();
	}

	private void Initialize()
	{
		Dictionary<PickCategory, CategoryParts> dictionary = new Dictionary<PickCategory, CategoryParts>();
		foreach (VariantRef value in Base.partsLoader.partVariants.Values)
		{
			Part part = PartsLoader.CreatePart(value, updateAdaptation: true);
			bool item = part.GetOwnershipState() == OwnershipState.OwnedAndUnlocked && CareerState.main.HasPart(value);
			UnityEngine.Object.DestroyImmediate(part.gameObject);
			foreach (Variants.PickTag pickTag in value.GetPickTags())
			{
				if (pickTag.tag == null)
				{
					throw new Exception(value.part.name);
				}
				if (!categoryOrder.Contains(pickTag.tag))
				{
					categoryOrder.Add(pickTag.tag);
				}
				if (!dictionary.ContainsKey(pickTag.tag))
				{
					dictionary[pickTag.tag] = new CategoryParts(pickTag.tag);
				}
				dictionary[pickTag.tag].parts.Add((item, value));
			}
		}
		dictionary = dictionary.Where((KeyValuePair<PickCategory, CategoryParts> pair) => pair.Value.parts.Any(((bool owned, VariantRef part) a) => a.owned)).ToDictionary((KeyValuePair<PickCategory, CategoryParts> pair) => pair.Key, (KeyValuePair<PickCategory, CategoryParts> pair) => pair.Value);
		foreach (PickCategory category in dictionary.Keys)
		{
			dictionary[category].parts = dictionary[category].parts.OrderBy(((bool owned, VariantRef part) variant) => -variant.part.GetPriority(category)).ToList();
		}
		CategoryParts[] array = dictionary.Values.OrderBy((CategoryParts picklist) => categoryOrder.IndexOf(picklist.tag)).ToArray();
		categoriesMenu.SetupElements(array);
		categoriesMenu.SelectCategory(array[0]);
	}

	public void OpenCategory(CategoryParts newCategory)
	{
		if (!alreadySwitching)
		{
			alreadySwitching = true;
			PickGridIcon[] array = createdIcons.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				UnityEngine.Object.Destroy(array[i].gameObject);
			}
			createdIcons.Clear();
			CreateParts(newCategory);
			scrollElement.PercentPosition = scrollElement.startPivot;
			alreadySwitching = false;
		}
	}

	private void CreateParts(CategoryParts category)
	{
		foreach (var part in category.parts)
		{
			var (flag, partVariant) = part;
			PickGridIcon pickIcon;
			Part createdPart;
			Vector2 startPosition;
			int mode;
			if (flag)
			{
				pickIcon = UnityEngine.Object.Instantiate(pickIconPrefab, pickIconsHolder.transform);
				createdIcons.Add(pickIcon);
				pickIcon.name = "Icon: " + partVariant.part.name;
				pickIcon.gameObject.SetActive(value: true);
				if (!createdParts.TryGetValue(partVariant, out createdPart))
				{
					createdPart = PartsLoader.CreatePart(partVariant, updateAdaptation: true);
					createdParts[partVariant] = createdPart;
					createdPart.transform.parent = createdPartsHolder.transform;
				}
				createdPart.gameObject.SetActive(value: true);
				pickIcon.GetComponent<RawImage>().texture = PartIconCreator.main.CreatePartIcon_PickGrid(createdPart, out var size);
				createdPart.gameObject.SetActive(value: false);
				pickIcon.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, pickIcon.rectTransform.rect.width * (size.y / size.x));
				startPosition = Vector2.zero;
				mode = -1;
				pickIcon.button.onDown += (Action<OnInputStartData>)delegate(OnInputStartData data)
				{
					mode = -1;
					startPosition = data.position.pixel;
				};
				pickIcon.button.onHold += new Action<OnInputStayData>(OnHold);
				pickIcon.button.onUp += new Action<OnInputEndData>(OnUp);
				pickIcon.button.onClick += new Action(ShowPartMenu);
				pickIcon.button.onLongClick += new Action(ShowPartMenu);
				pickIcon.button.onRightClick += new Action(ShowPartMenu);
				pickIcon.button.onScroll += (Action<float>)delegate(float delta)
				{
					scrollElement.Move(new Vector2(0f, delta * 50f));
				};
			}
			void OnHold(OnInputStayData data)
			{
				Vector2 vector = startPosition - data.position.pixel;
				if (mode == -1 && vector.magnitude > 25f)
				{
					if (Mathf.Abs(vector.normalized.y) > 0.95f && scrollElement.FreeMoveSpace.y > 0f)
					{
						mode = 1;
					}
					else
					{
						mode = 0;
						BuildManager.main.holdGrid.TakePart_PickGrid(partVariant, data.position.World(0f));
					}
				}
				if (mode == 1)
				{
					scrollElement.Move(data.delta.deltaPixel / positioner.screenSpaceCanvas.scaleFactor);
				}
				else if (mode == 0)
				{
					BuildManager.main.holdGrid.position.Value += data.delta.DeltaWorld(0f);
				}
			}
			void OnUp(OnInputEndData data)
			{
				if (mode == 0)
				{
					BuildManager.main.holdGrid.OnInputEnd(data);
				}
			}
			void ShowPartMenu()
			{
				RectTransform rectTransform = pickIcon.rectTransform;
				float x = rectTransform.rect.width - rectTransform.rect.width * rectTransform.pivot.x + 10f;
				Func<Vector2> getScreenPosition = AttachWithArrow.FollowTransform(rectTransform, new Vector2(x, 0f));
				partMenuState = BuildManager.main.buildMenus.partMenu.Open_DrawPart(null, new Part[1] { createdPart }, PartDrawSettings.PickGridSettings, getScreenPosition, dontUpdateOnZoomChange: true, skipAnimation: false);
				SoundPlayer.main.pickupSound.Play();
				OnPartMenuOpen?.Invoke();
			}
		}
	}

	public bool IsInsideGrid(Vector2 worldPosition, Vector2 mousePosition)
	{
		if (!IsInside(worldPosition))
		{
			return IsInside(mousePosition);
		}
		return true;
		bool IsInside(Vector2 a)
		{
			Vector3 position = positioner.SetZ_WorldSpace(a, base.transform.position.z);
			Vector3 point = positioner.dropArea.InverseTransformPoint(position);
			return positioner.dropArea.rect.Contains(point);
		}
	}

	private void OnDisable()
	{
		partMenuState.Close();
	}
}
