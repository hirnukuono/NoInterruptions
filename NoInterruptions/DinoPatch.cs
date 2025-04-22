using Gear;
using HarmonyLib;
using Player;
using UnityEngine;

namespace NoInterruptions
{
    [HarmonyPatch]
    internal static class InteractionPatch
    {
        [HarmonyPatch(typeof(Interact_Timed), nameof(Interact_Timed.CheckSoundPlayer))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool Post_CheckSoundPlayer(Interact_Timed __instance)
        {
            if (__instance.m_sound == null && __instance.transform == null)
            {
                __instance.m_sound = new(PlayerManager.GetLocalPlayerAgent().transform.position);
                return false;
            }
            return true;
        }

        private static Interact_Timed? _cachedPack;
        private static Interact_Timed? _cachedInteract;
        private static RaycastHit _rayHitInfo;

        [HarmonyPatch(typeof(PlayerInteraction), nameof(PlayerInteraction.FixedUpdate))]
        [HarmonyPrefix]
        private static void Pre_FixedUpdate(PlayerInteraction __instance)
        {
            if (_cachedPack == null && __instance.m_owner.Inventory.WieldedItem?.AmmoType == AmmoType.ResourcePackRel)
                _cachedPack = __instance.m_owner.Inventory.WieldedItem.TryCast<ResourcePackFirstPerson>()!.m_interactApplyResource;

            // Pack usage overrides all interacts
            if (_cachedPack != null && _cachedPack.TimerIsActive)
            {
                _cachedInteract = null;
                PlayerInteraction.CameraRayInteractionEnabled = false;
                PlayerInteraction.SphereCheckInteractionEnabled = false;
                PlayerInteraction.LadderInteractionEnabled = false;
                return;
            }

            _cachedInteract = __instance.m_bestSelectedInteract?.TryCast<Interact_Timed>();

            if (_cachedInteract != null && __instance.m_owner.Inventory.WieldedItem?.AllowPlayerInteraction == true && InteractIsActive(__instance))
            {
                PlayerInteraction.CameraRayInteractionEnabled = false;
                PlayerInteraction.SphereCheckInteractionEnabled = false;
                PlayerInteraction.LadderInteractionEnabled = false;
            }
            else
            {
                _cachedInteract = null;
                PlayerInteraction.CameraRayInteractionEnabled = true;
                PlayerInteraction.SphereCheckInteractionEnabled = true;
                PlayerInteraction.LadderInteractionEnabled = true;
            }
        }

        [HarmonyPatch(typeof(PlayerInteraction), nameof(PlayerInteraction.FixedUpdate))]
        [HarmonyPostfix]
        private static void Post_FixedUpdate(PlayerInteraction __instance)
        {
            __instance.m_bestInteractInCurrentSearch = _cachedInteract;
        }

        private static bool InteractIsActive(PlayerInteraction __instance)
        {
            if (_cachedInteract == null || !_cachedInteract.TimerIsActive) return false;

            if (!_cachedInteract.IsActive || !_cachedInteract.PlayerCanInteract(__instance.m_owner)) return false;

            Vector3 camPos = __instance.m_owner.CamPos;
            float searchRadius = __instance.m_searchRadius + Mathf.Min(Mathf.Abs(__instance.m_owner.TargetLookDir.y), 0.5f);
            float sqRadius = searchRadius * searchRadius;

            // Make sure the cached interact is still in range
            bool inRange = false;
            foreach (Collider collider in _cachedInteract.gameObject.GetComponentsInChildren<Collider>())
            {
                float dist = Vector3.SqrMagnitude(collider.transform.position - camPos);
                if (dist <= sqRadius)
                {
                    inRange = true;
                    break;
                }
            }

            Vector3 position = _cachedInteract.transform.position;
            Vector3 diff = position - camPos;
            if (diff.y < 2f)
                diff.y = 0;

            // In R6Mono, this only runs if the camera ray failed, but should be safe to put it here
            if (diff.sqrMagnitude <= __instance.m_proximityRadius * __instance.m_proximityRadius)
                __instance.AddToProximity(_cachedInteract);
            else
                __instance.RemoveFromProximity(_cachedInteract);

            if (!inRange) return false;

            FPSCamera camera = __instance.m_owner.FPSCamera;

            // Already looking at it even including blockers
            if (camera.CameraRayObject == _cachedInteract.gameObject) return true;

            // Check that looking at the object ignoring blockers
            if (Physics.Raycast(camera.m_camRay, out _rayHitInfo, searchRadius, LayerManager.MASK_PLAYER_INTERACT_SPHERE)
             && _rayHitInfo.collider.gameObject == _cachedInteract.gameObject) return true;

            if (_cachedInteract.OnlyActiveWhenLookingStraightAt) return false;

            // Check that it's on the screen
            Vector3 screenVector = camera.m_camera.WorldToScreenPoint(position);
            if (screenVector.z <= 0f || !GuiManager.IsOnScreen(screenVector)) return false;

            if (_cachedInteract.RequireCollisionCheck && Physics.Raycast(camPos, diff.normalized, out _rayHitInfo, diff.magnitude, LayerManager.MASK_PLAYER_INTERACT_SPHERE))
                return _rayHitInfo.collider.gameObject == _cachedInteract.gameObject;

            return true;
        }
    }
}
