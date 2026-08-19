using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

public class SDImageCache : Singleton<SDImageCache>
{
	private long maxCacheSize;

	private int maxCacheAge = 7;

	private string cacheDirectoryPath;

	private Dictionary<string, byte[]> memoryCache;

	protected override void Awake()
	{
		base.Awake();
		Init();
		Singleton<MainThread>.instance.Init();
		ThreadPool.QueueUserWorkItem(delegate
		{
			DeleteOldFilesOnDisk();
		});
	}

	private void Init()
	{
		Application.lowMemory += OnLowMemory;
		memoryCache = new Dictionary<string, byte[]>();
		cacheDirectoryPath = Path.Combine(Application.persistentDataPath, "SDWebImage");
		if (!Directory.Exists(cacheDirectoryPath))
		{
			Directory.CreateDirectory(cacheDirectoryPath);
		}
	}

	private void OnLowMemory()
	{
		ThreadPool.QueueUserWorkItem(delegate
		{
			ClearMemoryCache();
		});
	}

	public void QueryImageDataFromCacheForURL(string url, SDWebImageOptions options, Action<byte[]> callback)
	{
		if ((options & SDWebImageOptions.MemoryCache) != SDWebImageOptions.None && ImageDataExistsInMemory(url))
		{
			byte[] array = LoadImageDataFromMemory(url);
			if (array != null)
			{
				callback(array);
				return;
			}
		}
		if ((options & SDWebImageOptions.DiskCache) != SDWebImageOptions.None && ImageDataExistsInDisk(url))
		{
			ThreadPool.QueueUserWorkItem(delegate
			{
				byte[] data = LoadImageDataFromDisk(url);
				if ((options & SDWebImageOptions.MemoryCache) != SDWebImageOptions.None)
				{
					StoreImageDataInMemory(url, data);
				}
				Singleton<MainThread>.instance.Execute(delegate
				{
					if (data != null)
					{
						callback(data);
					}
					else
					{
						callback(null);
					}
				});
			});
		}
		else
		{
			callback(null);
		}
	}

	public void CacheImageDataForURL(string url, byte[] data, SDWebImageOptions options)
	{
		if ((options & SDWebImageOptions.MemoryCache) != SDWebImageOptions.None)
		{
			ThreadPool.QueueUserWorkItem(delegate
			{
				StoreImageDataInMemory(url, data);
			});
		}
		if ((options & SDWebImageOptions.DiskCache) != SDWebImageOptions.None)
		{
			ThreadPool.QueueUserWorkItem(delegate
			{
				StoreImageDataInDisk(url, data);
			});
		}
	}

	public void RemoveImageDataFromCache(string url, SDWebImageOptions options)
	{
		if ((options & SDWebImageOptions.MemoryCache) != SDWebImageOptions.None)
		{
			ThreadPool.QueueUserWorkItem(delegate
			{
				RemoveImageDataFromMemory(url);
			});
		}
		if ((options & SDWebImageOptions.DiskCache) != SDWebImageOptions.None)
		{
			ThreadPool.QueueUserWorkItem(delegate
			{
				RemoveImageDataFromDisk(url);
			});
		}
	}

	private bool ImageDataExistsInMemory(string url)
	{
		lock (memoryCache)
		{
			return memoryCache.ContainsKey(url);
		}
	}

	private void StoreImageDataInMemory(string url, byte[] data)
	{
		lock (memoryCache)
		{
			memoryCache.Add(url, data);
		}
	}

	private byte[] LoadImageDataFromMemory(string url)
	{
		lock (memoryCache)
		{
			return memoryCache.ContainsKey(url) ? memoryCache[url] : null;
		}
	}

	private void RemoveImageDataFromMemory(string url)
	{
		lock (memoryCache)
		{
			if (memoryCache.ContainsKey(url))
			{
				memoryCache.Remove(url);
			}
		}
	}

	private void ClearMemoryCache()
	{
		lock (memoryCache)
		{
			memoryCache.Clear();
		}
	}

	private bool ImageDataExistsInDisk(string url)
	{
		return File.Exists(PathForURL(url));
	}

	private void StoreImageDataInDisk(string url, byte[] data)
	{
		File.WriteAllBytes(PathForURL(url), data);
	}

	private byte[] LoadImageDataFromDisk(string url)
	{
		string path = PathForURL(url);
		if (File.Exists(path))
		{
			return File.ReadAllBytes(path);
		}
		return null;
	}

	private void RemoveImageDataFromDisk(string url)
	{
		if (ImageDataExistsInDisk(url))
		{
			File.Delete(PathForURL(url));
		}
	}

	private void DeleteOldFilesOnDisk()
	{
		if (maxCacheAge == 0 && maxCacheSize == 0L)
		{
			return;
		}
		FileInfo[] files = new DirectoryInfo(cacheDirectoryPath).GetFiles("*.*");
		DateTime t = DateTime.Now.AddDays(-maxCacheAge);
		List<string> list = new List<string>();
		long num = 0L;
		List<FileInfo> list2 = new List<FileInfo>();
		FileInfo[] array = files;
		foreach (FileInfo fileInfo in array)
		{
			if (maxCacheAge > 0 && DateTime.Compare(fileInfo.LastAccessTime, t) < 0)
			{
				list.Add(fileInfo.Name);
				continue;
			}
			num += fileInfo.Length;
			list2.Add(fileInfo);
		}
		foreach (string item in list)
		{
			File.Delete(PathForFilename(item));
		}
		if (maxCacheSize <= 0 || num <= maxCacheSize)
		{
			return;
		}
		long num2 = maxCacheSize / 2;
		foreach (FileInfo item2 in list2.OrderByDescending((FileInfo f) => f.LastWriteTime).ToList())
		{
			File.Delete(PathForFilename(item2.Name));
			num -= item2.Length;
			if (num < num2)
			{
				break;
			}
		}
	}

	private string PathForFilename(string filename)
	{
		return Path.Combine(cacheDirectoryPath, filename);
	}

	private string PathForURL(string url)
	{
		return Path.Combine(cacheDirectoryPath, FilenameForURL(url));
	}

	private string FilenameForURL(string url)
	{
		Match match = Regex.Match((!string.IsNullOrEmpty(Path.GetExtension(url))) ? Path.GetExtension(url) : ".img", "(\\.\\w+)");
		return Md5(url) + Path.GetExtension(match.Value);
	}

	private string Md5(string url)
	{
		MD5CryptoServiceProvider mD5CryptoServiceProvider = new MD5CryptoServiceProvider();
		byte[] bytes = Encoding.UTF8.GetBytes(url);
		bytes = mD5CryptoServiceProvider.ComputeHash(bytes);
		string text = "";
		byte[] array = bytes;
		foreach (byte b in array)
		{
			text += b.ToString("x2").ToLower();
		}
		return text;
	}
}
