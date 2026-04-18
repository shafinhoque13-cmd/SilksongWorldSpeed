using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using System;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.0.0")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> TargetSpeed;
        private float _lastSpeed = 1.0f;

        void Awake()
        {
            // Creates the slider in the BepInEx/Mod List menu
            TargetSpeed = Config.Bind("Settings", "Enemy Speed", 1.0f, 
                new ConfigDescription("Adjust world speed (0.9 to 5.0)", 
                new AcceptableValueRange<float>(0.9f, 5.0f)));
            
            Logger.LogInfo("Silksong World Speed Mod Loaded Successfully!");
        }

        void Update()
        {
            // Only runs the update if you actually move the slider
            if (Math.Abs(TargetSpeed.Value - _lastSpeed) > 0.01f)
            {
                ApplySpeedChange(TargetSpeed.Value);
                _lastSpeed = TargetSpeed.Value;
            }
        }

        private void ApplySpeedChange(float newSpeed)
        {
            // Unity 6 modern method: FindObjectsByType is faster and removes warnings
            // We use the full 'UnityEngine.Object' to avoid the CS0234 error
            Animator[] allAnimators = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
            
            foreach (Animator anim in allAnimators)
            {
                if (anim == null) continue;

                // Layer 9 is Hornet. We keep her at 1.0 so the game remains playable.
                if (anim.gameObject.layer == 9)
                {
                    anim.speed = 1.0f; 
                    continue;
                }

                // Apply the custom speed to everything else (Enemies, NPCs)
                anim.speed = newSpeed;
            }
            
            Logger.LogInfo($"World Speed updated to: {newSpeed}");
        }
    }
}
