using System;
using Lungfetcher.Editor.Scriptables;
using Lungfetcher.Helper;

namespace Lungfetcher.Editor.Helper
{
    [Serializable]
    public class LongContainersSoDictionary : UnitySerializedDictionary<long, ContainerSoList>
    {
    }
}