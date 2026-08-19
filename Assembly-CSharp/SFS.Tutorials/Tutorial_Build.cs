using SFS.Builds;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.World;
using UnityEngine;
using UnityEngine.Analytics;

namespace SFS.Tutorials;

public class Tutorial_Build : Tutorial_Base
{
	public GameObject capsulePopup;

	public GameObject parachutePopup;

	public GameObject fuelTankPopup;

	public GameObject enginePopup;

	public GameObject separatorPopup;

	public GameObject descriptionPopup;

	public GameObject infiniteArea;

	private void Start()
	{
		capsulePopup.SetActive(value: false);
		parachutePopup.SetActive(value: false);
		fuelTankPopup.SetActive(value: false);
		enginePopup.SetActive(value: false);
		separatorPopup.SetActive(value: false);
		descriptionPopup.SetActive(value: false);
		infiniteArea.SetActive(value: false);
		if (FileLocations.HasNotification("Tut_Basic_Build"))
		{
			return;
		}
		if (!FileLocations.HasNotification("First_Time_Playing"))
		{
			FileLocations.WriteNotification("Tut_Basic_Build");
		}
		else
		{
			if (AnalyticsSessionInfo.sessionCount > 3 && !Application.isEditor)
			{
				return;
			}
			PartHolder hold = BuildManager.main.holdGrid.holdGrid.partsHolder;
			PartHolder active = BuildManager.main.buildGrid.activeGrid.partsHolder;
			Add_ShowPopup(capsulePopup, () => hold.HasModule<CrewModule>());
			Add_Check(() => active.HasModule<CrewModule>());
			Add_Action(delegate
			{
				enginePopup.SetActive(value: true);
				fuelTankPopup.SetActive(value: true);
			});
			Add_Check(delegate
			{
				if (hold.HasModule<EngineModule>())
				{
					enginePopup.SetActive(value: false);
				}
				if (hold.HasModule<ResourceModule>())
				{
					fuelTankPopup.SetActive(value: false);
				}
				return !enginePopup.activeSelf && !fuelTankPopup.activeSelf;
			});
			Add_Check(() => active.HasModule<EngineModule>() && active.HasModule<ResourceModule>());
			Add_ShowPopup(separatorPopup, () => hold.HasModule<DetachModule>());
			Add_Check(() => active.HasModule<DetachModule>());
			Add_ShowPopup(parachutePopup, () => hold.HasModule<ParachuteModule>());
			Add_Check(() => active.HasModule<ParachuteModule>());
			Add_Action(delegate
			{
				FileLocations.WriteNotification("Tut_Basic_Build");
			});
		}
	}
}
