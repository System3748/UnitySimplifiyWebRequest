using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public enum RequestMethod
{
	POST,
	GET
}

[Serializable]
public class RequestInfo
{
	public float FakeDemoSec = -1f;
	public RequestMethod Method;
	public string URI;

	[SerializeField]
	[NamedArray("key,value")]
	private string[] _headerRef;
	private Dictionary<string, string> _headers;
	public Dictionary<string, string> Headers
	{
		get
		{
			if (_headers == null)
			{
				_headers = SetHeader(_headerRef);
			}

			return _headers;
		}
	}

	private static Dictionary<string, string> SetHeader(string[] values)
	{
		if (values == null && values.Length <= 0)
		{
			return null;
		}

		Dictionary<string, string> headers = new Dictionary<string, string>();

		foreach (var value in values)
		{
			var pairs = value.Split(',');

			headers.Add(pairs[0], pairs.Length > 1 ? pairs[1] : string.Empty);
		}

		return headers;
	}
}

public static class WebRequest
{
	public static IEnumerator Send(RequestInfo info, WWWForm formData = null,
		Action<long, string> onResp = null,
		Action<float> onProgress = null)
	{
		if (info.FakeDemoSec>0)
		{
			Debug.LogWarningFormat("[WebRequest.Send] FakeDemo @ Api info:{0}", info.ToJson());

			yield return FakeApiExcuted(info.FakeDemoSec, onProgress);

			onResp?.Invoke((int)ApiHttpCode.操作成功, "");
			yield break;
		}

		switch (info.Method)
		{
			case RequestMethod.GET:
			default:
				yield return Get(info.URI, info.Headers, onResp, onProgress);
				break;

			case RequestMethod.POST:
				yield return Post(info.URI, formData, info.Headers, onResp, onProgress);
				break;
		}
	}

	/// <summary>
	/// 針對Post請求做流程處理
	/// </summary>
	/// <param name="uri">請求資源的Uri</param>
	/// <param name="formData">附加的資料表單</param>
	/// <param name="headers">附加的封包表頭</param>
	/// <param name="onResp">事件回應時的處理</param>
	/// <param name="onProgress">請求過程中的進度顯示</param>
	/// <returns></returns>
	public static IEnumerator Post(string uri, WWWForm formData, Dictionary<string, string> headers = null,
		Action<long, string> onResp = null,
		Action<float> onProgress = null)
	{
		UnityWebRequest webRequest = UnityWebRequest.Post(uri, formData);
		if (headers != null)
		{
			var header = headers.GetEnumerator();

			while (header.MoveNext())
			{
				var curr = header.Current;
				webRequest.SetRequestHeader(curr.Key, curr.Value);
			}
		}

		if (onProgress == null)
		{
			yield return webRequest.SendWebRequest();
		}
		else
		{
			webRequest.SendWebRequest();
			while (!webRequest.isDone)
			{
				yield return null;
				onProgress.Invoke(webRequest.uploadProgress);
			}
		}

		if (webRequest.isNetworkError || webRequest.isHttpError)
		{
			onResp?.Invoke(webRequest.responseCode, webRequest.error);
		}
		else
		{
			onResp?.Invoke(webRequest.responseCode, webRequest.downloadHandler.text);
		}
	}
	public static IEnumerator Get(string uri, Dictionary<string, string> headers = null,
		Action<long, string> onResp = null,
		Action<float> onProgress = null)
	{
		UnityWebRequest webRequest = UnityWebRequest.Get(uri);

		SetHeader(ref webRequest, headers);

		if (onProgress == null)
		{
			yield return webRequest.SendWebRequest();
		}
		else
		{
			webRequest.SendWebRequest();

			while (!webRequest.isDone)
			{
				yield return null;
				onProgress.Invoke(webRequest.downloadProgress);
			}
		}

		if (webRequest.isNetworkError || webRequest.isHttpError)
		{
			onResp?.Invoke(webRequest.responseCode, webRequest.error);
		}
		else
		{
			onResp?.Invoke(webRequest.responseCode, webRequest.downloadHandler.text);
		}
	}

	private static IEnumerator FakeApiExcuted(float demoSec, Action<float> onProgress = null)
	{
		float fakeExcutedSec = demoSec;
		float fakeLoading = 0;

		while (fakeLoading <= fakeExcutedSec)
		{
			yield return null;
			fakeLoading += Time.deltaTime;
			onProgress?.Invoke(fakeLoading / fakeExcutedSec);
		}
	}
	private static void SetHeader(ref UnityWebRequest webRequest, Dictionary<string, string> headers)
	{
		if (headers == null)
		{
			return;
		}

		var header = headers.GetEnumerator();

		while (header.MoveNext())
		{
			var curr = header.Current;
			webRequest.SetRequestHeader(curr.Key, curr.Value);
		}
	}
}
