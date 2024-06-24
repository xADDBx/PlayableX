using Kingmaker.EntitySystem.Persistence;
using Kingmaker;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker.EntitySystem.Entities.Base;

namespace PlayableX.PlayableAdeptaSororitas; 
public static class AdeptaSororitasEntityPartStorage {
    public class PerSaveSettings : EntityPart {
        public const string ID = "PlayableAdeptaSororitas.PerSaveSettings";
        [JsonProperty]
        public Dictionary<string, List<string>> AddClothes = new();
    }
    private static PerSaveSettings cachedPerSave = null;
    public static void ClearCachedPerSave() => cachedPerSave = null;
    public static void ReloadPerSaveSettings() {
        var player = Game.Instance?.Player;
        if (player == null || Game.Instance.SaveManager.CurrentState == SaveManager.State.Loading) return;
        if (Game.Instance.State.InGameSettings.List.TryGetValue(PerSaveSettings.ID, out var obj) && obj is string json) {
            try {
                cachedPerSave = JsonConvert.DeserializeObject<PerSaveSettings>(json);
            }
            catch (Exception) {
            }
        }
        if (cachedPerSave == null) {
            cachedPerSave = new PerSaveSettings();
            SavePerSaveSettings();
        }
    }
    public static void SavePerSaveSettings() {
        var player = Game.Instance?.Player;
        if (player == null) return;
        if (cachedPerSave == null)
            ReloadPerSaveSettings();
        var json = JsonConvert.SerializeObject(cachedPerSave);
        Game.Instance.State.InGameSettings.List[PerSaveSettings.ID] = json;
    }
    public static PerSaveSettings perSave {
        get {
            try {
                if (cachedPerSave != null) return cachedPerSave;
                ReloadPerSaveSettings();
            }
            catch (Exception) { }
            return cachedPerSave;
        }
    }
}
