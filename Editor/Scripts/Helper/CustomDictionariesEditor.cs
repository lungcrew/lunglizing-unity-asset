using System;
using System.Collections.Generic;
using Lungfetcher.Editor.Scriptables;
using Lungfetcher.Helper;

namespace Lungfetcher.Editor.Helper
{
    [Serializable]
    public class LongProjectSoDictionary : UnitySerializedDictionary<long, ProjectSo>
    {
    }
    
    [Serializable]
    public class LongTableSotSoDictionary : UnitySerializedDictionary<long, TableSo>
    {
    }
    
    [Serializable]
    public class LongTableRequestDictionary : UnitySerializedDictionary<long, RequestOperation>
    {
    }
    
    [Serializable]
    public class LongProjectRequestDictionary : UnitySerializedDictionary<long, List<RequestOperation>>
    {
    }

    [Serializable]
    public class ProjectUpdateRequestDictionary : UnitySerializedDictionary<ProjectSo, RequestOperation>
    {
    }
}