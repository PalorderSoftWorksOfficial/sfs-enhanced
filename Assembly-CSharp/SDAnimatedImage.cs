using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("Miscellaneous/SDAnimatedImage")]
public class SDAnimatedImage : MonoBehaviour
{
	public enum State
	{
		None,
		PreDecoding,
		Decoding,
		DecodingError,
		Playing,
		Paused
	}

	public enum LoadingIndicatorType
	{
		None,
		RoundedRect,
		Circle,
		Circles
	}

	public delegate void OnImageSizeReadyAction(Vector2 size);

	public delegate void OnDecodingErrorAction();

	public delegate void OnLoadingErrorAction(SDWebImageDownloaderError error);

	public string imageURL;

	public Texture2D placeholderImage;

	public bool preserveAspect;

	public bool autoDownload = true;

	public bool autoPlay = true;

	public bool loop = true;

	public bool memoryCache;

	public bool diskCache = true;

	public bool showLoadingIndicator = true;

	public GameObject loadingIndicator;

	public LoadingIndicatorType loadingIndicatorType;

	public float loadingIndicatorScale = 1f;

	public Color loadingIndicatorColor = Color.black;

	private Texture2D animatedImageTexture;

	private Component _targetComponent;

	private int _targetMaterial;

	private GifDecoder decoder;

	private bool firstFrameShown;

	private List<GifDecoder.GifFrame> framesCache;

	private GifDecoder.GifFrame currentFrame;

	private bool frameIsReady;

	private int currentFrameIndex;

	private float currentFrameRemainingTime;

	private readonly object locker = new object();

	private Thread decoderThread;

	private bool threadIsRunning;

	private readonly AutoResetEvent autoResetEvent = new AutoResetEvent(initialState: false);

	public State state { get; private set; }

	public event OnImageSizeReadyAction OnImageSizeReady;

	public event OnDecodingErrorAction OnDecodingError;

	public event OnLoadingErrorAction OnLoadingError;

	private void Start()
	{
		if (autoDownload)
		{
			SetAnimatedImage();
		}
	}

	private void Update()
	{
		TrackAnimatedImageDecoding();
	}

	private void OnApplicationQuit()
	{
		TerminateDecoderThread();
	}

	public void SetAnimatedImage()
	{
		if (!string.IsNullOrEmpty(imageURL))
		{
			SetAnimatedImageWithURL(imageURL, placeholderImage);
		}
	}

	public void SetAnimatedImageWithURL(string url)
	{
		InternalSetAnimatedImageWithURL(url, null, GetSDWebImageOptions(), null);
	}

	public void SetAnimatedImageWithURL(string url, Texture2D placeholder)
	{
		InternalSetAnimatedImageWithURL(url, placeholder, GetSDWebImageOptions(), null);
	}

	public void SetAnimatedImageWithURL(string url, Texture2D placeholder, SDWebImageOptions options)
	{
		InternalSetAnimatedImageWithURL(url, placeholder, options, null);
	}

	public void SetAnimatedImageWithURL(string url, Texture2D placeholder, Action<float> progressCallback)
	{
		InternalSetAnimatedImageWithURL(url, placeholder, GetSDWebImageOptions(), progressCallback);
	}

	public void SetAnimatedImageWithURL(string url, Texture2D placeholder, SDWebImageOptions options, Action<float> progressCallback)
	{
		InternalSetAnimatedImageWithURL(url, placeholder, options, progressCallback);
	}

	public void Play()
	{
		if (state == State.Paused)
		{
			state = State.Playing;
		}
	}

	public void Pause()
	{
		if (state == State.Playing)
		{
			state = State.Paused;
		}
	}

	private void CacheGifFrame(GifDecoder.GifFrame frame)
	{
		byte[] array = new byte[frame.data.Length];
		Buffer.BlockCopy(frame.data, 0, array, 0, frame.data.Length);
		frame.data = array;
		lock (framesCache)
		{
			framesCache.Add(frame);
		}
	}

	private void StartDecoder()
	{
		ReadNextFrame();
		state = State.Paused;
		if (OnImageSizeReady != null)
		{
			OnImageSizeReady(new Vector2(decoder.GetImageWidth(), decoder.GetImageHeight()));
		}
		if (autoPlay)
		{
			Play();
		}
	}

	private void UpdateFrameTime()
	{
		if (state == State.Playing)
		{
			currentFrameRemainingTime -= Time.deltaTime;
		}
	}

	private void UpdateFrame()
	{
		if (decoder.TotalNumberOfFrames > 0 && decoder.TotalNumberOfFrames == currentFrameIndex)
		{
			currentFrameIndex = 0;
			if (!loop)
			{
				Pause();
				return;
			}
		}
		if (loop)
		{
			lock (framesCache)
			{
				currentFrame = ((framesCache.Count > currentFrameIndex) ? framesCache[currentFrameIndex] : decoder.GetCurrentFrame());
			}
			if (!decoder.AllFramesDecoded)
			{
				ReadNextFrame();
			}
		}
		else
		{
			currentFrame = decoder.GetCurrentFrame();
		}
		UpdateTexture();
		currentFrameRemainingTime = currentFrame.delay;
		currentFrameIndex++;
		if (!loop)
		{
			ReadNextFrame();
		}
	}

	private void TrackAnimatedImageDecoding()
	{
		if ((state != State.Playing && firstFrameShown) || !frameIsReady || state == State.Decoding)
		{
			return;
		}
		if (!firstFrameShown)
		{
			SetTexture();
			lock (locker)
			{
				UpdateFrame();
			}
			firstFrameShown = true;
			return;
		}
		UpdateFrameTime();
		if (currentFrameRemainingTime > 0f)
		{
			return;
		}
		lock (locker)
		{
			UpdateFrame();
		}
	}

	private void UpdateTexture()
	{
		animatedImageTexture.LoadRawTextureData(currentFrame.data);
		animatedImageTexture.Apply();
	}

	private void ReadNextFrame()
	{
		frameIsReady = false;
		autoResetEvent.Set();
	}

	private void StartDecoderThread()
	{
		if (decoderThread == null)
		{
			threadIsRunning = true;
			decoderThread = new Thread(DecodeAnimatedImageData);
			decoderThread.Name = "Gif_Decoder_" + decoderThread.ManagedThreadId;
			decoderThread.IsBackground = true;
			decoderThread.Start();
		}
	}

	private void TerminateDecoderThread()
	{
		if (threadIsRunning)
		{
			threadIsRunning = false;
			autoResetEvent.Set();
		}
	}

	private void DecodeAnimatedImageData()
	{
		autoResetEvent.WaitOne();
		while (threadIsRunning)
		{
			lock (locker)
			{
				decoder.ReadContents(!loop);
				if (loop && decoder.AllFramesDecoded)
				{
					frameIsReady = true;
					break;
				}
				if (loop)
				{
					CacheGifFrame(decoder.GetCurrentFrame());
				}
				frameIsReady = true;
			}
			autoResetEvent.WaitOne();
		}
		threadIsRunning = false;
		decoderThread = null;
	}

	private void Init()
	{
		_targetComponent = GetTargetComponent();
		decoder = new GifDecoder();
		currentFrameIndex = 0;
		firstFrameShown = false;
		frameIsReady = false;
		StartDecoderThread();
		if ((bool)loadingIndicator)
		{
			loadingIndicator.SetActive(value: false);
		}
		if (loop)
		{
			framesCache = new List<GifDecoder.GifFrame>();
		}
	}

	private void InternalSetAnimatedImageWithURL(string url, Texture2D placeholder, SDWebImageOptions options, Action<float> progressCallback)
	{
		if (placeholder != null)
		{
			animatedImageTexture = placeholder;
			SetTexture();
		}
		if (string.IsNullOrEmpty(url))
		{
			Debug.LogWarning("Image url is't set");
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
				return;
			}
			lock (locker)
			{
				Init();
				if (decoder.Read(new MemoryStream(imageData)) == GifDecoder.Status.Ok)
				{
					state = State.PreDecoding;
					CreateTargetTexture();
					StartDecoder();
				}
				else
				{
					state = State.DecodingError;
					Debug.LogWarning("Error decoding gif");
					if (OnDecodingError != null)
					{
						OnDecodingError();
					}
				}
			}
		});
	}

	private Component GetTargetComponent()
	{
		return GetComponents<Component>().FirstOrDefault((Component component) => component is Renderer || component is RawImage || component is Image);
	}

	private void SetTexture()
	{
		if (_targetComponent == null)
		{
			return;
		}
		if (_targetComponent is SpriteRenderer)
		{
			SpriteRenderer obj = (SpriteRenderer)_targetComponent;
			Vector2 size = obj.size;
			Sprite sprite = Sprite.Create(animatedImageTexture, new Rect(0f, 0f, animatedImageTexture.width, animatedImageTexture.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
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
					renderer.sharedMaterials[_targetMaterial].mainTexture = animatedImageTexture;
				}
				else
				{
					renderer.sharedMaterial.mainTexture = animatedImageTexture;
				}
			}
		}
		else if (_targetComponent is RawImage)
		{
			((RawImage)_targetComponent).texture = animatedImageTexture;
		}
		else if (_targetComponent is Image)
		{
			Image obj2 = (Image)_targetComponent;
			Sprite sprite2 = Sprite.Create(animatedImageTexture, new Rect(0f, 0f, animatedImageTexture.width, animatedImageTexture.height), new Vector2(0f, 0f));
			obj2.preserveAspect = preserveAspect;
			obj2.sprite = sprite2;
		}
	}

	private void CreateTargetTexture()
	{
		if (animatedImageTexture != null && decoder != null && animatedImageTexture.width == decoder.GetImageWidth() && animatedImageTexture.height == decoder.GetImageHeight())
		{
			return;
		}
		if (decoder == null || decoder.GetImageWidth() == 0 || decoder.GetImageWidth() == 0)
		{
			animatedImageTexture = Texture2D.blackTexture;
			return;
		}
		if (animatedImageTexture != null && animatedImageTexture.hideFlags == HideFlags.HideAndDontSave)
		{
			UnityEngine.Object.DestroyImmediate(animatedImageTexture);
		}
		animatedImageTexture = CreateTexture(decoder.GetImageWidth(), decoder.GetImageHeight());
		animatedImageTexture.hideFlags = HideFlags.HideAndDontSave;
	}

	private static Texture2D CreateTexture(int width, int height)
	{
		return new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
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
}
