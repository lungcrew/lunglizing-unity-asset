using System.Diagnostics;

namespace Lungfetcher.Web
{
    public class LungResponse<T>
    {
        public T data;
        public LungResponseMeta meta;
    }

    public class LungResponseMeta
    {
        public long total;
    }
}