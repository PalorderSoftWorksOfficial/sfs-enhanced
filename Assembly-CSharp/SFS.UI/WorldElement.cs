using System;
using System.Text;
using SFS.Translations;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.UI;

public class WorldElement : MonoBehaviour
{
	public TextAdapter worldNameText;

	public TextAdapter infoText;

	public ReorderingModule reorderingModule;

	public RadioButton radioButton;

	public Button button;

	public GameObject buttonHolder;

	public Button resumeGameButton;

	public Button renameButton;

	public WorldReference World { get; private set; }

	public string Name => World.worldName;

	public void Setup(WorldReference world)
	{
		World = world;
		worldNameText.Text = World.worldName;
		WorldSettings worldSettings = world.LoadWorldSettings();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(Loc.main.World_Mode_Name.Inject(worldSettings.mode.GetModeName(), "value"));
		stringBuilder.Append("\n" + Loc.main.World_Difficulty_Name.Inject(worldSettings.difficulty.GetName(), "value"));
		if (!worldSettings.mode.allowQuicksaves)
		{
			stringBuilder.Append("\n" + Loc.main.Allow_Quicksaves_Name.Inject(Loc.main.Off, "value"));
		}
		stringBuilder.Append("<size=25>\n\n</size>");
		stringBuilder.AppendLine((DateTime.Now - new DateTime((worldSettings.playtime.lastPlayedTime_Ticks == 0L) ? DateTime.Now.Ticks : worldSettings.playtime.lastPlayedTime_Ticks)).ToLastPlayedString());
		stringBuilder.Append(TimeSpan.FromSeconds((int)worldSettings.playtime.totalPlayTime_Seconds).ToTimePlayedString());
		infoText.Text = stringBuilder.ToString();
		HorizontalLayout[] componentsInChildren = GetComponentsInChildren<HorizontalLayout>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].UpdateHierarchy();
		}
	}
}
