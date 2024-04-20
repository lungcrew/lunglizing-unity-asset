using System;
using System.Collections.Generic;
using Lungfetcher.Data;
using Lungfetcher.Editor.Scriptables;
using Lungfetcher.Helper;
using UnityEngine;

namespace Lungfetcher.Editor.Helper
{
    [Serializable]
    public class LongTablesSoDictionary : UnitySerializedDictionary<long, TableSoList>
    {
    }
}