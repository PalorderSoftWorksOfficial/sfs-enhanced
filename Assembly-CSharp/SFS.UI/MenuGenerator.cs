using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Input;
using SFS.Translations;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

public static class MenuGenerator
{
	public static void OpenConfirmation(CloseMode closeMode, Func<string> text, Func<string> confirmText, Action onConfirm, Func<string> denyText = null, Action onDeny = null)
	{
		OpenConfirmation(closeMode, delegate
		{
		}, delegate
		{
		}, text, confirmText, onConfirm, denyText ?? ((Func<string>)(() => Loc.main.Cancel)), onDeny ?? ((Action)delegate
		{
		}));
	}

	public static CallbackAwaiter<bool> OpenConfirmationAsync(CloseMode closeMode, Func<string> text, Func<string> confirmText, Func<string> denyText = null)
	{
		CallbackAwaiter<bool> awaitable = new CallbackAwaiter<bool>();
		OpenConfirmation(closeMode, delegate
		{
		}, delegate
		{
		}, text, confirmText, delegate
		{
			awaitable.Callback(result: true);
		}, denyText ?? ((Func<string>)(() => Loc.main.Cancel)), delegate
		{
			awaitable.Callback(result: false);
		});
		return awaitable;
	}

	public static void OpenConfirmation(CloseMode closeMode, Action onMenuOpen, Action onMenuClose, Func<string> text, Func<string> confirmText, Action onConfirm, Func<string> denyText, Action onDeny)
	{
		Func<Screen_Base> screen = CreateMenu(CancelButton.None, closeMode, onMenuOpen, onMenuClose, TextBuilder.CreateText(text), ElementGenerator.VerticalSpace(50), ElementGenerator.HorizontalGroup(delegate(HorizontalLayoutGroup group)
		{
			group.spacing = 10f;
			((RectTransform)group.transform).pivot = new Vector2(0.5f, 0.5f);
		}, true, true, new SizeSyncerBuilder(out var carrier).HorizontalMode(SizeMode.MaxChildSize), ButtonBuilder.CreateButton(carrier, denyText, onDeny, closeMode).MinSize(250f, 60f), ButtonBuilder.CreateButton(carrier, confirmText, onConfirm, closeMode).MinSize(250f, 60f)));
		ScreenManager.main.OpenScreen(screen);
	}

	public static void AskOverwrite(Func<string> titleText, Func<string> overwriteText, Func<string> newText, Action overwriteSave, Action newSave)
	{
		ShowChoices(titleText, ButtonBuilder.CreateButton(null, overwriteText, overwriteSave, CloseMode.Stack).MinSize(300f, 60f), ButtonBuilder.CreateButton(null, newText, newSave, CloseMode.Stack).MinSize(300f, 60f), ButtonBuilder.CreateButton(null, () => Loc.main.Cancel, delegate
		{
		}, CloseMode.Current).MinSize(300f, 60f));
	}

	public static void ShowChoices(Func<string> titleText, params ButtonBuilder[] buttons)
	{
		List<MenuElement> list = new List<MenuElement>();
		if (titleText != null)
		{
			list.Add(TextBuilder.CreateText(titleText));
			list.Add(ElementGenerator.VerticalSpace(50));
		}
		list.Add(new SizeSyncerBuilder(out var syncer).HorizontalMode(SizeMode.MaxChildSize));
		list.AddRange(((IEnumerable<ButtonBuilder>)buttons).Select((Func<ButtonBuilder, MenuElement>)((ButtonBuilder button) => button.SizeSync(syncer))).ToArray());
		ScreenManager.main.OpenScreen(CreateMenu(CancelButton.None, CloseMode.None, delegate
		{
		}, delegate
		{
		}, list.ToArray()));
	}

	public static void OpenMenu(CancelButton cancelButton, CloseMode onEscape, params MenuElement[] elements)
	{
		ScreenManager.main.OpenScreen(CreateMenu(cancelButton, onEscape, delegate
		{
		}, delegate
		{
		}, elements));
	}

	public static Func<Screen_Base> CreateMenu(CancelButton cancelButton, CloseMode onCancel, Action onMenuOpen, Action onMenuClose, params MenuElement[] elements)
	{
		return Menu.options.CreateDelegate(onCancel, cancelButton, onMenuOpen, onMenuClose, elements);
	}
}
