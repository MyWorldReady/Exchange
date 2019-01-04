
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using LuaInterface;
using System.IO;
using System.Collections;

class PlatformUtilAndroid : PlatformUtil {

	#if UNITY_ANDROID
	private AndroidJavaObject javaObject;
#endif

    private Action<string> openPWebViewCallback;
    private Action<string> openWebViewCallback;

    public PlatformUtilAndroid() {
		#if UNITY_ANDROID
		using (AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
		javaObject = jc.GetStatic<AndroidJavaObject>("currentActivity");
		}

		// -- 打开相册功能
		saveHeadFileName = "clip_temp.jpg";
		saveHeadFolder = System.IO.Path.Combine(Application.persistentDataPath, "Head");
		if (!Directory.Exists(saveHeadFolder)) {
		Directory.CreateDirectory(saveHeadFolder);
		}
        PrintTools.Log("CreateDirectory saveHeadFolder result:" + Directory.Exists(saveHeadFolder));
		SetPlayerHeadImgSaveMsgIE();
		#endif
	}


	private void JavaCall(string javaMethodName, params object[] args) {
		#if UNITY_ANDROID
		try {
		javaObject.Call(javaMethodName, args);
		} catch (Exception e) {
        PrintTools.LogWarning(string.Format("调用java方法{0}出现异常:{1}", javaMethodName, e.Message));
		}
		#endif
	}


	private ReturnType JavaCall<ReturnType>(string javaMethodName, params object[] args) {
		#if UNITY_ANDROID
		try {
		return javaObject.Call<ReturnType>(javaMethodName, args);
		} catch (Exception e) {
        PrintTools.LogWarning(string.Format("调用java方法{0}出现异常:{1}", javaMethodName, e.Message));
		return default(ReturnType);
		}
		#else
		return default(ReturnType);
		#endif
	}


	/// <summary>
	/// 更新（下载并安装）整包
	/// </summary>
	public override void UpdateFullPackage(string pkgUrl,string apkName) {
		JavaCall("updateApk", pkgUrl, apkName);
	}


    /// <summary>
    /// 打开需要跳转app的webView
    /// </summary>
    /// <param name="url"></param>
    /// <param name="sign">打开app的最终地址的标志（匹配字符串开头）</param>
    /// <param name="viewRect"></param>
    /// <param name="completeCallback"></param>
    public override void OpenPWebView(string url,string sign, Rect viewRect, Action<string> completeCallback) {
        openPWebViewCallback = completeCallback;
        // 给url地址附加一个时间戳，避免由于缓存导致更新不及时
        StringBuilder sb = StringBuilderCache.Acquire();
		sb.Append(url);
		sb.Append(url.Contains("?") ? "&sbtimestamp=" : "?sbtimestamp=");
		sb.Append(DateTime.Now.ToString("yyyymmddhhmmss"));
		string webUrl = StringBuilderCache.GetStringAndRelease(sb);
        float rightOffset = Screen.width - viewRect.x - viewRect.width;
        float bottomOffset = Screen.height - viewRect.y - viewRect.height;
        //JavaCall("openWebView", webUrl, (int)viewRect.xMin, (int)viewRect.yMin, (int)rightOffset, (int)bottomOffset);
        JavaCall("openPWebView", webUrl, sign,(int)viewRect.xMin, (int)viewRect.yMin, (int)rightOffset, (int)bottomOffset);

    }
    public override void OpenPWebViewResutl(string json){
        if (openPWebViewCallback != null) {
            openPWebViewCallback(json);
        }
    }

	/// <summary>
	/// 关闭平台的WebView
	/// </summary>
	public override void ClosePWebView() {
		JavaCall("closePWebView");
    }



    public override void OpenWebView(string url, Rect viewRect, Action<string> completeCallback) {
        openWebViewCallback = completeCallback;
        // 给url地址附加一个时间戳，避免由于缓存导致更新不及时
        StringBuilder sb = StringBuilderCache.Acquire();
        sb.Append(url);
        sb.Append(url.Contains("?") ? "&sbtimestamp=" : "?sbtimestamp=");
        sb.Append(DateTime.Now.ToString("yyyymmddhhmmss"));
        string webUrl = StringBuilderCache.GetStringAndRelease(sb);
        float rightOffset = Screen.width - viewRect.x - viewRect.width;
        float bottomOffset = Screen.height - viewRect.y - viewRect.height;
        JavaCall("openWebView", webUrl, (int)viewRect.xMin, (int)viewRect.yMin, (int)rightOffset, (int)bottomOffset);
    }
    public override void OpenWebViewResutl(string json) {
        if (openWebViewCallback != null) {
            openWebViewCallback(json);
        }
    }

    public override void CloseWebView() {
        JavaCall("closeWebView");
    }


    public override void OpenP2WebView(string url, Action<string> completeCallback){PrintTools.LogError("TODO");}
    public override void OpenP2WebViewResutl(string json){PrintTools.LogError("TODO");}
    public override void CloseP2WebView(){PrintTools.LogError("TODO");}


        /// <summary>
        /// 复制文本到剪切板
        /// </summary>
    public override void CopyToClipBoard(string content, Action<bool> completeCallback) {
		/*copyToClipBoardCallback = completeCallback;
            JavaCall("copyToClipboard", content);*/

		try {
			AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
			AndroidJavaObject currContent = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
			AndroidJavaObject clipboardMgr = currContent.Call<AndroidJavaObject>("getSystemService", "clipboard");
			AndroidJavaClass clipData = new AndroidJavaClass("android.content.ClipData");
			AndroidJavaObject clipDataResult = clipData.CallStatic<AndroidJavaObject>("newPlainText", "data", content);
			clipboardMgr.Call("setPrimaryClip", clipDataResult);
			if (completeCallback != null)
				completeCallback(true);
		} catch {
			if (completeCallback != null)
				completeCallback(false);
		}
	}

    /// <summary>
    /// 获取剪贴板里的内容
    /// </summary>
    public override string GetClipboardContent(int maxLen) {
        return JavaCall<string>("GetClipboardContent", maxLen);
    }
    /// <summary>
    /// 获取app的版本号
    /// </summary>
    /// <returns></returns>
    public override int GetAppVersionCode() {
		return JavaCall<int>("getAppVersionCode");
	}


	/// <summary>
	/// 获取app的版本名
	/// </summary>
	/// <returns></returns>
	public override string GetAppVersionName() {
		return JavaCall<string>("getAppVersionName");
	}

	// ------------------------------------------------------------------------
	private void SetPlayerHeadImgSaveMsgIE() {
		using (AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
			AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
			jo.Call("SetPlayerHeadImgMsg", saveHeadFolder, saveHeadFileName);
		}
	}

	//打开头像选择方式菜单
	public override void OpenHeadSelectMenu(Action<bool, string> callback) {
		using (AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
			AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
			jo.Call("OpenHeadSelectMenu");
		}
		setHeadFromPhoneAction = callback;
	}

	/// <summary>
	/// 打开头像选择方式菜单的回调
	/// </summary>
	/// <returns></returns>
	public override void OnHeadSelectMenuCallback(string result) {
        PrintTools.Log("OnHeadSelectMenuCallback=" + result);
		if (setHeadFromPhoneAction != null)
			setHeadFromPhoneAction(System.Boolean.Parse(result), System.IO.Path.Combine(saveHeadFolder, saveHeadFileName));
	}

	public override string GetPackageName()
	{
		return JavaCall<string>("getAppPackageName");
	}

	public override string GetChannelId()
	{
		return JavaCall<string>("getChannelId");
	}

	/// <summary>
	/// 获取手机硬件型号 如iphone8 iphonex
	/// </summary>
	/// <returns>The phone hard ward type.</returns>
	public override string GetPhoneHardWardType (){
		return string.Empty;
	}


    /// <summary>
    /// 获取手机的mac地址
    /// </summary>
    /// <returns></returns>
    public override string GetMac() {
        return SystemInfo.deviceUniqueIdentifier;
    }




    public override string GetSystemTime(string format) {
        return JavaCall<string>("GetSystemTime",format);
    }


}
