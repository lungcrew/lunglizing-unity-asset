using System;
using Lungfetcher.Data;
using UnityEngine;

namespace Lungfetcher.Helper
{
    [Serializable]
    public class LongScriptableObjectDictionary : UnitySerializedDictionary<long, ScriptableObject>
    {
    }

    [Serializable]
    public class LongEntryDictionary : UnitySerializedDictionary<long, Entry>
    {
    }
}
