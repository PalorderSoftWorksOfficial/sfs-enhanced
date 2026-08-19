using SFS.Builds;
using SFS.World;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.Parts.Modules;

public class MaterialByScene : MonoBehaviour
{
	public SceneType sceneType;

	public Material material;

	public Graphic graphic;

	private void Start()
	{
		if ((sceneType == SceneType.Build && BuildManager.main != null) || (sceneType == SceneType.World && GameManager.main != null))
		{
			graphic.material = material;
		}
	}
}
