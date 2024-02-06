using System.Collections.Generic;
using System.Threading.Tasks;
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

    public async Task FetchAll()
    {
        // Exibir ui de carregamento do processo todo

        List<Task> tasks = new();

        // foreach (var table in LungSettings.instance.projects[0].tables)
        // {
        //     tasks.Add(FetchTable(table.id));
        // }

        await Task.WhenAll(tasks);

        // remover ui de carregamento
    }


    public async Task FetchTable(int id)
    {
        // Exibir ui de carregando dados

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

        // remover ui de carregamento e Exibir barra de progresso

        // Traduzir as entradas em unity localization string table


        // Remover barra de progresso
    }
}