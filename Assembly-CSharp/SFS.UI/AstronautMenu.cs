using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Career;
using SFS.Input;
using SFS.Translations;
using SFS.World;
using UnityEngine;

namespace SFS.UI;

public class AstronautMenu : Screen_Menu
{
	public static AstronautMenu main;

	public GameObject menuHolder;

	public ScrollElement elementHolder;

	public AstronautElement elementPrefab;

	public RadioGroup radioGroup;

	public Button fireButton;

	private List<AstronautElement> elements = new List<AstronautElement>();

	private CrewModule.Seat seat;

	private Action redrawSeat;

	protected override CloseMode OnEscape => CloseMode.Current;

	private void Awake()
	{
		main = this;
	}

	private void Start()
	{
		menuHolder.SetActive(value: false);
		base.enabled = false;
		elementPrefab.gameObject.SetActive(value: false);
	}

	public void OpenMenu(CrewModule.Seat seat, Action redrawSeat)
	{
		Open();
		this.seat = seat;
		this.redrawSeat = redrawSeat;
		DrawList();
	}

	public override void OnOpen()
	{
		menuHolder.gameObject.SetActive(value: true);
		base.enabled = true;
		DrawList();
	}

	public override void OnClose()
	{
		menuHolder.SetActive(value: false);
		base.enabled = false;
	}

	private void DrawList()
	{
		foreach (AstronautElement element in elements)
		{
			UnityEngine.Object.Destroy(element.gameObject);
		}
		elements.Clear();
		List<WorldSave.Astronauts.Data> list = AstronautState.main.state.astronauts.ToList();
		list.Sort((WorldSave.Astronauts.Data a, WorldSave.Astronauts.Data b) => GetSortingScore(a).CompareTo(GetSortingScore(b)));
		foreach (WorldSave.Astronauts.Data item in list)
		{
			AstronautElement astronautElement = UnityEngine.Object.Instantiate(elementPrefab, elementHolder.transform);
			astronautElement.gameObject.SetActive(value: true);
			elementHolder.RegisterScrolling(astronautElement.elementBack);
			elements.Add(astronautElement);
			string astronautName = item.astronautName;
			astronautElement.astronaut = astronautName;
			astronautElement.nameText.Text = astronautName;
			bool flag = seat != null;
			AstronautState.State astronautState = AstronautState.main.GetAstronautState(astronautName);
			astronautElement.statusText.Text = AstronautState.main.GetAstronautStateText(astronautState, flag);
			astronautElement.button.gameObject.SetActive(flag);
			if (flag)
			{
				astronautElement.button.SetEnabled(astronautState == AstronautState.State.Available);
				astronautElement.button.onClick += (Action)delegate
				{
					seat.Board(astronautName, 1.0, float.NegativeInfinity);
					redrawSeat();
					Close();
				};
			}
		}
		static int GetSortingScore(WorldSave.Astronauts.Data a)
		{
			return (int)AstronautState.main.GetAstronautState(a.astronautName);
		}
	}

	private void Update()
	{
		bool selected = GetSelected(out var _);
		fireButton.SetEnabled(selected);
	}

	private bool GetSelected(out AstronautElement selected)
	{
		selected = elements.Find((AstronautElement element) => element.elementBack == radioGroup.SelectedButton);
		return selected != null;
	}

	public void CreateAstronaut()
	{
		Menu.textInput.Open(Loc.main.Cancel, "Create", delegate(string[] textInput)
		{
			Create(textInput[0]);
		}, CloseMode.Current, TextInputMenu.Element("Astronaut name", ""));
	}

	private void Create(string astronautName)
	{
		AstronautState.main.CreateAstronaut(astronautName);
		DrawList();
	}

	public void FireAstronaut()
	{
		if (GetSelected(out var selected))
		{
			string astronaut = selected.astronaut;
			MenuGenerator.OpenConfirmation(CloseMode.Current, () => "Discharge astronaut " + astronaut + "?", () => "Discharge", delegate
			{
				AstronautState.main.FireAstronaut(astronaut);
				DrawList();
			});
		}
	}
}
