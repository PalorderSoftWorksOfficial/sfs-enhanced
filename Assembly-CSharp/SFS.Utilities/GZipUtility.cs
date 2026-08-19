using System.IO;
using System.IO.Compression;

namespace SFS.Utilities;

public static class GZipUtility
{
	public static byte[] Compress(byte[] data)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress);
		gZipStream.Write(data, 0, data.Length);
		gZipStream.Flush();
		gZipStream.Close();
		return memoryStream.ToArray();
	}

	public static byte[] Decompress(byte[] data)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using MemoryStream stream = new MemoryStream(data);
		using GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress);
		gZipStream.CopyTo(memoryStream);
		return memoryStream.ToArray();
	}
}
