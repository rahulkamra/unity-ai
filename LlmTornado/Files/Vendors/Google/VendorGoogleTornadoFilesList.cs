using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Files.Vendors.Google
{
    internal class VendorGoogleTornadoFilesList
    {
        [JsonProperty("files")] 
        public List<VendorGoogleTornadoFileContent> Files { get; set; } = new List<VendorGoogleTornadoFileContent>();

        [JsonProperty("pageToken")]
        public string? PageToken { get; set; }
    }
}
