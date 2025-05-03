using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OneLauncher.Core
{
    public class VersionInfomations
    {
        VersionsInformations a;
        public VersionInfomations(string Json)
        {
            try 
            { 
                a = JsonSerializer.Deserialize<VersionsInformations>(Json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"解析版本Json时出错");
            }
        }
        public List<SFNTD> GetLibrarys(string path)
        {
            List<SFNTD> SFNTDs = new List<SFNTD>();
            foreach (var i in a.Libraries)
            {
                SFNTDs.Add(new SFNTD(i.Downloads.Artifact.Url, i.Downloads.Artifact.Sha1, path+ ".minecraft/libraries/" + i.Downloads.Artifact.Path));
            }
            return SFNTDs;
        }
        public SFNTD GetMainFile(string path,string version)
        {
            return new SFNTD(a.Downloads.Client.Url, a.Downloads.Client.Sha1, $"{path}.minecraft/versions/{version}/{version}.jar");
        }
        public SFNTD GetAssets(string path)
        {
            return new SFNTD(a.AssetIndex.Url, a.AssetIndex.Sha1, $"{path}.minecraft/assets/indexes/{a.AssetIndex.Id}.json");
        }
        public string GetAssetIndexVersion()
        {
            return a.AssetIndex.Id;
        }
        protected class VersionsInformations
        {
            [JsonPropertyName("assetIndex")]
            public AssetIndex AssetIndex { get; set; }


            [JsonPropertyName("downloads")]
            public Downloads Downloads { get; set; }


            [JsonPropertyName("libraries")]
            public List<Library> Libraries { get; set; }
        }
        protected class Downloads : VersionsInformations
        {
            [JsonPropertyName("client")]
            public DownloadUrl Client { get; set; }
        }
        protected class DownloadUrl : Downloads
        {
            [JsonPropertyName("sha1")]
            public string Sha1 { get; set; }

            [JsonPropertyName("url")]
            public string Url { get; set; }
        }
        protected class Library : VersionsInformations
        {
            [JsonPropertyName("downloads")]
            public LibraryDownloads Downloads { get; set; }
        }
        protected class LibraryDownloads : Library
        {
            [JsonPropertyName("artifact")]
            public LibraryArtifact? Artifact { get; set; }
        }
        protected class LibraryArtifact : LibraryDownloads
        {
            [JsonPropertyName("path")]
            public string Path { get; set; }

            [JsonPropertyName("sha1")]
            public string Sha1 { get; set; }

            [JsonPropertyName("url")]
            public string Url { get; set; }
        }
        protected class AssetIndex
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("sha1")]
            public string Sha1 { get; set; }

            [JsonPropertyName("url")]
            public string Url { get; set; }
        }
    }
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
        public List<VersionBasicInfo> GetLatestVersionList()
        {
            List<VersionBasicInfo> a = new List<VersionBasicInfo>();
            foreach (var i in this.a.AllVersions)
            {
                if (i.Id == this.a.latest.snapshot)
                    a.Add(new VersionBasicInfo(i.Id, i.Type, i.Url, i.Time));
                else
                if (i.Id == this.a.latest.release)
                    a.Add(new VersionBasicInfo(i.Id, i.Type, i.Url, i.Time));
            }
            return a;
        }
        public List<VersionBasicInfo> GetAllVersionList()
        {
            List<VersionBasicInfo> a = new List<VersionBasicInfo>();
            foreach (var i in this.a.AllVersions)
            {
                a.Add(new VersionBasicInfo(i.Id, i.Type, i.Url, i.Time));
            }
            return a;
        }
        public List<VersionBasicInfo> GetReleaseVersionList()
        {
            List<VersionBasicInfo> a = new List<VersionBasicInfo>();
            foreach (var i in this.a.AllVersions)
            {
                if ( i.Type == "release" )
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
            public string Time { get; set; }

        }
    }
    public class VersionAssetIndex
    {
        public static List<SFNTD> ParseAssetsIndex(string jsonString,string path)
        {
            var assets = new List<SFNTD>();
            var jsonDocument = JsonDocument.Parse(jsonString);
            var objects = jsonDocument.RootElement.GetProperty("objects");
            foreach (var property in objects.EnumerateObject())
            {
                string fileName = property.Name;
                string hash = property.Value.GetProperty("hash").GetString();

                string hashPrefix = hash.Substring(0, 2);

                assets.Add(new SFNTD($"https://resources.download.minecraft.net/{hashPrefix}/{hash}", hash, path + $".minecraft/assets/objects/{hashPrefix}/{hash}"));
            }

            return assets;
        }
    }
}
