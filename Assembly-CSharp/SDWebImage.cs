using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("Miscellaneous/SDWebImage")]
public class SDWebImage : MonoBehaviour
{
	public enum LoadingIndicatorType
	{
		None,
		RoundedRect,
		Circle,
		Circles
	}

	public delegate void OnImageSizeReadyAction(Vector2 size);

	public delegate void OnLoadingErrorAction(SDWebImageDownloaderError error);

	public string imageURL;

	public Texture2D placeholderImage;

	public bool preserveAspect;

	public bool autoDownload = true;

	public bool memoryCache;

	public bool diskCache = true;

	public bool showLoadingIndicator = true;

	public GameObject loadingIndicator;

	public LoadingIndicatorType loadingIndicatorType;

	public float loadingIndicatorScale = 1f;

	public Color loadingIndicatorColor = Color.black;

	private Component _targetComponent;

	private int _targetMaterial;

	public event OnImageSizeReadyAction OnImageSizeReady;

	public event OnLoadingErrorAction OnLoadingError;

	private void Start()
	{
		Init();
		if (autoDownload)
		{
			SetImage();
		}
	}

	public void Init()
	{
		_targetComponent = GetTargetComponent();
		if ((bool)loadingIndicator)
		{
			loadingIndicator.SetActive(value: false);
		}
	}

	public void SetImage()
	{
		SetImageWithURL(imageURL, placeholderImage);
	}

	public void SetImageWithURL(string url)
	{
		InternalSetImageWithURL(url, null, GetSDWebImageOptions(), null, SetTexture);
	}

	public void SetImageWithURL(string url, Texture2D placeholder)
	{
		InternalSetImageWithURL(url, placeholder, GetSDWebImageOptions(), null, SetTexture);
	}

	public void SetImageWithURL(string url, Texture2D placeholder, SDWebImageOptions options)
	{
		InternalSetImageWithURL(url, placeholder, options, null, SetTexture);
	}

	public void SetImageWithURL(string url, Action<Texture2D> completionCallback)
	{
		InternalSetImageWithURL(url, null, GetSDWebImageOptions(), null, completionCallback);
	}

	public void SetImageWithURL(string url, Texture2D placeholder, Action<float> progressCallback)
	{
		InternalSetImageWithURL(url, placeholder, GetSDWebImageOptions(), progressCallback, SetTexture);
	}

	public void SetImageWithURL(string url, Texture2D placeholder, Action<Texture2D> completionCallback)
	{
		InternalSetImageWithURL(url, placeholder, GetSDWebImageOptions(), null, completionCallback);
	}

	public void SetImageWithURL(string url, Texture2D placeholder, SDWebImageOptions options, Action<float> progressCallback)
	{
		InternalSetImageWithURL(url, placeholder, options, progressCallback, SetTexture);
	}

	public void SetImageWithURL(string url, Texture2D placeholder, SDWebImageOptions options, Action<Texture2D> completionCallback)
	{
		InternalSetImageWithURL(url, placeholder, options, null, completionCallback);
	}

	public void SetImageWithURL(string url, Texture2D placeholder, Action<float> progressCallback, Action<Texture2D> completionCallback)
	{
		InternalSetImageWithURL(url, placeholder, GetSDWebImageOptions(), progressCallback, completionCallback);
	}

	public void SetImageWithURL(string url, Texture2D placeholder, SDWebImageOptions options, Action<float> progressCallback, Action<Texture2D> completionCallback)
	{
		InternalSetImageWithURL(url, placeholder, options, progressCallback, completionCallback);
	}

	private void InternalSetImageWithURL(string url, Texture2D placeholder, SDWebImageOptions options, Action<float> progressCallback, Action<Texture2D> completionCallback)
	{
		if (placeholder != null && completionCallback != null)
		{
			completionCallback(placeholder);
		}
		if (string.IsNullOrEmpty(url))
		{
			return;
		}
		if ((options & SDWebImageOptions.ShowLoadingIndicator) != SDWebImageOptions.None && (bool)loadingIndicator)
		{
			loadingIndicator.GetComponent<RectTransform>().localPosition = Vector3.zero;
			loadingIndicator.SetActive(value: true);
		}
		Singleton<SDWebImageManager>.instance.LoadImageWithURL(url, GetInstanceID(), options, progressCallback, delegate(SDWebImageDownloaderError error, byte[] imageData)
		{
			if ((options & SDWebImageOptions.ShowLoadingIndicator) != SDWebImageOptions.None && (bool)loadingIndicator)
			{
				loadingIndicator.SetActive(value: false);
			}
			if (error != null)
			{
				Debug.LogWarning(error.description);
				if (OnLoadingError != null)
				{
					OnLoadingError(error);
				}
			}
			else
			{
				Texture2D texture2D = TextureFromData(imageData);
				if (texture2D != null)
				{
					if (OnImageSizeReady != null)
					{
						OnImageSizeReady(new Vector2(texture2D.width, texture2D.height));
					}
					if (completionCallback != null)
					{
						completionCallback(texture2D);
					}
				}
			}
		});
	}

	private Component GetTargetComponent()
	{
		return GetComponents<Component>().FirstOrDefault((Component component) => component is Renderer || component is RawImage || component is Image);
	}

	private void SetTexture(Texture2D texture)
	{
		if (_targetComponent == null)
		{
			return;
		}
		if (_targetComponent is SpriteRenderer)
		{
			SpriteRenderer obj = (SpriteRenderer)_targetComponent;
			Vector2 size = obj.size;
			Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
			sprite.name = "Web Image Sprite";
			sprite.hideFlags = HideFlags.HideAndDontSave;
			obj.sprite = sprite;
			obj.size = size;
		}
		else if (_targetComponent is Renderer)
		{
			Renderer renderer = (Renderer)_targetComponent;
			if (!(renderer.sharedMaterial == null))
			{
				if (renderer.sharedMaterials.Length != 0 && renderer.sharedMaterials.Length > _targetMaterial)
				{
					renderer.sharedMaterials[_targetMaterial].mainTexture = texture;
				}
				else
				{
					renderer.sharedMaterial.mainTexture = texture;
				}
			}
		}
		else if (_targetComponent is RawImage)
		{
			((RawImage)_targetComponent).texture = texture;
		}
		else if (_targetComponent is Image)
		{
			Image obj2 = (Image)_targetComponent;
			Sprite sprite2 = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0f, 0f));
			obj2.preserveAspect = preserveAspect;
			obj2.sprite = sprite2;
		}
	}

	private SDWebImageOptions GetSDWebImageOptions()
	{
		SDWebImageOptions sDWebImageOptions = SDWebImageOptions.None;
		if (memoryCache)
		{
			sDWebImageOptions |= SDWebImageOptions.MemoryCache;
		}
		if (diskCache)
		{
			sDWebImageOptions |= SDWebImageOptions.DiskCache;
		}
		if (showLoadingIndicator)
		{
			sDWebImageOptions |= SDWebImageOptions.ShowLoadingIndicator;
		}
		return sDWebImageOptions;
	}

	private Texture2D TextureFromData(byte[] data)
	{
		Texture2D texture2D = new Texture2D(8, 8);
		texture2D.LoadImage(data);
		if (!(texture2D == null) && (texture2D.width != 8 || texture2D.height != 8))
		{
			return texture2D;
		}
		return null;
	}
}
