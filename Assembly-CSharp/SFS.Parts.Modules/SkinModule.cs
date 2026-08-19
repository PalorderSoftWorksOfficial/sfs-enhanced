using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Variables;
using UnityEngine;

namespace SFS.Parts.Modules;

public class SkinModule : MonoBehaviour, I_InitializePartModule
{
	public string skinTag;

	public PipeMesh[] meshModules;

	private ColorTexture[] defaultColors;

	private ShapeTexture[] defaultShapes;

	public String_Reference colorTextureName;

	public String_Reference shapeTextureName;

	public bool disableColorSelect;

	public bool disableShapeSelect;

	int I_InitializePartModule.Priority => -10;

	void I_InitializePartModule.Initialize()
	{
		defaultColors = meshModules.Select((PipeMesh meshModule) => meshModule.textures.texture.colorTexture).ToArray();
		defaultShapes = meshModules.Select((PipeMesh meshModule) => meshModule.textures.texture.shapeTexture).ToArray();
		colorTextureName.OnChange += new Action(OnColorTextureChange);
		shapeTextureName.OnChange += new Action(OnShapeTextureChange);
	}

	public void SetTexture(int channel, PartTexture texture)
	{
		int num = GetTextureOptions(channel).IndexOf(texture);
		if (num != -1)
		{
			if (channel == 0)
			{
				colorTextureName.Value = GetColorTextures()[num].name;
			}
			if (channel == 1)
			{
				shapeTextureName.Value = GetShapeTextures()[num].name;
			}
		}
	}

	private void OnColorTextureChange()
	{
		for (int i = 0; i < meshModules.Length; i++)
		{
			bool fullVersion = DevSettings.FullVersion;
			bool fullVersion2 = DevSettings.FullVersion;
			string value = colorTextureName.Value;
			Dictionary<string, ColorTexture> colorTextures = Base.partsLoader.colorTextures;
			meshModules[i].SetColorTexture((fullVersion && colorTextures.TryGetValue(value, out var value2) && (!value2.pack_Redstone_Atlas | fullVersion2)) ? value2 : defaultColors[i]);
		}
	}

	private void OnShapeTextureChange()
	{
		for (int i = 0; i < meshModules.Length; i++)
		{
			bool fullVersion = DevSettings.FullVersion;
			bool fullVersion2 = DevSettings.FullVersion;
			string value = shapeTextureName.Value;
			Dictionary<string, ShapeTexture> shapeTextures = Base.partsLoader.shapeTextures;
			meshModules[i].SetShapeTexture((fullVersion && shapeTextures.TryGetValue(value, out var value2) && (!value2.pack_Redstone_Atlas | fullVersion2)) ? value2 : defaultShapes[i]);
		}
	}

	public PartTexture GetTexture(int channel)
	{
		Textures.TextureSelector texture = meshModules[0].textures.texture;
		return channel switch
		{
			0 => texture.colorTexture.GetTexID(), 
			1 => texture.shapeTexture.GetTexID(), 
			_ => throw new Exception(), 
		};
	}

	public List<PartTexture> GetTextureOptions(int channel)
	{
		List<PartTexture> result = null;
		if (channel == 0)
		{
			result = GetColorTextures().ConvertAll((ColorTexture colorTex) => colorTex.GetTexID());
		}
		if (channel == 1)
		{
			result = GetShapeTextures().ConvertAll((ShapeTexture shapeTex) => shapeTex.GetTexID());
		}
		return result;
	}

	private List<ColorTexture> GetColorTextures()
	{
		List<ColorTexture> list = new List<ColorTexture>();
		if (!disableColorSelect)
		{
			foreach (ColorTexture value in Base.partsLoader.colorTextures.Values)
			{
				if (value.tags.Contains(skinTag))
				{
					list.Add(value);
				}
			}
		}
		return list;
	}

	private List<ShapeTexture> GetShapeTextures()
	{
		List<ShapeTexture> list = new List<ShapeTexture>();
		if (!disableShapeSelect)
		{
			foreach (ShapeTexture value in Base.partsLoader.shapeTextures.Values)
			{
				if (value.tags.Contains(skinTag))
				{
					list.Add(value);
				}
			}
		}
		return list;
	}
}
