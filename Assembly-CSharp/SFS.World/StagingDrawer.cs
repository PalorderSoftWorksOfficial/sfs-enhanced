using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Audio;
using SFS.Builds;
using SFS.Input;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.UI;
using SFS.Variables;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.World;

public class StagingDrawer : MonoBehaviour, I_GLDrawer
{
	public static StagingDrawer main;

	public SFS.UI.Button addStageButton_Attached;

	public SFS.UI.Button addStageButton_Fixed;

	public SFS.UI.Button[] useStageButtons;

	public ScrollElement scroller;

	[Space]
	public GameObject menuHolder;

	public VerticalLayoutGroup stagesGroup;

	[Space]
	public RectTransform stagesWindow;

	public RectTransform stagesHolder;

	[Space]
	public ReorderingGroup reorderingGroup;

	public StageUI stageUIPrefab;

	[Space]
	public Color outlineColor;

	public float outlineWidth;

	public Bool_Local shown = new Bool_Local();

	private Staging_Local staging = new Staging_Local();

	private Bool_Local scrolling = new Bool_Local();

	private List<StageUI> stagesUI = new List<StageUI>();

	private StageUI selected;

	private static int useStageIdentifier;

	public bool HasStages
	{
		get
		{
			if (staging.Value != null)
			{
				return staging.Value.stages.Count > 0;
			}
			return false;
		}
	}

	private void Awake()
	{
		main = this;
	}

	public void Initialize_Build()
	{
		staging.Value = BuildManager.main.buildMenus.staging;
		Initialize();
	}

	public void Initialize_Game()
	{
		PlayerController.main.player.OnChange += new Action<Player>(OnPlayerChange);
		PlayerController.main.hasControl.OnChange += new Action(OnHasControlChange);
		WorldTime.main.realtimePhysics.OnChange += new Action(UpdateShow);
		useStageButtons.ForEach(delegate(SFS.UI.Button a)
		{
			a.onClick += new Action(UseFirstStage);
		});
		GameManager.AddOnKeyDown(KeybindingsPC.keys.Activate_Stage, UseFirstStage);
		Initialize();
		void UseFirstStage()
		{
			if (staging.Value != null && staging.Value.stages.Count > 0)
			{
				UseStage(staging.Value.stages.First());
			}
		}
	}

	private void Initialize()
	{
		stageUIPrefab.gameObject.SetActive(value: false);
		staging.OnChange += new Action<Staging, Staging>(OnStagingChange);
		scrolling.OnChange += new Action(OnScrollingModeChange);
		staging.OnChange += new Action(UpdateShow);
		shown.OnChange += new Action(UpdateShow);
		ReorderingGroup obj = reorderingGroup;
		obj.onReorder = (Action<ReorderingModule>)Delegate.Combine(obj.onReorder, (Action<ReorderingModule>)delegate
		{
			OnReorder();
		});
		addStageButton_Fixed.onClick += new Action(AddStage);
		addStageButton_Attached.onClick += new Action(AddStage);
		GLDrawer.Register(this);
	}

	private void OnDestroy()
	{
		GLDrawer.Unregister(this);
	}

	private void OnPlayerChange(Player player)
	{
		staging.Value = ((player is Rocket rocket) ? rocket.staging : null);
	}

	private void OnStagingChange(Staging oldValue, Staging newValue)
	{
		if (oldValue != null)
		{
			ClearStages_UI();
			oldValue.onStageAdded = (Action<Stage, int>)Delegate.Remove(oldValue.onStageAdded, new Action<Stage, int>(CreateStage_UI));
			oldValue.onStageRemoved = (Action<Stage>)Delegate.Remove(oldValue.onStageRemoved, new Action<Stage>(DestroyStage_UI));
		}
		if (!(newValue != null))
		{
			return;
		}
		foreach (Stage stage in newValue.stages)
		{
			CreateStage_UI(stage, -1);
		}
		newValue.onStageAdded = (Action<Stage, int>)Delegate.Combine(newValue.onStageAdded, new Action<Stage, int>(CreateStage_UI));
		newValue.onStageRemoved = (Action<Stage>)Delegate.Combine(newValue.onStageRemoved, new Action<Stage>(DestroyStage_UI));
	}

	private void UpdateShow()
	{
		if (!Base.sceneLoader.isUnloading)
		{
			menuHolder.SetActive(staging.Value != null && shown.Value && (BuildManager.main != null || WorldTime.main.realtimePhysics.Value || true));
		}
	}

	private void OnHasControlChange()
	{
		bool hasControl = PlayerController.main.hasControl.Value;
		addStageButton_Fixed.SetEnabled(hasControl);
		addStageButton_Attached.SetEnabled(hasControl);
		useStageButtons.ForEach(delegate(SFS.UI.Button a)
		{
			a.SetEnabled(hasControl);
		});
	}

	private void OnReorder()
	{
		if (!(staging.Value == null))
		{
			Dictionary<Stage, int> order = stagesUI.ToDictionary((StageUI ui) => ui.stage, (StageUI ui) => -ui.transform.GetSiblingIndex());
			stagesUI.Sort((StageUI a, StageUI b) => order[a.stage].CompareTo(order[b.stage]));
			staging.Value.ApplyReorder(order);
			RefreshStageId();
		}
	}

	private void OnScrollingModeChange()
	{
		addStageButton_Attached.gameObject.SetActive(!scrolling.Value);
		addStageButton_Fixed.gameObject.SetActive(scrolling.Value);
		float num = (scrolling.Value ? 63 : 0);
		stagesWindow.offsetMax = new Vector2(stagesWindow.offsetMax.x, 0f - num);
		if (!scrolling.Value)
		{
			scroller.GetRect().anchoredPosition = Vector2.zero;
		}
	}

	private void LateUpdate()
	{
		UpdateIsScrolling(0f);
	}

	private void UpdateIsScrolling(float extraHeight)
	{
		scrolling.Value = stagesHolder.rect.height + extraHeight > stagesWindow.rect.height;
	}

	private void ClampToTop(float extraHeight)
	{
		if (stagesHolder.localPosition.y + (stagesHolder.rect.height + extraHeight) < stagesWindow.rect.height)
		{
			MoveToTop(extraHeight);
		}
	}

	private void MoveToTop(float extraHeight)
	{
		stagesHolder.localPosition = new Vector3(stagesHolder.localPosition.x, stagesWindow.rect.height - (stagesHolder.rect.height + extraHeight));
	}

	public void ToggleEditMode()
	{
		staging.Value.editMode.Value = !staging.Value.editMode.Value;
	}

	private void AddStage()
	{
		if (!(GameManager.main != null) || PlayerController.main.HasControl(MsgDrawer.main))
		{
			if (BuildManager.main != null)
			{
				Undo.main.CreateNewStep("Add Stage");
			}
			staging.Value.InsertStage(new Stage((staging.Value.stages.Count <= 0) ? 1 : (staging.Value.stages.Max((Stage s) => s.stageId) + 1), new List<Part>()), BuildManager.main != null);
			SetSelected(stagesUI.Last());
			float num = stagesUI.Last().GetRect().rect.height + stagesGroup.spacing;
			bool flag = !scrolling.Value;
			UpdateIsScrolling(num);
			if (scrolling.Value)
			{
				MoveToTop(num - (flag ? (addStageButton_Attached.GetRect().rect.height + stagesGroup.spacing) : 0f));
			}
		}
	}

	private void UseStage(Stage stageToUse)
	{
		if ((GameManager.main != null && !PlayerController.main.HasControl(MsgDrawer.main)) || !WorldTime.main.realtimePhysics.Value || this.staging.Value == null)
		{
			return;
		}
		useStageIdentifier++;
		stageToUse.useStageIdentifier = useStageIdentifier;
		Part[] array = stageToUse.parts.ToArray();
		UsePartData[] array2 = Rocket.UseParts(fromStaging: true, array.Select((Part a) => ((Part a, PolygonData))(a: a, null)).ToArray());
		for (int num = 0; num < array.Length; num++)
		{
			if (!array2[num].successfullyUsedPart || !(array[num] != null))
			{
				continue;
			}
			Staging staging = array[num].Rocket.staging;
			for (int num2 = staging.stages.Count - 1; num2 >= 0; num2--)
			{
				Stage stage = staging.stages[num2];
				if (stage.useStageIdentifier == useStageIdentifier)
				{
					stage.RemovePart(array[num], record: false);
					if (stageToUse.PartCount == 0)
					{
						staging.RemoveStage(stageToUse, record: false);
					}
				}
			}
		}
		if (array.Length == 0)
		{
			this.staging.Value.RemoveStage(stageToUse, record: false);
		}
		PopupManager.MarkUsed(PopupManager.Feature.Stages);
	}

	private void DuplicateStage(StageUI a)
	{
		if (BuildManager.main != null)
		{
			Undo.main.CreateNewStep("Duplicate Stage");
		}
		Stage a2 = new Stage(-1, a.stage.parts.ToList());
		staging.Value.InsertStage(a2, BuildManager.main != null, stagesUI.IndexOf(a) + 1);
	}

	private void RemoveStage(StageUI a)
	{
		if (BuildManager.main != null)
		{
			Undo.main.CreateNewStep("Remove Stage");
		}
		if (staging.Value != null)
		{
			staging.Value.RemoveStage(a.stage, BuildManager.main != null);
		}
	}

	private void ToggleStageSelect(StageUI a)
	{
		if (!(GameManager.main != null) || PlayerController.main.HasControl(MsgDrawer.main))
		{
			if (IsSelectedStage(a))
			{
				DeselectStage(a);
			}
			else
			{
				SetSelected(a);
			}
		}
	}

	private void DeselectStage(StageUI a)
	{
		if (IsSelectedStage(a))
		{
			SetSelected(null);
		}
	}

	public void SetSelected(StageUI a)
	{
		if (selected != null)
		{
			selected.selectedMark.SetActive(value: false);
		}
		selected = a;
		if (selected != null)
		{
			selected.selectedMark.SetActive(value: true);
		}
		if (BuildManager.main != null)
		{
			BuildManager.main.buildMenus.selector.DeselectAll();
		}
	}

	private bool IsSelectedStage(StageUI a)
	{
		if (HasStageSelected())
		{
			return selected == a;
		}
		return false;
	}

	public bool HasStageSelected()
	{
		return selected != null;
	}

	public bool IsPartSelected(Part part)
	{
		if (HasStageSelected())
		{
			return selected.stage.parts.Contains(part);
		}
		return false;
	}

	public void TogglePartSelected(Part part, bool playSound, bool createNewStep)
	{
		if (HasStageSelected() && (CanStagePart(part, playSound) || selected.stage.parts.Select((Part x) => x).Contains(part)))
		{
			selected.stage.ToggleSelected(part, createNewStep);
			SoundPlayer.main.pickupSound.Play();
		}
	}

	public void AddPartSelected(Part part, bool playDenySound, bool createNewStep)
	{
		if (HasStageSelected() && CanStagePart(part, playDenySound) && !selected.stage.parts.Contains(part))
		{
			selected.stage.AddPart(part, record: true, createNewStep);
		}
	}

	private static bool CanStagePart(Part part, bool playDenySound)
	{
		bool num = (part.onPartUsed?.GetPersistentEventCount() ?? 0) > 0 && !part.HasModule<CannotStageModule>();
		if (!num & playDenySound)
		{
			SoundPlayer.main.denySound.Play();
		}
		return num;
	}

	private void CreateStage_UI(Stage A, int index)
	{
		StageUI stageUI = UnityEngine.Object.Instantiate(stageUIPrefab, stagesHolder);
		stageUI.gameObject.SetActive(value: true);
		stageUI.Initialize(A);
		if (stagesUI.IsValidInsert(index))
		{
			stagesUI.Insert(index, stageUI);
		}
		else
		{
			stagesUI.Add(stageUI);
		}
		foreach (StageUI item in stagesUI)
		{
			item.transform.SetSiblingIndex(1);
		}
		stageUI.button.onClick += (Action)delegate
		{
			ToggleStageSelect(stageUI);
		};
		stageUI.duplicateButtons.ForEach(delegate(SFS.UI.Button duplicateButton)
		{
			duplicateButton.onClick += (Action)delegate
			{
				DuplicateStage(stageUI);
			};
		});
		stageUI.removeButtons.ForEach(delegate(SFS.UI.Button removeButton)
		{
			removeButton.onClick += (Action)delegate
			{
				RemoveStage(stageUI);
			};
		});
		stageUI.useButtons.ForEach(delegate(SFS.UI.Button useButton)
		{
			useButton.onClick += (Action)delegate
			{
				UseStage(A);
			};
		});
		scroller.RegisterScrolling(stageUI.button, () => !reorderingGroup.holding);
		reorderingGroup.Add(stageUI.elementOrderModule);
		RefreshStageId();
	}

	private void DestroyStage_UI(Stage A)
	{
		StageUI stageUI = stagesUI.FirstOrDefault((StageUI UI) => UI.stage == A);
		if (!(stageUI == null))
		{
			DeselectStage(stageUI);
			stagesUI.Remove(stageUI);
			stageUI.Destroy();
			float extraHeight = 0f - (stageUI.GetRect().rect.height + stagesGroup.spacing);
			UpdateIsScrolling(extraHeight);
			if (scrolling.Value)
			{
				ClampToTop(extraHeight);
			}
			RefreshStageId();
		}
	}

	private void RefreshStageId()
	{
		if (BuildManager.main != null)
		{
			for (int i = 0; i < stagesUI.Count; i++)
			{
				stagesUI[i].stage.stageId = i + 1;
			}
		}
		foreach (StageUI item in stagesUI)
		{
			item.stageIdText.Text = ((item.stage.stageId > 0) ? item.stage.stageId.ToString() : "");
		}
	}

	private void ClearStages_UI()
	{
		SetSelected(null);
		StageUI[] array = stagesUI.ToArray();
		foreach (StageUI stageUI in array)
		{
			DestroyStage_UI(stageUI.stage);
		}
	}

	void I_GLDrawer.Draw()
	{
		if (!HasStageSelected() || !menuHolder.gameObject.activeInHierarchy)
		{
			return;
		}
		foreach (Part part in selected.stage.parts)
		{
			BuildSelector.DrawRegionalOutline(part.GetClickPolygons(), symmetry: false, outlineColor, outlineWidth);
		}
	}
}
