using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using LevelGeneration;
using Player;
using UnityEngine;
using System.Collections;

namespace NoInterruptions
{
    [HarmonyPatch(typeof(LG_ComputerTerminal))]
    internal static class TerminalPatch
    {
        [HarmonyPatch(nameof(LG_ComputerTerminal.Update))]
        [HarmonyPrefix]
        private static void Postfix_CheckPlayerVicinity(LG_ComputerTerminal __instance)
        {
            if (__instance.name == "fdrunning") return;
            if (__instance.CurrentStateName == TERM_State.Sleeping) return;
            var plrs = PlayerManager.PlayerAgentsInLevel;
            bool flag = false;
            foreach (var plr in plrs) if (plr.Alive && UnityEngine.Vector3.Distance(plr.Position, __instance.gameObject.transform.position) < 2f) flag = true;
            if (!flag)
            {
                Debug.Log("NoInterruptions - terminal not sleeping, all players more than 2 meters away..");
                __instance.name = "fdrunning";
                CoroutineManager.StartCoroutine(FiveSecondDelay(__instance).WrapToIl2Cpp());
            }
        }


        [HarmonyPatch(nameof(LG_ComputerTerminal.EnterFPSView))]
        [HarmonyPostfix]
        private static void Postfix_EnterInteractingFPS(LG_ComputerTerminal __instance)
        {
            if (__instance.CurrentStateName == TERM_State.PlayerInteracting)
                CoroutineManager.StartCoroutine(DelayedFixState(__instance, __instance.m_localInteractionSource).WrapToIl2Cpp());
        }

        [HarmonyPatch(nameof(LG_ComputerTerminal.SyncChangeState))]
        [HarmonyPostfix]
        private static void Postfix_EnterInteracting(LG_ComputerTerminal __instance)
        {
            if (__instance.CurrentStateName == TERM_State.PlayerInteracting)
                CoroutineManager.StartCoroutine(DelayedFixState(__instance, __instance.m_syncedInteractionSource).WrapToIl2Cpp());
        }
        private static IEnumerator FiveSecondDelay(LG_ComputerTerminal terminal)
        {
            yield return new WaitForSeconds(5);
            var plrs = PlayerManager.PlayerAgentsInLevel;
            bool flag = false;
            foreach (var plr in plrs) if (plr.Alive && UnityEngine.Vector3.Distance(plr.Position, terminal.gameObject.transform.position) < 2f) flag = true;
            if (!flag)
            {
                Debug.Log("NoInterruptions - terminal not sleeping, players still far away. go to sleeeeep..");
                terminal.ChangeState(TERM_State.Sleeping);
                terminal.name = "fdnotrunning";
            }
            yield break;
        }

        private static IEnumerator DelayedFixState(LG_ComputerTerminal terminal, PlayerAgent player)
        {
            // JFS - Delay checking locomotion in case packet was delayed
            float endTime = Clock.Time + 0.5f;
            while (Clock.Time < endTime)
            {
                if (terminal.CurrentStateName != TERM_State.PlayerInteracting)
                    yield break;
                yield return null;
            }

            while (terminal.CurrentStateName == TERM_State.PlayerInteracting)
            {
                AttemptFixState(terminal, player);
                yield return null;
            }
        }

        private static void AttemptFixState(LG_ComputerTerminal terminal, PlayerAgent player)
        {
            if (player.Locomotion.m_currentStateEnum != PlayerLocomotion.PLOC_State.OnTerminal)
            {
                if (player.IsLocallyOwned || (player.transform.position - player.Sync.m_locomotionData.Pos).sqrMagnitude > 0.0001f)
                    terminal.ChangeState(TERM_State.Awake);
            }
        }
    }
}