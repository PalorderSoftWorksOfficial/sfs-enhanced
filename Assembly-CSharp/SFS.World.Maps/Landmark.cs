using System;
using SFS.Translations;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World.Maps;

public class Landmark : MonoBehaviour
{
	public LandmarkData data;

	public Double2 position;

	public Field displayName;

	public void Initialize(LandmarkData data, Planet planet)
	{
		this.data = data;
		position = Double2.CosSin((float)Math.PI / 180f * data.angle) * (planet.Radius + planet.GetTerrainHeightAtAngle(data.angle * ((float)Math.PI / 180f)));
		Loc.OnChange += new Action(UpdateName);
	}

	private void OnDestroy()
	{
		Loc.OnChange -= new Action(UpdateName);
	}

	private void UpdateName()
	{
		displayName = GetDisplayName(data.name);
	}

	private static Field GetDisplayName(string codeName)
	{
		return codeName switch
		{
			"Sea of Tranquility" => Loc.main.Sea_of_Tranquility, 
			"Sea of Serenity" => Loc.main.Sea_of_Serenity, 
			"Ocean of Storms" => Loc.main.Ocean_of_Storms, 
			"Copernicus Crater" => Loc.main.Copernicus_Crater, 
			"Tycho Crater" => Loc.main.Tycho_Crater, 
			"Olympus Mons" => Loc.main.Olympus_Mons, 
			"Valles Marineris" => Loc.main.Valles_Marineris, 
			"Gale Crater" => Loc.main.Gale_Crater, 
			"Hellas Planitia" => Loc.main.Hellas_Planitia, 
			"Arcadia Planitia" => Loc.main.Arcadia_Planitia, 
			"Utopia Planitia" => Loc.main.Utopia_Planitia, 
			"Jezero Crater" => Loc.main.Jezero_Crater, 
			"Atalanta Planitia" => Loc.main.Atalanta_Planitia, 
			"Lavinia Planitia" => Loc.main.Lavinia_Planitia, 
			"Maxwell Montes" => Loc.main.Maxwell_Montes, 
			"Caloris Planitia" => Loc.main.Caloris_Planitia, 
			"Borealis Planitia" => Loc.main.Borealis_Planitia, 
			_ => Field.Text(Application.isEditor ? ("*" + codeName) : codeName), 
		};
	}
}
