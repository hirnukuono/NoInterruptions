using BepInEx;
using BepInEx.Unity.IL2CPP;
using UnityEngine;
using HarmonyLib;
using BepInEx.Logging;

namespace NoInterruptions;

[BepInPlugin("NoInterruptions", "NoInterruptions", "0.1.4")]

public class Plugin : BasePlugin
{
    internal static ManualLogSource L;
    public override void Load()
    {
        L = Log;
        Harmony gargle = new("gargle");
        gargle.PatchAll();
        Debug.Log("NoInterruptions - zombie hand sticking out of a dead game's grave");
    }
} // plugin
