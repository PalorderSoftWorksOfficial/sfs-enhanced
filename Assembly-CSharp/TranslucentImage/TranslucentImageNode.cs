using System;
using SFS.Cameras;
using UnityEngine;

namespace TranslucentImage;

public class TranslucentImageNode : MonoBehaviour
{
	public TranslucentImageSource source;

	private void Start()
	{
		ActiveCamera.main.activeCamera.OnChange += new Action(OnMainCameraChange);
	}

	private void OnMainCameraChange()
	{
		if (ActiveCamera.Camera != null)
		{
			source = ActiveCamera.Camera.GetComponent<TranslucentImageSource>();
		}
	}
}
