using System;
using System.Collections.Generic;
using System.Text;
using SFS.Analytics;
using SFS.Parsers.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace SFS.Sharing;

public class RequestUtil
{
	public enum InitResult
	{
		Failed,
		Success
	}

	private string _url;

	private string _userAgent;

	private string _token;

	public bool ready;

	public InitResult Initialize()
	{
		if (ready)
		{
			return InitResult.Success;
		}
		try
		{
			FetchRemotes();
			ready = true;
			return InitResult.Success;
		}
		catch (Exception message)
		{
			Debug.Log(message);
			return InitResult.Failed;
		}
	}

	public void SendRequest(UnityWebRequest request, Action<long, string> callback, int timeout = 120)
	{
		request.timeout = timeout;
		request.SendWebRequest().completed += delegate
		{
			callback(request.responseCode, request.downloadHandler.text);
		};
	}

	public UnityWebRequest PostRequest(string apiPath, params (string, object)[] data)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		for (int i = 0; i < data.Length; i++)
		{
			(string, object) tuple = data[i];
			string item = tuple.Item1;
			object item2 = tuple.Item2;
			dictionary[item] = item2;
		}
		UnityWebRequest unityWebRequest = UnityWebRequest.PostWwwForm(_url + apiPath, "");
		SetAgentHeader(unityWebRequest);
		UploadJson(unityWebRequest, JsonWrapper.ToJson(dictionary, pretty: false));
		return unityWebRequest;
	}

	public UnityWebRequest GetRequest(string apiPath)
	{
		UnityWebRequest unityWebRequest = UnityWebRequest.Get(_url + apiPath);
		SetAgentHeader(unityWebRequest);
		return unityWebRequest;
	}

	public UnityWebRequest AuthedGetRequest(string apiPath)
	{
		UnityWebRequest request = GetRequest(apiPath);
		request.SetRequestHeader("Login-token", _token);
		return request;
	}

	public UnityWebRequest AuthedPostRequest(string apiPath, params (string, object)[] data)
	{
		UnityWebRequest unityWebRequest = PostRequest(apiPath, data);
		unityWebRequest.SetRequestHeader("Login-token", _token);
		return unityWebRequest;
	}

	private void SetAgentHeader(UnityWebRequest webRequest)
	{
		webRequest.SetRequestHeader("User-agent", _userAgent);
	}

	private static void UploadJson(UnityWebRequest webRequest, string json)
	{
		webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
		webRequest.SetRequestHeader("Content-Type", "application/json");
	}

	private void FetchRemotes()
	{
		_url = FbRemoteSettings.GetString("sharingDomain", "https://sharing.spaceflightsimulator.app");
		_userAgent = FbRemoteSettings.GetString("sharingUserAgent", "s&1FS&xdkf2r5k9p2zU!PDYXW$bqae");
	}

	public void SetToken(string token)
	{
		if (!string.IsNullOrEmpty(token))
		{
			_token = token;
		}
	}
}
