using SFS.Builds;
using SFS.World;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.Parts.Modules;

public class ColorByScene : MonoBehaviour
{
	public SceneType sceneType;

	public Color color = Color.white;

	public Graphic graphic;

	private void Start()
	{
		if ((sceneType == SceneType.Build && BuildManager.main != null) || (sceneType == SceneType.World && GameManager.main != null))
		{
			graphic.color = color;
		}
	}
}
