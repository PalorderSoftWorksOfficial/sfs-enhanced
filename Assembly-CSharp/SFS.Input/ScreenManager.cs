using System;
using System.Collections.Generic;
using SFS.World;
using UnityEngine;

namespace SFS.Input;

public class ScreenManager : MonoBehaviour
{
	public static ScreenManager main;

	private Stack<Func<Screen_Base>> screenStack = new Stack<Func<Screen_Base>>();

	[Space]
	public bool selfInitialize;

	public Screen_Base initialScreen;

	public Screen_Base CurrentScreen { get; private set; }

	private void Awake()
	{
		main = this;
		if (selfInitialize)
		{
			SetStack(() => initialScreen);
		}
		else
		{
			Time.timeScale = 0f;
		}
	}

	private void OnDestroy()
	{
		if (CurrentScreen != null)
		{
			CurrentScreen.OnClose();
		}
	}

	public int GetStackCount()
	{
		return screenStack.Count;
	}

	public void SetStack(Func<Screen_Base> screen)
	{
		screenStack = new Stack<Func<Screen_Base>>();
		screenStack.Push(screen);
		OpenScreen(screenStack.Pop());
	}

	public void CloseCurrent()
	{
		if (screenStack.Count != 1)
		{
			screenStack.Pop();
			OpenScreen(screenStack.Pop());
		}
	}

	public void CloseStack()
	{
		while (screenStack.Count > 1)
		{
			screenStack.Pop();
		}
		OpenScreen(screenStack.Pop());
	}

	public void OpenScreen(Func<Screen_Base> screen)
	{
		screenStack.Push(screen);
		if (CurrentScreen != null)
		{
			CurrentScreen.OnClose();
		}
		CurrentScreen = screen();
		CurrentScreen.OnOpen();
		Time.timeScale = (CurrentScreen.PauseWhileOpen ? 0f : ((WorldTime.main != null) ? WorldTime.main.TimeScale : 1f));
	}

	private void Update()
	{
		CurrentScreen.ProcessInput();
	}
}
