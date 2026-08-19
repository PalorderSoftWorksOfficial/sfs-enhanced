using System;
using System.IO;

public class GifDecoder
{
	public enum Status
	{
		Ok,
		FormatError,
		OpenError
	}

	public class GifFrame
	{
		public byte[] data;

		public float delay;

		public GifFrame(byte[] data, float delay)
		{
			this.data = data;
			this.delay = delay;
		}
	}

	private Stream inStream;

	private Status status;

	private int width;

	private int height;

	private bool gctFlag;

	private int gctSize;

	private int loopCount = 1;

	private int[] gct;

	private int[] lct;

	private int[] act;

	private int bgIndex;

	private int bgColor;

	private int lastBgColor;

	private bool lctFlag;

	private bool interlace;

	private int lctSize;

	private int ix;

	private int iy;

	private int iw;

	private int ih;

	private int lrx;

	private int lry;

	private int lrw;

	private int lrh;

	private int[] image;

	private byte[] bitmap;

	private readonly byte[] block = new byte[256];

	private int blockSize;

	private int dispose;

	private int lastDispose;

	private bool transparency;

	private float delay;

	private int transIndex;

	private long imageDataOffset;

	private const int MaxStackSize = 4096;

	private short[] prefix;

	private byte[] suffix;

	private byte[] pixelStack;

	private byte[] pixels;

	private GifFrame currentFrame;

	private int frameCount;

	public int TotalNumberOfFrames { get; private set; }

	public bool AllFramesDecoded { get; private set; }

	public float GetCurrentFrameDelay()
	{
		return currentFrame.delay;
	}

	public int GetFrameCount()
	{
		return frameCount;
	}

	public GifFrame GetCurrentFrame()
	{
		return currentFrame;
	}

	public int GetLoopCount()
	{
		return loopCount;
	}

	public int GetImageWidth()
	{
		return width;
	}

	public int GetImageHeight()
	{
		return height;
	}

	public Status Read(Stream inStream)
	{
		Init();
		if (inStream != null)
		{
			this.inStream = inStream;
			ReadHeader();
			if (Error())
			{
				status = Status.FormatError;
			}
		}
		else
		{
			status = Status.OpenError;
		}
		return status;
	}

	public void Reset()
	{
		inStream.Position = 0L;
		Read(inStream);
	}

	public void Close()
	{
		inStream.Dispose();
	}

	public void ReadContents(bool loop)
	{
		while (!Error())
		{
			switch (Read())
			{
			case 44:
				ReadImage();
				return;
			case 33:
				switch (Read())
				{
				case 249:
					ReadGraphicControlExt();
					break;
				case 255:
				{
					ReadBlock();
					string text = "";
					for (int i = 0; i < 11; i++)
					{
						string text2 = text;
						char c = (char)block[i];
						text = text2 + c;
					}
					if (text.Equals("NETSCAPE2.0"))
					{
						ReadNetscapeExt();
					}
					else
					{
						Skip();
					}
					break;
				}
				default:
					Skip();
					break;
				}
				break;
			case 59:
				TotalNumberOfFrames = frameCount;
				if (loop)
				{
					ResetReader();
					break;
				}
				AllFramesDecoded = true;
				return;
			default:
				status = Status.FormatError;
				break;
			case 0:
				break;
			}
		}
	}

	private void ResetReader()
	{
		frameCount = 0;
		AllFramesDecoded = false;
		inStream.Position = imageDataOffset;
	}

	private void SetPixels()
	{
		if (lastDispose > 0 && frameCount - 1 > 0 && lastDispose == 2)
		{
			int num = ((!transparency) ? lastBgColor : 0);
			for (int i = 0; i < lrh; i++)
			{
				int num2 = i;
				num2 += lry;
				if (num2 < height)
				{
					int num3 = (height - num2 - 1) * width + lrx;
					int num4 = num3 + lrw;
					while (num3 < num4)
					{
						image[num3++] = num;
					}
				}
			}
		}
		int num5 = 1;
		int num6 = 8;
		int num7 = 0;
		for (int j = 0; j < ih; j++)
		{
			int num8 = j;
			if (interlace)
			{
				if (num7 >= ih)
				{
					num5++;
					switch (num5)
					{
					case 2:
						num7 = 4;
						break;
					case 3:
						num7 = 2;
						num6 = 4;
						break;
					case 4:
						num7 = 1;
						num6 = 2;
						break;
					}
				}
				num8 = num7;
				num7 += num6;
			}
			num8 += iy;
			if (num8 >= height)
			{
				continue;
			}
			int num9 = j * iw;
			int k = (height - num8 - 1) * width + ix;
			for (int num10 = k + iw; k < num10; k++)
			{
				int num11 = act[pixels[num9++] & 0xFF];
				if (num11 != 0)
				{
					image[k] = num11;
				}
			}
		}
	}

	private void DecodeImageData()
	{
		int num = iw * ih;
		if (pixels == null || pixels.Length < num)
		{
			pixels = new byte[num];
		}
		if (prefix == null)
		{
			prefix = new short[4096];
		}
		if (suffix == null)
		{
			suffix = new byte[4096];
		}
		if (pixelStack == null)
		{
			pixelStack = new byte[4097];
		}
		int num2 = Read();
		int num3 = 1 << num2;
		int num4 = num3 + 1;
		int num5 = num3 + 2;
		int num6 = -1;
		int num7 = num2 + 1;
		int num8 = (1 << num7) - 1;
		for (int i = 0; i < num3; i++)
		{
			prefix[i] = 0;
			suffix[i] = (byte)i;
		}
		int j;
		int num10;
		int num11;
		int num12;
		int num13;
		int num9 = (j = (num10 = (num11 = (num12 = (num13 = 0)))));
		int k = 0;
		while (k < num)
		{
			if (num12 == 0)
			{
				for (; j < num7; j += 8)
				{
					if (num10 == 0)
					{
						num10 = ReadBlock();
						num13 = 0;
					}
					num9 += (block[num13++] & 0xFF) << j;
					num10--;
				}
				int i = num9 & num8;
				num9 >>= num7;
				j -= num7;
				if (i > num5 || i == num4)
				{
					break;
				}
				if (i == num3)
				{
					num7 = num2 + 1;
					num8 = (1 << num7) - 1;
					num5 = num3 + 2;
					num6 = -1;
					continue;
				}
				if (num6 == -1)
				{
					pixelStack[num12++] = suffix[i];
					num6 = i;
					num11 = i;
					continue;
				}
				int num14 = i;
				if (i == num5)
				{
					pixelStack[num12++] = (byte)num11;
					i = num6;
				}
				while (i > num3)
				{
					pixelStack[num12++] = suffix[i];
					i = prefix[i];
				}
				num11 = suffix[i] & 0xFF;
				if (num5 >= 4096)
				{
					break;
				}
				pixelStack[num12++] = (byte)num11;
				prefix[num5] = (short)num6;
				suffix[num5] = (byte)num11;
				num5++;
				if ((num5 & num8) == 0 && num5 < 4096)
				{
					num7++;
					num8 += num5;
				}
				num6 = num14;
			}
			num12--;
			pixels[k++] = pixelStack[num12];
		}
		for (; k < num; k++)
		{
			pixels[k] = 0;
		}
	}

	private bool Error()
	{
		return status != Status.Ok;
	}

	private void Init()
	{
		status = Status.Ok;
		frameCount = 0;
		currentFrame = null;
		AllFramesDecoded = false;
		gct = null;
		lct = null;
	}

	private int Read()
	{
		int result = 0;
		try
		{
			result = inStream.ReadByte();
		}
		catch (IOException)
		{
			status = Status.FormatError;
		}
		return result;
	}

	private int ReadBlock()
	{
		blockSize = Read();
		int i = 0;
		if (blockSize <= 0)
		{
			return i;
		}
		try
		{
			int num;
			for (; i < blockSize; i += num)
			{
				num = inStream.Read(block, i, blockSize - i);
				if (num == -1)
				{
					break;
				}
			}
		}
		catch (IOException)
		{
		}
		if (i < blockSize)
		{
			status = Status.FormatError;
		}
		return i;
	}

	private int[] ReadColorTable(int ncolors)
	{
		int num = 3 * ncolors;
		int[] array = null;
		byte[] array2 = new byte[num];
		int num2 = 0;
		try
		{
			num2 = inStream.Read(array2, 0, array2.Length);
		}
		catch (IOException)
		{
		}
		if (num2 < num)
		{
			status = Status.FormatError;
		}
		else
		{
			array = new int[256];
			int num3 = 0;
			int num4 = 0;
			while (num3 < ncolors)
			{
				uint num5 = array2[num4++];
				uint num6 = (uint)(array2[num4++] & 0xFF);
				uint num7 = (uint)(array2[num4++] & 0xFF);
				array[num3++] = (int)(0xFF000000u | (num7 << 16) | (num6 << 8) | num5);
			}
		}
		return array;
	}

	private void ReadGraphicControlExt()
	{
		Read();
		int num = Read();
		dispose = (num & 0x1C) >> 2;
		if (dispose == 0)
		{
			dispose = 1;
		}
		transparency = (num & 1) != 0;
		delay = (float)ReadShort() / 100f;
		transIndex = Read();
		Read();
	}

	private void ReadHeader()
	{
		string text = "";
		for (int i = 0; i < 6; i++)
		{
			text += (char)Read();
		}
		if (!text.StartsWith("GIF", StringComparison.CurrentCulture))
		{
			status = Status.FormatError;
			return;
		}
		ReadLsd();
		if (gctFlag && !Error())
		{
			gct = ReadColorTable(gctSize);
			bgColor = gct[bgIndex];
		}
		imageDataOffset = inStream.Position;
	}

	private void ReadImage()
	{
		ix = ReadShort();
		iy = ReadShort();
		iw = ReadShort();
		ih = ReadShort();
		int num = Read();
		lctFlag = (num & 0x80) != 0;
		interlace = (num & 0x40) != 0;
		lctSize = 2 << (num & 7);
		if (lctFlag)
		{
			lct = ReadColorTable(lctSize);
			act = lct;
		}
		else
		{
			act = gct;
			if (bgIndex == transIndex)
			{
				bgColor = 0;
			}
		}
		int num2 = 0;
		if (transparency)
		{
			num2 = act[transIndex];
			act[transIndex] = 0;
		}
		if (act == null)
		{
			status = Status.FormatError;
		}
		if (Error())
		{
			return;
		}
		DecodeImageData();
		Skip();
		if (!Error())
		{
			if (image == null)
			{
				image = new int[width * height];
			}
			if (bitmap == null)
			{
				bitmap = new byte[width * height * 4];
			}
			SetPixels();
			Buffer.BlockCopy(image, 0, bitmap, 0, bitmap.Length);
			currentFrame = new GifFrame(bitmap, delay);
			frameCount++;
			if (transparency)
			{
				act[transIndex] = num2;
			}
			ResetFrame();
		}
	}

	private void ReadLsd()
	{
		width = ReadShort();
		height = ReadShort();
		int num = Read();
		gctFlag = (num & 0x80) != 0;
		gctSize = 2 << (num & 7);
		bgIndex = Read();
		Read();
	}

	private void ReadNetscapeExt()
	{
		do
		{
			ReadBlock();
			if (block[0] == 1)
			{
				int num = block[1] & 0xFF;
				int num2 = block[2] & 0xFF;
				loopCount = (num2 << 8) | num;
			}
		}
		while (blockSize > 0 && !Error());
	}

	private int ReadShort()
	{
		return Read() | (Read() << 8);
	}

	private void ResetFrame()
	{
		lastDispose = dispose;
		lrx = ix;
		lry = iy;
		lrw = iw;
		lrh = ih;
		lastBgColor = bgColor;
		lct = null;
	}

	private void Skip()
	{
		do
		{
			ReadBlock();
		}
		while (blockSize > 0 && !Error());
	}
}
