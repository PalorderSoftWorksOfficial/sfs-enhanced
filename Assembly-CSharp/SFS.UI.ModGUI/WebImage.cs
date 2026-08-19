using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI.ModGUI;

public class WebImage : GUIElement
{
	private SDWebImage webImage;

	private Mask maskComponent;

	private Image maskImage;

	public string ImageLink
	{
		get
		{
			return webImage.imageURL;
		}
		set
		{
			webImage.imageURL = value;
			webImage.SetImage();
		}
	}

	public SDWebImage.LoadingIndicatorType LoadingIndicatorType
	{
		get
		{
			return webImage.loadingIndicatorType;
		}
		set
		{
			webImage.loadingIndicatorType = value;
		}
	}

	public Color LoadingIndicatorColor
	{
		get
		{
			return webImage.loadingIndicatorColor;
		}
		set
		{
			webImage.loadingIndicatorColor = value;
		}
	}

	public bool PreserveAspect
	{
		get
		{
			return webImage.preserveAspect;
		}
		set
		{
			webImage.preserveAspect = value;
		}
	}

	public bool UseMask
	{
		get
		{
			return maskComponent.enabled;
		}
		set
		{
			maskComponent.enabled = value;
			maskImage.enabled = value;
		}
	}

	public override void Init(GameObject self, Transform parent)
	{
		gameObject = self;
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		rectTransform = gameObject.Rect();
		webImage = gameObject.transform.GetChild(0).GetComponent<SDWebImage>();
		maskComponent = gameObject.GetComponent<Mask>();
		maskImage = gameObject.GetComponent<Image>();
		webImage.Init();
	}
}
