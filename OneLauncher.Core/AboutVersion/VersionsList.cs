using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace OneLauncher.Core
{
    public class VersionsList
    {
        VersionJsonInfo a;
        public VersionsList(string Json)
        {
            try
            {
                a = JsonSerializer.Deserialize<VersionJsonInfo>(Json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"解析版本列表Json时出错");
            }
        }
        public List<VersionBasicInfo> GetReleaseVersionList()
        {
            List<VersionBasicInfo> a = new List<VersionBasicInfo>();
            foreach (var i in this.a.AllVersions)
            {
                if (i.Type == "release")
                    a.Add(new VersionBasicInfo(i.Id, i.Type, i.Url, i.Time));
            }
            return a;
        }
        protected class VersionJsonInfo
        {
            [JsonPropertyName("latest")]
            public LatestList latest { get; set; }
            [JsonPropertyName("versions")]
            public List<AllVersionInfomations> AllVersions { get; set; }
        }
        protected class LatestList : VersionJsonInfo
        {
            [JsonPropertyName("release")]
            public string release { get; set; }
            [JsonPropertyName("snapshot")]
            public string snapshot { get; set; }
        }
        protected class AllVersionInfomations : VersionJsonInfo
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }
            [JsonPropertyName("type")]
            public string Type { get; set; }
            [JsonPropertyName("url")]
            public string Url { get; set; }
            [JsonPropertyName("releaseTime")]
            public DateTime Time { get; set; }

        }
    }
}
