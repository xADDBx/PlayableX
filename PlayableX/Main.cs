using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityModManagerNet;
using static UnityModManagerNet.UnityModManager;
using UnityEngine;
using Kingmaker.Blueprints.Area;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker;

namespace PlayableX;

#if DEBUG
[EnableReloading]
#endif
public static class Main {
    internal static Harmony HarmonyInstance;
    internal static UnityModManager.ModEntry.ModLogger log;
    public static Settings settings;

    public static bool Load(UnityModManager.ModEntry modEntry) {
        log = modEntry.Logger;
#if DEBUG
        modEntry.OnUnload = OnUnload;
#endif
        modEntry.OnGUI = OnGUI;
        modEntry.OnSaveGUI = OnSaveGUI;
        settings = Settings.Load<Settings>(modEntry);
        HarmonyInstance = new Harmony(modEntry.Info.Id);
        HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        return true;
    }
    public static void OnSaveGUI(ModEntry modEntry) {
        settings.Save(modEntry);
    }
    public static bool MyToggle(ref bool val, string label) {
        var tmp = val;
        var newVal = GUILayout.Toggle(tmp, label);
        if (newVal != val) {
            val = newVal;
            return true;
        }
        return false;
    }
    public static void TurnAllOff() {

    }
    public static void OnGUI(UnityModManager.ModEntry modEntry) {
        GUILayout.Label("Should the next character creation be a Navigator?");
        if (MyToggle(ref PlayableNavigator.NavigatorPatches.CreateNavigator, "Create Navigator") && PlayableNavigator.NavigatorPatches.CreateNavigator) {
            PlayableTechPriest.TechPriestPatches.CreateTechPriest = false;
            PlayableAdeptaSororitas.AdeptaSororitasPatches.CreateAdeptaSororitas = false;
            DeathCultAssassin.DeathCultAssassinPatches.CreateDeathCultAssassin = false;
        }

        settings.enableMoreThanOneNavigatorInParty = GUILayout.Toggle(settings.enableMoreThanOneNavigatorInParty, "Allow more than one Navigator in a Party", GUILayout.ExpandWidth(false));

        GUILayout.Label("Should the next character creation be a Tech Priest?");
        if (MyToggle(ref PlayableTechPriest.TechPriestPatches.CreateTechPriest, "Create Tech Priest") && PlayableTechPriest.TechPriestPatches.CreateTechPriest) {
            PlayableNavigator.NavigatorPatches.CreateNavigator = false;
            PlayableAdeptaSororitas.AdeptaSororitasPatches.CreateAdeptaSororitas = false;
            DeathCultAssassin.DeathCultAssassinPatches.CreateDeathCultAssassin = false;
        }

        GUILayout.Label("Should the next character creation be a Adepta Sororitas?");
        if (MyToggle(ref PlayableAdeptaSororitas.AdeptaSororitasPatches.CreateAdeptaSororitas, "Create Adepta Sororitas") && PlayableAdeptaSororitas.AdeptaSororitasPatches.CreateAdeptaSororitas) {
            PlayableNavigator.NavigatorPatches.CreateNavigator = false;
            PlayableTechPriest.TechPriestPatches.CreateTechPriest = false;
            DeathCultAssassin.DeathCultAssassinPatches.CreateDeathCultAssassin = false;
        }

        GUILayout.Label("Should the next character creation be a Death Cult Assassin?");
        if (MyToggle(ref DeathCultAssassin.DeathCultAssassinPatches.CreateDeathCultAssassin, "Create Death Cult Assassin") && DeathCultAssassin.DeathCultAssassinPatches.CreateDeathCultAssassin) {
            PlayableNavigator.NavigatorPatches.CreateNavigator = false;
            PlayableTechPriest.TechPriestPatches.CreateTechPriest = false;
            PlayableAdeptaSororitas.AdeptaSororitasPatches.CreateAdeptaSororitas = false;
        }
    }

#if DEBUG
    public static bool OnUnload(UnityModManager.ModEntry modEntry) {
        HarmonyInstance.UnpatchAll(modEntry.Info.Id);
        return true;
    }
#endif
    [HarmonyPatch(typeof(Game))]
    internal static class Game_Patch {
        [HarmonyPatch(nameof(Game.LoadArea), [typeof(BlueprintArea), typeof(BlueprintAreaEnterPoint), typeof(AutoSaveMode), typeof(SaveInfo), typeof(Action)])]
        [HarmonyPrefix]
        private static void LoadArea() {
            PlayableAdeptaSororitas.AdeptaSororitasEntityPartStorage.ClearCachedPerSave();
            PlayableNavigator.NavigatorEntityPartStorage.ClearCachedPerSave();
            PlayableTechPriest.TechPriestEntityPartStorage.ClearCachedPerSave();
        }
    }
}
