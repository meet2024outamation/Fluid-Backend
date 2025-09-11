using System.Globalization;
using System.Text;

namespace SharedKernel.Models
{
    public sealed class HeadersCollection
    {
        private readonly IDictionary<string, string> _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string this[string name]
        {
            get
            {
                if (_values.TryGetValue(name, out var value))
                {
                    return value;
                }

                return null;
            }
            set
            {
                if (value != null)
                {
                    _values[name] = value;
                }
                else if (_values.ContainsKey(name))
                {
                    _values.Remove(name);
                }
            }
        }

        public int Count => _values.Count;

        public ICollection<string> Keys => _values.Keys;

        public string CacheControl
        {
            get
            {
                return this["Cache-Control"];
            }
            set
            {
                this["Cache-Control"] = value;
            }
        }

        public string ContentDisposition
        {
            get
            {
                return this["Content-Disposition"];
            }
            set
            {
                this["Content-Disposition"] = value;
            }
        }

        public string ContentEncoding
        {
            get
            {
                return this["Content-Encoding"];
            }
            set
            {
                this["Content-Encoding"] = value;
            }
        }

        public long ContentLength
        {
            get
            {
                string text = this["Content-Length"];
                if (string.IsNullOrEmpty(text))
                {
                    return -1L;
                }

                return long.Parse(text, CultureInfo.InvariantCulture);
            }
            set
            {
                this["Content-Length"] = value.ToString(CultureInfo.InvariantCulture);
            }
        }

        public string ContentMD5
        {
            get
            {
                return this["Content-MD5"];
            }
            set
            {
                this["Content-MD5"] = value;
            }
        }

        public string ContentType
        {
            get
            {
                return this["Content-Type"];
            }
            set
            {
                this["Content-Type"] = value;
            }
        }

        public DateTime? ExpiresUtc
        {
            get
            {
                if (this["Expires"] == null)
                {
                    return null;
                }

                return DateTime.Parse(this["Expires"], CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
            }
            set
            {
                if (!value.HasValue)
                {
                    this["Expires"] = null;
                }

                this["Expires"] = value.GetValueOrDefault().ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss \\G\\M\\T", CultureInfo.InvariantCulture);
            }
        }

        [Obsolete("Setting this property results in non-UTC DateTimes not being marshalled correctly. Use ExpiresUtc instead.", false)]
        public DateTime? Expires
        {
            get
            {
                return ExpiresUtc?.ToLocalTime();
            }
            set
            {
                ExpiresUtc = ((!value.HasValue) ? null : new DateTime?(new DateTime(value.Value.Ticks, DateTimeKind.Utc)));
            }
        }

        internal bool IsSetContentType()
        {
            return !string.IsNullOrEmpty(ContentType);
        }
    }

    public sealed class ParameterCollection
    {
        private IDictionary<string, string> values = new Dictionary<string, string>();

        /// <summary>
        /// Returns a string representation of the ParameterCollection in the format "param1=value1&param2=value2".
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new();

            foreach (var key in values.Keys)
            {
                sb.Append($"{key}={values[key]}&");
            }

            if (sb.Length > 0)
            {
                sb.Length--;
            }

            return sb.ToString();
        }

        public string this[string name]
        {
            get
            {
                if (!name.StartsWith("x-", StringComparison.OrdinalIgnoreCase))
                {
                    name = "x-" + name;
                }

                if (values.TryGetValue(name, out var value))
                {
                    return value;
                }

                return null;
            }
            set
            {
                if (!name.StartsWith("x-", StringComparison.OrdinalIgnoreCase))
                {
                    name = "x-" + name;
                }

                values[name] = value;
            }
        }

        public int Count => values.Count;

        public ICollection<string> Keys => values.Keys;

        public void Add(string name, string value)
        {
            this[name] = value;
        }
    }

    public sealed class MetadataCollection
    {
        internal const string MetaDataHeaderPrefix = "x-ss-meta-";

        private IDictionary<string, string> values = new Dictionary<string, string>();

        public string this[string name]
        {
            get
            {
                if (!name.StartsWith(MetaDataHeaderPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    name = MetaDataHeaderPrefix + name;
                }

                if (values.TryGetValue(name, out var value))
                {
                    return value;
                }

                return null;
            }
            set
            {
                if (!name.StartsWith(MetaDataHeaderPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    name = MetaDataHeaderPrefix + name;
                }

                values[name] = value;
            }
        }

        public int Count => values.Count;

        public ICollection<string> Keys => values.Keys;

        public void Add(string name, string value)
        {
            this[name] = value;
        }

        public string Get(string name)
        {
            return this[name];
        }

        public void Clear()
        {
            foreach (string item in values.Keys.ToList())
            {
                if (item.StartsWith(MetaDataHeaderPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    values.Remove(item);
                }
            }
        }
    }

    public class ResponseHeaderOverrides
    {
        internal const string RESPONSE_CONTENT_TYPE = "response-content-type";

        internal const string RESPONSE_CONTENT_LANGUAGE = "response-content-language";

        internal const string RESPONSE_EXPIRES = "response-expires";

        internal const string RESPONSE_CACHE_CONTROL = "response-cache-control";

        internal const string RESPONSE_CONTENT_DISPOSITION = "response-content-disposition";

        internal const string RESPONSE_CONTENT_ENCODING = "response-content-encoding";

        private string _contentType = "binary/octet-stream";

        private string _contentLanguage;

        private string _expires;

        private string _cacheControl;

        private string _contentDisposition;

        private string _contentEncoding;

        public string ContentType
        {
            get
            {
                return _contentType;
            }
            set
            {
                _contentType = value;
            }
        }

        public string ContentLanguage
        {
            get
            {
                return _contentLanguage;
            }
            set
            {
                _contentLanguage = value;
            }
        }

        public string Expires
        {
            get
            {
                return _expires;
            }
            set
            {
                _expires = value;
            }
        }

        public string CacheControl
        {
            get
            {
                return _cacheControl;
            }
            set
            {
                _cacheControl = value;
            }
        }

        public string ContentDisposition
        {
            get
            {
                return _contentDisposition;
            }
            set
            {
                _contentDisposition = value;
            }
        }

        public string ContentEncoding
        {
            get
            {
                return _contentEncoding;
            }
            set
            {
                _contentEncoding = value;
            }
        }
    }

    public enum Protocol
    {
        HTTPS,

        HTTP
    }

    public enum HttpVerb
    {
        GET,

        HEAD,

        PUT,

        DELETE
    }

    public enum SigningAlgorithm
    {
        HmacSHA1,
        HmacSHA256
    }
}
