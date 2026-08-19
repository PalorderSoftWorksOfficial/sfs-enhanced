using System;
using SFS.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.World;

[Serializable]
public class TemperatureBar
{
	public GameObject[] holders;

	public TextAdapter temperatureText;

	public Image bar;

	public TextAdapter temperatureDegree;
}
