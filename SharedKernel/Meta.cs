using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace SharedKernel
{
    public class Meta
    {
        public static DateTime Now => DateTime.UtcNow;
        public static string ServicePrincipalClient => "ServicePrincipalClient";
        public static string MergeJson(string destination, string source)
        {
            JObject destinationObj = JObject.Parse(destination);
            destinationObj.Merge(JObject.Parse(source),
                            new JsonMergeSettings
                            {
                                MergeNullValueHandling = MergeNullValueHandling.Merge,
                                MergeArrayHandling = MergeArrayHandling.Replace,
                            });
            return JsonConvert.SerializeObject(destinationObj)!;
        }        
    }
}
