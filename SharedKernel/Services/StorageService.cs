using Amazon.Runtime;
using Amazon.Runtime.Internal.Auth;
using SharedKernel.Models;
using System.Globalization;
using System.Text;

namespace SharedKernel.Services
{
    public class StorageServiceClient(string accessKeyId, string accessKeySecret)
    {
        private readonly string _accessKeyId = accessKeyId;
        private readonly string _accessKeySecret = accessKeySecret;
        public static readonly byte[] TerminatorBytes = Encoding.UTF8.GetBytes("ss_presign_request");

        public string GetPreSignedURL(GetPreSignedUrlRequest request)
        {
            if (String.IsNullOrEmpty(_accessKeyId) || String.IsNullOrEmpty(_accessKeySecret))
            {
                throw new Exception("Credentials must be specified, cannot call method anonymously");
            }

            if (request == null)
            {
                throw new ArgumentNullException("request", "The PreSignedUrlRequest specified is null!");
            }

            StringBuilder pathBuilder = new("/");
            if (!string.IsNullOrEmpty(request.UrlPath))
            {
                pathBuilder.Append(request.UrlPath);
            }
            if (!string.IsNullOrEmpty(request.BucketName))
            {
                if (pathBuilder.Length > 1)
                {
                    pathBuilder.Append('/');
                }
                pathBuilder.Append(request.BucketName);
            }
            if (!string.IsNullOrEmpty(request.Key))
            {
                if (pathBuilder.Length > 1)
                {
                    pathBuilder.Append('/');
                }

                pathBuilder.Append(request.Key);
            }

            long secondsRemaining = Convert.ToInt64((request.Expires.ToUniversalTime() - DateTime.Now.ToUniversalTime()).TotalSeconds);

            DateTime signedAt = DateTime.Now.ToUniversalTime();

            HeadersCollection headers = request.Headers;
            StringBuilder requiredHeadersInResponse = new("");
            foreach (string key in headers.Keys)
            {
                requiredHeadersInResponse.Append(Extensions.ConvertToLowerCaseWithHyphens(key));
                requiredHeadersInResponse.Append(';');
            }
            foreach (string key in request.Metadata.Keys)
            {
                requiredHeadersInResponse.Append(key);
                requiredHeadersInResponse.Append(';');

            }
            if (!string.IsNullOrEmpty(requiredHeadersInResponse.ToString()))
            {
                requiredHeadersInResponse?.ToString().TrimEnd(';');
            }

            var signedHeaders = requiredHeadersInResponse!.ToString();

            request.Parameters.Add("x-ss-expires", secondsRemaining.ToString());
            request.Parameters.Add("x-ss-credential", _accessKeyId);
            request.Parameters.Add("x-ss-date", Extensions.FormatDateTime(signedAt, "yyyyMMddTHHmmssZ"));
            request.Parameters.Add("x-ss-signedheaders", signedHeaders);

            UriBuilder uriBuilder = new()
            {
                Scheme = request.Protocol.ToString(),
                Host = request.HostName,
                Path = pathBuilder.ToString(),
                Query = request.Parameters.ToString(),
                Port = request.Port
            };

            var canonicalRequest = $"{pathBuilder}/{request.Parameters}";
            if (!string.IsNullOrEmpty(request.UrlPath))
            {
                canonicalRequest = canonicalRequest.Replace("/" + request.UrlPath, "");
            }
            var signingResult = AWS4Signer.ComputeSignature(_accessKeyId, _accessKeySecret, "global", signedAt, "ss", signedHeaders, canonicalRequest);

            request.Parameters.Add("x-ss-signature", signingResult.Signature);
            uriBuilder.Query = request.Parameters.ToString();

            return uriBuilder.Uri.ToString();
        }

    }
    public class GetPreSignedUrlRequest : AmazonWebServiceRequest
    {
        private string hostName = string.Empty;

        private string bucketName = string.Empty;

        private DateTime expires;

        private string key = string.Empty;

        private Protocol protocol = Protocol.HTTPS;

        private HttpVerb verb = HttpVerb.GET;
        
        private string versionId = string.Empty;

        private HeadersCollection headersCollection = new();

        private MetadataCollection metadataCollection = new();

        private ParameterCollection parameterCollection = new();

        private int port = -1;

        private string urlPath = string.Empty;

        //
        // Summary:
        //     The name of the bucket to create a pre-signed url to, or containing the object.
        public string BucketName
        {
            get
            {
                return bucketName;
            }
            set
            {
                bucketName = value;
            }
        }


        //
        // Summary:
        //     The name of the Host to create a pre-signed url.
        public string HostName
        {
            get
            {
                return hostName;
            }
            set
            {
                hostName = value;
            }
        }

        //
        // Summary:
        //     The key to the object for which a pre-signed url should be created.
        //
        // Remarks:
        //     This property will be used as part of the resource path of the HTTP request.
        //     In .NET the System.Uri class is used to construct the uri for the request. The
        //     System.Uri class will canonicalize the uri string by compacting characters like
        //     "..". /// For example an object key of "foo/../bar/file.txt" will be transformed
        //     into "bar/file.txt" because the ".." is interpreted as use parent directory.
        //     For further information view the documentation for the Uri class: https://docs.microsoft.com/en-us/dotnet/api/system.uri
        public string Key
        {
            get
            {
                return key;
            }
            set
            {
                key = value;
            }
        }


        // Summary:
        //     A standard MIME type describing the format of the object data.
        //
        // Remarks:
        //     The content type for the content being uploaded. This property defaults to "binary/octet-stream".
        //     For more information, refer to: http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.17.
        //
        //
        //     Note that if content type is specified, it should also be included in the HttpRequest
        //     headers of the eventual upload request, otherwise a signature error may result.
        public string ContentType
        {
            get
            {
                return headersCollection.ContentType;
            }
            set
            {
                headersCollection.ContentType = value;
            }
        }

        //
        // Summary:
        //     The expiry date and time for the pre-signed url.
        public DateTime Expires
        {
            get
            {
                return expires;
            }
            set
            {
                expires = value;
            }
        }


        //
        // Summary:
        //     The requested protocol (http/https) for the pre-signed url.
        //
        // Remarks:
        //     Defaults to https.
        public Protocol Protocol
        {
            get
            {
                return protocol;
            }
            set
            {
                protocol = value;
            }
        }

        //
        // Summary:
        //     The verb for the pre-signed url.
        //
        // Remarks:
        //     Accepted verbs are GET, PUT, DELETE and HEAD. Default is GET.
        public HttpVerb Verb
        {
            get
            {
                return verb;
            }
            set
            {
                verb = value;
            }
        }

        //
        // Summary:
        //     The collection of headers for the request.
        public HeadersCollection Headers
        {
            get
            {
                headersCollection ??= new HeadersCollection();

                return headersCollection;
            }
            internal set
            {
                headersCollection = value;
            }
        }

        //
        // Summary:
        //     The collection of meta data for the request.
        public MetadataCollection Metadata
        {
            get
            {
                metadataCollection ??= new MetadataCollection();

                return metadataCollection;
            }
            internal set
            {
                metadataCollection = value;
            }
        }

        //
        // Summary:
        //     Custom parameters to include in the signed request, so that they are tamper-proof.
        public ParameterCollection Parameters
        {
            get
            {
                parameterCollection ??= new ParameterCollection();

                return parameterCollection;
            }
            internal set
            {
                parameterCollection = value;
            }
        }

        //
        // Summary:
        //     Checks if FolderName property is set.
        //
        // Returns:
        //     true if FolderName property is set.
        internal bool IsSetFolderName()
        {
            return !string.IsNullOrEmpty(bucketName);
        }

        //
        // Summary:
        //     Checks if Key property is set.
        //
        // Returns:
        //     true if Key property is set.
        internal bool IsSetKey()
        {
            return !string.IsNullOrEmpty(key);
        }

        //
        // Summary:
        //     Checks if Host property is set.
        //
        // Returns:
        //     true if Host property is set.
        public bool IsSetHost()
        {
            return !string.IsNullOrEmpty(hostName);
        }

        /// <summary>
        /// Path to append after hostname
        /// </summary>
        public string UrlPath
        {
            get
            {
                return urlPath;
            }
            set
            {
                urlPath = value;
            }
        }

        /// <summary>
        /// Port to pass in case of internal Url
        /// </summary>
        public int Port
        {
            get
            {
                return port;
            }
            set
            {
                port = value;
            }
        }
    }
}
