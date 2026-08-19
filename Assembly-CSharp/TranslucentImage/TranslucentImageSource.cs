using UnityEngine;

namespace TranslucentImage;

[ExecuteInEditMode]
public class TranslucentImageSource : MonoBehaviour
{
	public bool onetime;

	private bool update = true;

	public float maxUpdateRate = float.PositiveInfinity;

	[Tooltip("Preview the effect on entire screen")]
	public bool preview;

	[SerializeField]
	private float size = 5f;

	[SerializeField]
	private int iteration = 4;

	[SerializeField]
	private Rect blurRegion = new Rect(0f, 0f, 1f, 1f);

	private Rect lastBlurRegion = new Rect(0f, 0f, 1f, 1f);

	[SerializeField]
	private int maxDepth = 4;

	[SerializeField]
	private int downsample;

	[SerializeField]
	private int lastDownsample;

	[SerializeField]
	private float strength;

	private float lastUpdate;

	public Camera camera;

	private Shader shader;

	private Material material;

	private Material previewMaterial;

	private static int _sizePropId;

	private static int _cropRegionPropId;

	public RenderTexture BlurredScreen { get; private set; }

	private Camera Cam
	{
		get
		{
			if (!camera)
			{
				return camera = GetComponent<Camera>();
			}
			return camera;
		}
	}

	public float Strength
	{
		get
		{
			return strength = Size * Mathf.Pow(2f, Iteration + Downsample);
		}
		set
		{
			strength = Mathf.Max(0f, value);
			SetAdvancedFieldFromSimple();
		}
	}

	public float Size
	{
		get
		{
			return size;
		}
		set
		{
			size = Mathf.Max(0f, value);
		}
	}

	public int Iteration
	{
		get
		{
			return iteration;
		}
		set
		{
			iteration = Mathf.Max(0, value);
		}
	}

	public int Downsample
	{
		get
		{
			return downsample;
		}
		set
		{
			downsample = Mathf.Max(0, value);
		}
	}

	public Rect BlurRegion
	{
		get
		{
			return blurRegion;
		}
		set
		{
			Vector2 vector = new Vector2(1f / (float)Cam.pixelWidth, 1f / (float)Cam.pixelHeight);
			value.x = Mathf.Clamp(value.x, 0f, 1f - vector.x);
			value.y = Mathf.Clamp(value.y, 0f, 1f - vector.y);
			value.width = Mathf.Clamp(value.width, vector.x, 1f - value.x);
			value.height = Mathf.Clamp(value.height, vector.y, 1f - value.y);
			blurRegion = value;
		}
	}

	public int MaxDepth
	{
		get
		{
			return maxDepth;
		}
		set
		{
			maxDepth = Mathf.Max(1, value);
		}
	}

	private float ScreenSize => (float)Mathf.Min(Cam.pixelWidth, Cam.pixelHeight) / 1080f;

	private float MinUpdateCycle
	{
		get
		{
			if (!(maxUpdateRate > 0f))
			{
				return float.PositiveInfinity;
			}
			return 1f / maxUpdateRate;
		}
	}

	protected virtual void SetAdvancedFieldFromSimple()
	{
		Size = strength / Mathf.Pow(2f, Iteration + Downsample);
		if (Size < 1f)
		{
			if (Downsample > 0)
			{
				Downsample--;
				Size *= 2f;
			}
			else if (Iteration > 0)
			{
				Iteration--;
				Size *= 2f;
			}
		}
		while (Size > 8f)
		{
			Size /= 2f;
			Iteration++;
		}
	}

	protected virtual void Start()
	{
		camera = Cam;
		shader = Shader.Find("Hidden/EfficientBlur");
		if (!shader.isSupported)
		{
			base.enabled = false;
		}
		material = new Material(shader);
		previewMaterial = new Material(Shader.Find("Hidden/FillCrop"));
		_sizePropId = Shader.PropertyToID("size");
		_cropRegionPropId = Shader.PropertyToID("_CropRegion");
		CreateNewBlurredScreen();
		lastDownsample = Downsample;
	}

	private void OnDestroy()
	{
		Object.DestroyImmediate(material);
		Object.DestroyImmediate(previewMaterial);
		Object.DestroyImmediate(BlurredScreen);
	}

	protected virtual void CreateNewBlurredScreen()
	{
		BlurredScreen = new RenderTexture(Mathf.RoundToInt((float)Cam.pixelWidth * BlurRegion.width) >> Downsample, Mathf.RoundToInt((float)Cam.pixelHeight * BlurRegion.height) >> Downsample, 0)
		{
			filterMode = FilterMode.Bilinear
		};
	}

	protected virtual void ProgressiveResampling(int level, ref RenderTexture target)
	{
		level = Mathf.Min(level + Downsample, MaxDepth);
		int width = BlurredScreen.width >> level;
		int height = BlurredScreen.height >> level;
		RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0, BlurredScreen.format);
		temporary.filterMode = FilterMode.Bilinear;
		Graphics.Blit(target, temporary, material, 0);
		RenderTexture.ReleaseTemporary(target);
		target = temporary;
	}

	protected virtual void ProgressiveBlur(RenderTexture sourceRt)
	{
		if (Downsample != lastDownsample || !BlurRegion.Equals(lastBlurRegion))
		{
			CreateNewBlurredScreen();
			lastDownsample = Downsample;
			lastBlurRegion = BlurRegion;
		}
		if (BlurredScreen.IsCreated())
		{
			BlurredScreen.DiscardContents();
		}
		material.SetFloat(_sizePropId, Size * ScreenSize);
		int num = ((iteration > 0) ? 1 : 0);
		int width = BlurredScreen.width >> num;
		int height = BlurredScreen.height >> num;
		RenderTexture target = RenderTexture.GetTemporary(width, height, 0, sourceRt.format);
		target.filterMode = FilterMode.Bilinear;
		sourceRt.filterMode = FilterMode.Bilinear;
		material.SetVector(_cropRegionPropId, new Vector4(BlurRegion.xMin, BlurRegion.yMin, BlurRegion.xMax, BlurRegion.yMax));
		Graphics.Blit(sourceRt, target, material, 1);
		for (int i = 2; i <= iteration; i++)
		{
			ProgressiveResampling(i, ref target);
		}
		for (int num2 = iteration - 1; num2 >= 1; num2--)
		{
			ProgressiveResampling(num2, ref target);
		}
		Graphics.Blit(target, BlurredScreen, material, 0);
		RenderTexture.ReleaseTemporary(target);
	}

	protected virtual void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (Time.unscaledTime - lastUpdate >= MinUpdateCycle)
		{
			if (update)
			{
				ProgressiveBlur(source);
			}
			if (onetime)
			{
				update = false;
			}
			lastUpdate = Time.unscaledTime;
		}
		if (preview)
		{
			previewMaterial.SetVector("_CropRegion", new Vector4(BlurRegion.xMin, BlurRegion.yMin, BlurRegion.xMax, BlurRegion.yMax));
			Graphics.Blit(BlurredScreen, destination, previewMaterial);
		}
		else
		{
			Graphics.Blit(source, destination);
		}
	}
}
