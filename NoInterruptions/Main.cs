using BepInEx;
using BepInEx.Unity.IL2CPP;
using UnityEngine;
using HarmonyLib;

namespace NoInterruptions;

[BepInPlugin("NoInterruptions", "NoInterruptions", "0.1.3")]

public class Plugin : BasePlugin
{
    public override void Load()
    {
        Harmony gargle = new("gargle");
        gargle.PatchAll();
        Debug.Log("NoInterruptions - zombie hand sticking out of a dead game's grave");
    }
} // plugin
