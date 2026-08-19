using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.CloudSave;
using Newtonsoft.Json;
using DeleteOptions = Unity.Services.CloudSave.Models.Data.Player.DeleteOptions;

namespace Assets._Scripts.Map
{
    public class MapSaveService
    {
        private const string MapDataKey = "RoguelikeMapData";

        public static async Task SaveMapAsync(MapData data)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data);
                var dict = new Dictionary<string, object>
                {
                    { MapDataKey, json }
                };
                await CloudSaveService.Instance.Data.Player.SaveAsync(dict);
                Debug.Log("Map data saved to Cloud Save.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save map data: {e.Message}");
            }
        }

        public static async Task<MapData> LoadMapAsync()
        {
            try
            {
                var keys = new HashSet<string> { MapDataKey };
                var results = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

                if (results.TryGetValue(MapDataKey, out var item))
                {
                    string json = item.Value.GetAsString();
                    return JsonConvert.DeserializeObject<MapData>(json);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load map data: {e.Message}");
            }
            return null;
        }

        public static async Task DeleteMapAsync()
        {
            try
            {                
                await CloudSaveService.Instance.Data.Player.DeleteAsync(MapDataKey, new DeleteOptions());
                Debug.Log("Map data deleted from Cloud Save.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete map data: {e.Message}");
            }
        }
    }
}
