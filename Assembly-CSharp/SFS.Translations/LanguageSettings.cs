using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Input;
using SFS.IO;
using SFS.UI;
using SFS.Variables;
using TMPro;
using UnityEngine;

namespace SFS.Translations;

public class LanguageSettings : SettingsBase<LanguageLoader.LanguageReference>
{
	public static LanguageSettings main;

	public TextAdapter languageName;

	public GameObject settingsManager;

	public Event_Local onChange = new Event_Local();

	public TMP_Dropdown languageDropdown;

	private bool isInitialized;

	protected override string FileName => "LanguageSettings_2";

	private void Awake()
	{
		main = this;
	}

	private void Start()
	{
		if (Application.isEditor)
		{
			Initialize(onChange.Invoke);
		}
		languageDropdown.onValueChanged.AddListener(LanguageDropdownChanged);
	}

	public void Initialize(Action callback)
	{
		if (isInitialized)
		{
			return;
		}
		isInitialized = true;
		Base.language.RunAfterInitialization(delegate
		{
			Load();
			List<string> list = (from language in Base.language.GetAvailableLanguages()
				where language != null
				select language.displayName).ToList();
			languageDropdown.ClearOptions();
			languageDropdown.AddOptions(list);
			languageDropdown.value = list.IndexOf(Base.language.GetDisplayName(settings));
			callback();
		});
	}

	private void SelectLanguage(LanguageLoader.LanguageReference settings, Action callback)
	{
		Base.language.LoadLanguage(settings, Menu.read, delegate(bool success)
		{
			if (success)
			{
				base.settings = settings;
				Save();
			}
			callback();
			if (languageName != null)
			{
				languageName.Text = Base.language.GetDisplayName(base.settings);
			}
		});
	}

	protected override void OnLoad()
	{
		Base.language.LoadLanguage(settings, Menu.read, delegate
		{
			if (languageName != null)
			{
				languageName.Text = Base.language.GetDisplayName(settings);
			}
		});
	}

	public void OpenLanguageSelector()
	{
		OpenLanguageSelector(onChange.Invoke);
	}

	private void OpenLanguageSelector(Action callback)
	{
		if (!Base.language.IsInitialized)
		{
			Menu.loading.Open();
		}
		Base.language.RunAfterInitialization(delegate
		{
			Menu.loading.Close();
			List<MenuElement> list = new List<MenuElement> { new SizeSyncerBuilder(out var carrier).HorizontalMode(SizeMode.MaxChildSize) };
			LanguageLoader.LanguageReference[] availableLanguages = Base.language.GetAvailableLanguages();
			foreach (LanguageLoader.LanguageReference languageReference in availableLanguages)
			{
				if (languageReference != null)
				{
					LanguageLoader.LanguageReference copy = languageReference;
					string buttonText = copy.displayName;
					list.Add(ButtonBuilder.CreateButton(carrier, () => buttonText, delegate
					{
						ScreenManager.main.CloseCurrent();
						SelectLanguage(copy, callback);
					}, CloseMode.None).MinSize(300f, 60f));
				}
			}
			ScreenManager.main.OpenScreen(MenuGenerator.CreateMenu(CancelButton.None, CloseMode.None, delegate
			{
			}, delegate
			{
			}, list.ToArray()));
		});
	}

	private void LanguageDropdownChanged(int index)
	{
		SelectLanguage(Base.language.GetAvailableLanguages()[index], delegate
		{
		});
		languageDropdown.Hide();
	}
}
