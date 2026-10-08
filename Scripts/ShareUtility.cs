using UnityEngine;

// 시스템 공유 창 호출 (안드로이드: 설치된 SNS·메신저 목록이 뜸)
public static class ShareUtility
{
    public static void ShareText(string text, string chooserTitle)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent"))
            using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent"))
            {
                intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                intent.Call<AndroidJavaObject>("setType", "text/plain");
                intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);

                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, chooserTitle))
                {
                    activity.Call("startActivity", chooser);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Share] 공유 실패: " + e.Message);
        }
#else
        // 에디터에서는 공유 창 대신 클립보드에 복사하고 로그로 확인
        GUIUtility.systemCopyBuffer = text;
        Debug.Log("[Share] 클립보드에 복사됨:\n" + text);
#endif
    }
}