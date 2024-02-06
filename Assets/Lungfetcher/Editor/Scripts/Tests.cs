using System.Collections.Generic;
using System.Threading.Tasks;
using Lungfetcher.Data;
using Lungfetcher.Editor;
using Lungfetcher.Web;
using UnityEditor;
using UnityEngine;

public class MyEditorWindow : EditorWindow
{
    [MenuItem("Debug/Go!")]
    public static void ShowExample()
    {
        LungSettings.instance.Test = Random.Range(0, 100);

        Debug.Log(LungSettings.instance.Test);
    }

    [MenuItem("Debug/Log!")]
    public static void Log()
    {
        Debug.Log(LungSettings.instance.Test);
    }

    [MenuItem("Debug/Request Tables!")]
    public static async void Request()
    {
        LungRequest request = LungRequest.Create("tables", "QcS74OK.M4L77werD9BhsrGGPjixUgKwjVmFrfXQ");
        var response = await request.Fetch<List<Table>>();

        foreach (var table in response.data)
        {
            Debug.Log($"Table: {table.name}({table.id})");
        }
    }

    [MenuItem("Debug/Request Entries!")]
    public static async void RequestEntries()
    {
        LungRequest request = LungRequest.Create("tables/87/entries", "QcS74OK.M4L77werD9BhsrGGPjixUgKwjVmFrfXQ");
        var response = await request.Fetch<List<Entry>>();

        foreach (var entry in response.data)
        {
            Debug.Log($"------------ Entry {entry.uuid} ------------");
            foreach (var localization in entry.localizations)
            {
                Debug.Log($"{localization.locale.code}: {localization.text}");
            }
        }
    }

    [MenuItem("Debug/Request Info!")]
    public static async void RequestInfo()
    {
        LungRequest request = LungRequest.Create("info", "QcS74OK.M4L77werD9BhsrGGPjixUgKwjVmFrfXQ");
        var response = await request.Fetch<Project>();

        Debug.Log($"Title: {response.data.title}");
        Debug.Log($"Description: {response.data.description}");
        Debug.Log($"Mother Locale: {response.data.mother_locale.name}");
        Debug.Log("Locales: ");
        foreach (var locale in response.data.locales)
        {
            Debug.Log($"{locale.name} ({locale.code})");
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