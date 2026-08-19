using System;
using SFS.Parts;
using SFS.Parts.Modules;
using UnityEngine;

namespace SFS.UI;

public class AttachableStatsMenu : MonoBehaviour
{
	public StatsMenu drawer;

	public AttachWithArrow attach;

	public OpenTracker Open_DrawPart(Func<bool> hasControl, Part[] allParts, PartDrawSettings partDrawSettings, Func<Vector2> getScreenPosition, bool dontUpdateOnZoomChange, bool skipAnimation, Action onOpen = null, Action onClose = null)
	{
		onOpen = (Action)Delegate.Combine(onOpen, (Action)delegate
		{
			Part[] array = allParts;
			foreach (Part obj in array)
			{
				obj.onPartDestroyed = (Action<Part>)Delegate.Combine(obj.onPartDestroyed, new Action<Part>(OnPartDestroyed));
			}
		});
		onClose = (Action)Delegate.Combine(onClose, (Action)delegate
		{
			Part[] array = allParts;
			foreach (Part obj in array)
			{
				obj.onPartDestroyed = (Action<Part>)Delegate.Remove(obj.onPartDestroyed, new Action<Part>(OnPartDestroyed));
			}
		});
		return Open(hasControl, delegate(StatsMenu builder)
		{
			allParts[0].DrawPartStats(allParts, builder, partDrawSettings);
		}, getScreenPosition, dontUpdateOnZoomChange, skipAnimation, onOpen, onClose);
		void OnPartDestroyed(Part _)
		{
			Close();
		}
	}

	public OpenTracker Open(Func<bool> hasControl, Action<StatsMenu> draw, Func<Vector2> getScreenPosition, bool dontUpdateOnZoomChange, bool skipAnimation, Action onOpen = null, Action onClose = null)
	{
		attach.Open(getScreenPosition, dontUpdateOnZoomChange);
		return drawer.Open(hasControl, draw, skipAnimation, onOpen, onClose);
	}

	public void Close()
	{
		attach.Close();
		drawer.Close();
	}
}
