using System;
using UnityEngine;

namespace SFS.World;

public class GameMenuLayout_Mobile : MonoBehaviour
{
	public GameObject locationTextBelow;

	public GameObject locationTextSide;

	public StagingDrawer stagingDrawer;

	public ThrottleDrawer throttleDrawer;

	public GameObject openStagingBelow;

	public GameObject openStagingSide;

	public GameObject closeStagingBelow;

	public GameObject closeStagingSide;

	public GameObject useStageButtonBelow;

	public GameObject useStageButtonSide;

	[Space]
	public RectTransform bottomRightArea;

	public RectTransform throttleWindow;

	public RectTransform stagingArea;

	public RectTransform stagingWindow;

	private void Start()
	{
		Base.screenTracker.orientation.OnChange += new Action(UpdateLayout);
		PlayerController.main.player.OnChange += new Action<Player, Player>(OnPlayerChange);
		WorldTime.main.realtimePhysics.OnChange += new Action(UpdateRightMenuEnabled);
	}

	private void OnDestroy()
	{
		Base.screenTracker.orientation.OnChange -= new Action(UpdateLayout);
	}

	private void Update()
	{
		bool hasStages = stagingDrawer.HasStages;
		if (useStageButtonBelow.activeSelf != hasStages)
		{
			useStageButtonBelow.SetActive(hasStages);
			useStageButtonSide.SetActive(hasStages);
			UpdateLayout();
		}
	}

	public void UpdateLayout()
	{
		bottomRightArea.offsetMax = new Vector2(bottomRightArea.offsetMax.x, 0f);
		bool flag = !Base.screenTracker.Portrait && (bottomRightArea.rect.size.y - 180f) / (throttleWindow.rect.size.y - 65f) < 0.9f;
		bool flag2 = !Base.screenTracker.Portrait & flag;
		locationTextBelow.SetActive(!flag2);
		locationTextSide.SetActive(flag2 && !GameMenus.main.recoverButton.gameObject.activeInHierarchy);
		float num = (Base.screenTracker.Portrait ? 250 : (flag2 ? 100 : 180));
		bottomRightArea.offsetMax = new Vector2(bottomRightArea.offsetMax.x, 0f - num);
		bool flag3 = !Base.screenTracker.Portrait;
		openStagingBelow.SetActive(!flag3);
		openStagingSide.SetActive(flag3);
		closeStagingBelow.SetActive(!flag3);
		closeStagingSide.SetActive(flag3);
		float num2 = ((flag3 || !useStageButtonBelow.activeSelf) ? 65f : 0f);
		float num3 = Mathf.Clamp01(bottomRightArea.rect.size.y / (throttleWindow.rect.size.y - num2));
		throttleWindow.localScale = Vector3.one * num3;
		throttleWindow.localPosition = Vector3.down * (num2 * num3);
		float y = (flag3 ? 71 : 144);
		stagingArea.offsetMin = new Vector2(stagingArea.offsetMax.x, y);
		stagingWindow.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, stagingArea.rect.height / stagingWindow.localScale.y);
	}

	private void OnPlayerChange(Player valueOld, Player valueNew)
	{
		if (valueOld is Rocket rocket)
		{
			rocket.staging.editMode.OnChange -= new Action(UpdateRightMenuEnabled);
		}
		if (valueNew is Rocket rocket2)
		{
			rocket2.staging.editMode.OnChange += new Action(UpdateRightMenuEnabled);
		}
		else
		{
			UpdateRightMenuEnabled();
		}
	}

	private void UpdateRightMenuEnabled()
	{
		bool flag = PlayerController.main.player.Value is Rocket rocket && rocket.staging.editMode.Value;
		throttleDrawer.shown.Value = !flag;
		stagingDrawer.shown.Value = flag;
	}
}
