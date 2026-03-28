using System.Collections.Generic;

namespace Shared.Analytics
{
    public sealed class AnalyticsParams
    {
        readonly Dictionary<string, object> _data = new Dictionary<string, object>();

        public AnalyticsParams Add(string key, int value) { _data[key] = value; return this; }
        public AnalyticsParams Add(string key, float value) { _data[key] = value; return this; }
        public AnalyticsParams Add(string key, string value) { _data[key] = value; return this; }

        public IReadOnlyDictionary<string, object> Build() => _data;
    }
}
