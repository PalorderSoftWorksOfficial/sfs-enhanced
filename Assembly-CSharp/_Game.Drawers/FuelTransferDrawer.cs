using System;
using SFS.World;
using SFS.World.Maps;
using UnityEngine;

namespace _Game.Drawers;

public class FuelTransferDrawer : MonoBehaviour
{
	public Transform transferHolder;

	public FuelTransferUI gaugePrefab;

	public FuelTransferLine linePrefab;

	private Pool<FuelTransferUI> gauges;

	private Pool<FuelTransferLine> lines;

	private Local_Resources resources = new Local_Resources();

	private void Start()
	{
		gauges = new Pool<FuelTransferUI>(() => UnityEngine.Object.Instantiate(gaugePrefab, transferHolder), delegate(FuelTransferUI A)
		{
			if (A.gameObject.activeInHierarchy)
			{
				A.gameObject.SetActive(value: false);
			}
		});
		lines = new Pool<FuelTransferLine>(delegate
		{
			FuelTransferLine fuelTransferLine = UnityEngine.Object.Instantiate(linePrefab, transferHolder);
			fuelTransferLine.transform.SetAsFirstSibling();
			return fuelTransferLine;
		}, delegate(FuelTransferLine A)
		{
			if (A.gameObject.activeInHierarchy)
			{
				A.gameObject.SetActive(value: false);
			}
		});
		PlayerController.main.player.OnChange += new Action(OnPlayerChange);
		resources.OnChange += new Action(OnResourcesChange);
	}

	private void OnDestroy()
	{
		PlayerController.main.player.OnChange -= new Action(OnPlayerChange);
		resources.Value = null;
	}

	private void OnPlayerChange()
	{
		resources.Value = ((PlayerController.main.player.Value is Rocket rocket) ? rocket.resources : null);
	}

	private void OnResourcesChange()
	{
		if (!(base.enabled = resources.Value != null))
		{
			gauges.Reset();
			lines.Reset();
		}
	}

	private void LateUpdate()
	{
		gauges.Reset();
		lines.Reset();
		if ((float)PlayerController.main.cameraDistance > 1000f || Map.manager.mapMode.Value)
		{
			return;
		}
		foreach (SFS.World.Resources.Transfer transfer in resources.Value.transfers)
		{
			FuelTransferUI item = gauges.GetItem();
			item.DrawFuelPercent(transfer);
			item.gameObject.SetActive(value: true);
		}
		if (resources.Value.transfers.Count == 2 && resources.Value.transfers[0].group.resourceType == resources.Value.transfers[1].group.resourceType)
		{
			FuelTransferLine item2 = lines.GetItem();
			item2.gameObject.SetActive(value: true);
			Vector3 vector = gauges.Items[1].holder.localPosition - gauges.Items[0].holder.localPosition;
			item2.holder.localPosition = gauges.Items[0].holder.localPosition;
			item2.holder.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(vector.y, vector.x) * 57.29578f);
			item2.holder.sizeDelta = new Vector2(vector.magnitude, 0f);
			double resourceAmount = resources.Value.transfers[0].group.ResourceAmount;
			double resourceSpace = resources.Value.transfers[1].group.ResourceSpace;
			item2.transferAnimation.enabled = resourceAmount > 0.0 && resourceSpace > 0.0;
		}
	}
}
