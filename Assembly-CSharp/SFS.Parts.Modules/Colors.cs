using System;
using System.Collections.Generic;
using UnityEngine;
using UV;

namespace SFS.Parts.Modules;

[Serializable]
public class Colors
{
	[Serializable]
	public class ColorKey
	{
		public ColorSelector color;

		public float height;
	}

	[Serializable]
	public class ColorSelector
	{
		public enum Type
		{
			Local,
			Module
		}

		public Type type;

		public Color colorBasic = Color.white;

		public ColorModule colorModule;

		public Color GetColor()
		{
			if (type != Type.Local)
			{
				return colorModule.GetColor();
			}
			return colorBasic;
		}
	}

	public Mode mode;

	public ColorSelector color;

	public ColorKey[] colors;

	public Color_Channel GetOutput()
	{
		if (mode == Mode.Single)
		{
			Color color = this.color.GetColor();
			return new Color_Channel(new List<StartEnd_Color>
			{
				new StartEnd_Color(new Color2(color, color), new Line(0f, 0f))
			});
		}
		Color_Channel color_Channel = new Color_Channel(new List<StartEnd_Color>());
		for (int i = 0; i < colors.Length; i++)
		{
			Color2 color_Edge = new Color2(colors[i].color.GetColor(), colors[i].color.GetColor());
			Line height = new Line((i == 0) ? 0f : colors[i - 1].height, colors[i].height);
			color_Channel.elements.Add(new StartEnd_Color(color_Edge, height));
		}
		return color_Channel;
	}
}
