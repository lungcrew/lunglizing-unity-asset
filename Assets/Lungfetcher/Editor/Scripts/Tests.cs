using Lungfecther.Web;
using Lungfetcher.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class MyEditorWindow : EditorWindow
{
    [MenuItem("Tools/Go!")]
    public static void ShowExample()
    {
        LungSettings.instance.Test = Random.Range(0, 100);

        Debug.Log(LungSettings.instance.Test);
    }

    [MenuItem("Tools/Log!")]
    public static void Log()
    {
        Debug.Log(LungSettings.instance.Test);
    }

    [MenuItem("Tools/Fetch!")]
    public static async void Fetch()
    {
        // var request = WebRequest.To("http://localhost/api/v0/external/tables/");
        var request = WebRequest.To("http://localhost/api/v0/external/tables/87/entries");

        request.SetBearerAuth("QcS74OK.M4L77werD9BhsrGGPjixUgKwjVmFrfXQ");

        WebResponse response = await request.SendAsync();

        if (response.Success)
        {
            Debug.Log(response.Contents);
        }
        else
        {
            Debug.Log(response.HttpErrorMessage);
        }

    }
}