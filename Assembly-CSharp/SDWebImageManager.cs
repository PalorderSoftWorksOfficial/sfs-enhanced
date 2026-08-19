using System;
using System.Collections.Generic;

public class SDWebImageManager : Singleton<SDWebImageManager>
{
	private HashSet<string> failedURLs;

	protected override void Awake()
	{
		base.Awake();
		failedURLs = new HashSet<string>();
	}

	public void LoadImageWithURL(string url, int objectId, SDWebImageOptions options, Action<float> progressCallback, Action<SDWebImageDownloaderError, byte[]> completionCallback)
	{
		if (failedURLs.Contains(url))
		{
			if (completionCallback != null)
			{
				completionCallback(new SDWebImageDownloaderError(SDWebImageDownloaderError.ErrorType.FailedURL), null);
			}
			return;
		}
		Singleton<SDImageCache>.instance.QueryImageDataFromCacheForURL(url, options, delegate(byte[] cachedData)
		{
			if (cachedData != null)
			{
				if (completionCallback != null)
				{
					completionCallback(null, cachedData);
				}
			}
			else
			{
				Singleton<SDWebImageDownloader>.instance.DownloadImageWithURL(url, objectId, progressCallback, delegate(SDWebImageDownloaderError error, byte[] downloadedData)
				{
					if (error != null)
					{
						if (error.type == SDWebImageDownloaderError.ErrorType.InvalidURL || error.type == SDWebImageDownloaderError.ErrorType.NotFound || error.type == SDWebImageDownloaderError.ErrorType.FailedURL)
						{
							failedURLs.Add(url);
						}
						if (completionCallback != null)
						{
							completionCallback(error, null);
						}
					}
					else if (downloadedData != null)
					{
						if (completionCallback != null)
						{
							completionCallback(null, downloadedData);
						}
						Singleton<SDImageCache>.instance.CacheImageDataForURL(url, downloadedData, options);
					}
				});
			}
		});
	}
}
