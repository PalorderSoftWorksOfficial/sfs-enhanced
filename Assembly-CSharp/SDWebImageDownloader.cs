using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class SDWebImageDownloader : Singleton<SDWebImageDownloader>
{
	private int maxConcurrentDownloads = 6;

	private OrderedDictionary operationsQueue = new OrderedDictionary();

	public void DownloadImageWithURL(string url, int objectId, Action<float> progressCallback, Action<SDWebImageDownloaderError, byte[]> completionCallback)
	{
		RemoveOldInstanceOperationsFromQueue(objectId);
		SDWebImageDownloaderOperation[] operationsWithPredicate = GetOperationsWithPredicate((SDWebImageDownloaderOperation sDWebImageDownloaderOperation) => sDWebImageDownloaderOperation.url == url);
		if (operationsWithPredicate.Length != 0)
		{
			((SDWebImageDownloaderOperation)operationsQueue[operationsWithPredicate[0].url]).AddCallBacks(objectId, progressCallback, completionCallback);
			return;
		}
		SDWebImageDownloaderOperation operation = new SDWebImageDownloaderOperation(url, objectId, InternalDownloadImageWithURL(url), progressCallback, completionCallback);
		AddOperationToQueue(operation);
	}

	private IEnumerator InternalDownloadImageWithURL(string url)
	{
		Action<SDWebImageDownloaderError, byte[]> EndDownloadOperation = delegate(SDWebImageDownloaderError error, byte[] data)
		{
			if (error != null || data != null)
			{
				((SDWebImageDownloaderOperation)operationsQueue[url]).CallCompletionCallbacks(error, data);
			}
			if (((SDWebImageDownloaderOperation)operationsQueue[url]).downloadProgressTrackingOperation != null)
			{
				StopCoroutine(((SDWebImageDownloaderOperation)operationsQueue[url]).downloadProgressTrackingOperation);
				((SDWebImageDownloaderOperation)operationsQueue[url]).downloadProgressTrackingOperation = null;
			}
			((SDWebImageDownloaderOperation)operationsQueue[url]).finished = true;
			HandleOperationsQueueUpdate();
		};
		if (!IsURLValid(url))
		{
			SDWebImageDownloaderError arg = new SDWebImageDownloaderError(SDWebImageDownloaderError.ErrorType.InvalidURL);
			EndDownloadOperation(arg, null);
			yield break;
		}
		if (Application.internetReachability == NetworkReachability.NotReachable)
		{
			SDWebImageDownloaderError arg2 = new SDWebImageDownloaderError(SDWebImageDownloaderError.ErrorType.NoInternet);
			EndDownloadOperation(arg2, null);
			yield break;
		}
		using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
		{
			www.timeout = 30;
			((SDWebImageDownloaderOperation)operationsQueue[url]).downloadProgressTrackingOperation = TrackDownloadProgress(www);
			StartCoroutine(((SDWebImageDownloaderOperation)operationsQueue[url]).downloadProgressTrackingOperation);
			yield return www.SendWebRequest();
			if (!string.IsNullOrEmpty(www.error))
			{
				SDWebImageDownloaderError arg3 = new SDWebImageDownloaderError(SDWebImageDownloaderError.ErrorType.Unknown, www.error);
				EndDownloadOperation(arg3, null);
				yield break;
			}
			Texture content = DownloadHandlerTexture.GetContent(www);
			GifDecoder gifDecoder = new GifDecoder();
			if ((content == null || (content.width == 8 && content.height == 8)) && gifDecoder.Read(new MemoryStream(www.downloadHandler.data)) != GifDecoder.Status.Ok)
			{
				SDWebImageDownloaderError arg4 = new SDWebImageDownloaderError(SDWebImageDownloaderError.ErrorType.FailedURL);
				EndDownloadOperation(arg4, null);
				yield break;
			}
			if (www.isDone && www.downloadHandler.data != null)
			{
				((SDWebImageDownloaderOperation)operationsQueue[url]).CallProgressCallbacks(1f);
				EndDownloadOperation(null, www.downloadHandler.data);
				yield break;
			}
		}
		EndDownloadOperation(null, null);
	}

	private IEnumerator TrackDownloadProgress(UnityWebRequest www)
	{
		while (!www.isDone)
		{
			if (www.downloadProgress >= 0f && www.downloadProgress < 1f)
			{
				((SDWebImageDownloaderOperation)operationsQueue[www.url]).CallProgressCallbacks(www.downloadProgress);
			}
			yield return new WaitForSeconds(0.1f);
		}
	}

	private bool IsURLValid(string url)
	{
		if (!string.IsNullOrEmpty(url))
		{
			return url.Substring(0, 4) == "http";
		}
		return false;
	}

	private void RemoveOldInstanceOperationsFromQueue(int id)
	{
		SDWebImageDownloaderOperation[] operationsWithPredicate = GetOperationsWithPredicate((SDWebImageDownloaderOperation operation) => operation.IsIdIncluded(id));
		if (operationsWithPredicate.Length == 0)
		{
			return;
		}
		SDWebImageDownloaderOperation[] array = operationsWithPredicate;
		foreach (SDWebImageDownloaderOperation sDWebImageDownloaderOperation in array)
		{
			((SDWebImageDownloaderOperation)operationsQueue[sDWebImageDownloaderOperation.url]).RemoveCallbacks(id);
			if (!((SDWebImageDownloaderOperation)operationsQueue[sDWebImageDownloaderOperation.url]).IsValid())
			{
				RemoveOperationFromQueue(sDWebImageDownloaderOperation.url);
			}
		}
	}

	private void AddOperationToQueue(SDWebImageDownloaderOperation operation)
	{
		operationsQueue.Add(operation.url, operation);
		HandleOperationsQueueUpdate();
	}

	private void RemoveOperationFromQueue(string url)
	{
		if (operationsQueue.Contains(url))
		{
			if (((SDWebImageDownloaderOperation)operationsQueue[url]).downloadOperation != null)
			{
				StopCoroutine(((SDWebImageDownloaderOperation)operationsQueue[url]).downloadOperation);
				((SDWebImageDownloaderOperation)operationsQueue[url]).downloadOperation = null;
			}
			operationsQueue.Remove(url);
			HandleOperationsQueueUpdate();
		}
	}

	private void HandleOperationsQueueUpdate()
	{
		if (operationsQueue.Count <= 0)
		{
			return;
		}
		SDWebImageDownloaderOperation[] operationsWithPredicate = GetOperationsWithPredicate((SDWebImageDownloaderOperation operation) => operation.finished);
		foreach (SDWebImageDownloaderOperation sDWebImageDownloaderOperation in operationsWithPredicate)
		{
			RemoveOperationFromQueue(sDWebImageDownloaderOperation.url);
		}
		int num2 = GetOperationsWithPredicate((SDWebImageDownloaderOperation operation) => !operation.running).Length;
		int num3 = GetOperationsWithPredicate((SDWebImageDownloaderOperation operation) => operation.running).Length;
		if (num2 <= 0 || num3 >= maxConcurrentDownloads)
		{
			return;
		}
		int num4 = maxConcurrentDownloads - num3;
		for (int num5 = 0; num5 < operationsQueue.Count; num5++)
		{
			if (!((SDWebImageDownloaderOperation)operationsQueue[num5]).running)
			{
				((SDWebImageDownloaderOperation)operationsQueue[num5]).running = true;
				StartCoroutine(((SDWebImageDownloaderOperation)operationsQueue[num5]).downloadOperation);
				if (--num4 <= 0)
				{
					break;
				}
			}
		}
	}

	private SDWebImageDownloaderOperation[] GetOperationsWithPredicate(Predicate<SDWebImageDownloaderOperation> predicate)
	{
		SDWebImageDownloaderOperation[] array = new SDWebImageDownloaderOperation[operationsQueue.Count];
		operationsQueue.Values.CopyTo(array, 0);
		return Array.FindAll(array, predicate);
	}
}
