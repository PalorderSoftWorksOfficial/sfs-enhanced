using System;
using UnityEngine;

[Serializable]
public class PlanetData_Old
{
	[Serializable]
	public class BasicModule
	{
		public string name;

		public double radius = -1.0;

		public double gravity = -1.0;

		public double timewarpHeight = -1.0;

		public SerializableColor mapColor = new SerializableColor(Color.gray);

		public int mapResolution;
	}

	[Serializable]
	public class SerializableColor
	{
		public float r = -1f;

		public float g = -1f;

		public float b = -1f;

		public SerializableColor(Color c)
		{
			r = c.r;
			g = c.g;
			b = c.b;
		}
	}

	[Serializable]
	public struct MyVector2
	{
		public float x;

		public float y;

		public Vector2 ToVector2()
		{
			return new Vector2(x, y);
		}
	}

	[Serializable]
	public class TerrainModule_Old
	{
		[Serializable]
		public class DetailLevel_Old
		{
			public double loadDistance;

			public float verticeSize;
		}

		[Serializable]
		public class TerrainTexture_Old
		{
			[Space]
			public string planetTexture = "None";

			public float planetTextureCutout = -1f;

			[Space]
			public string surfaceTextureA = "None";

			public MyVector2 surfaceTextureSizeA = new MyVector2
			{
				x = -1f,
				y = -1f
			};

			[Space]
			public string surfaceTextureB = "None";

			public MyVector2 surfaceTextureSizeB = new MyVector2
			{
				x = -1f,
				y = -1f
			};

			[Space]
			public string terrainTexture = "None";

			public MyVector2 terrainTextureSize = new MyVector2
			{
				x = -1f,
				y = -1f
			};

			[Space]
			public float surfaceLayerSize = -1f;

			[Space]
			public float minFade = -1f;

			public float maxFade = -1f;

			[Space]
			public float shadowIntensity = -1f;

			public float shadowHeight = -1f;
		}

		[Space]
		public TerrainTexture_Old TERRAIN_TEXTURE_DATA;

		public string[] terrainFromula;

		public string[] textureFormula;

		[Space]
		public DetailLevel_Old[] DETAIL_LEVELS;
	}

	[Serializable]
	public class AtmosphereModule_Old
	{
		[Serializable]
		public class Physics_Old
		{
			public double height = -1.0;

			public double density = -1.0;

			public double curve = -1.0;
		}

		[Serializable]
		public class Gradient_Old
		{
			public int positionZ = -1;

			public double gradientHeight = -1.0;

			public string gradientTexture = "None";
		}

		[Serializable]
		public class Clouds_Old
		{
			public string cloudTexture = "None";

			public int startHeight = -1;

			public int height = -1;

			public float repeatX;

			public float alpha;

			public float cloudVelocity;
		}

		[Serializable]
		public class SerializableGradient_Old
		{
			[Serializable]
			public class Key
			{
				public float r = -1f;

				public float g = -1f;

				public float b = -1f;

				public float a = -1f;

				public float distance = -1f;

				public Color GetColor()
				{
					return new Color(r, g, b, a);
				}
			}

			public Key[] keys;
		}

		public Physics_Old PHYSICS;

		public Gradient_Old GRADIENT;

		public Clouds_Old CLOUDS;

		public SerializableGradient_Old FOG;
	}

	[Serializable]
	public class ColorGradingGradient_Old
	{
		[Serializable]
		public class Key
		{
			public float height;

			public float shadowIntensity = 1.65f;

			public float hueShift;

			public float saturation = 1f;

			public float contrast = 1.1f;

			public float red = 1f;

			public float green = 1f;

			public float blue = 1f;

			private void Default()
			{
				shadowIntensity = 1.65f;
				hueShift = 0f;
				saturation = 1f;
				contrast = 1.1f;
				red = 1f;
				green = 1f;
				blue = 1f;
			}
		}

		public Key[] keys;
	}

	[Serializable]
	public class OrbitModule_Old
	{
		public string parent;

		public double orbitHeight;

		public double eccentricity;

		public double argumentOfPeriapsis;

		public double multiplierSOI = 1.0;

		public int orbitLineResolution = 200;
	}

	[Space]
	public BasicModule BASE_DATA;

	[Space]
	public bool hasAtmosphere;

	public AtmosphereModule_Old ATMOSPHERE_DATA;

	[Space]
	public bool hasTerrain;

	public TerrainModule_Old TERRAIN_DATA;

	[Space]
	public bool hasPostProcessing;

	public ColorGradingGradient_Old POST_PROCESSING;

	[Space]
	public bool hasOrbitData;

	public OrbitModule_Old ORBIT_DATA;
}
