using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OneLauncher.Core
{
    public class VersionAssetIndex
    {
        public static List<SFNTD> ParseAssetsIndex(string jsonString, string path)
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
