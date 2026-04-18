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
            TargetSpeed = Config.Bind("Settings", "Enemy Speed", 1.0f, 
                new ConfigDescription("Adjust speed (0.9 to 5.0)", 
                new AcceptableValueRange<float>(0.9f, 5.0f)));
            Logger.LogInfo("Mod Ready!");
        }

        void Update()
        {
            if (Math.Abs(TargetSpeed.Value - _lastSpeed) > 0.01f)
            {
                ApplySpeedChange(TargetSpeed.Value);
                _lastSpeed = TargetSpeed.Value;
            }
        }

        private void ApplySpeedChange(float newSpeed)
        {
            // Explicitly using UnityEngine.Object to fix the Unity 6 error
            Animator[] allAnimators = UnityEngine.Object.FindObjectsOfType<Animator>();
            foreach (Animator anim in allAnimators)
            {
                if (anim == null) continue;
                // Layer 9 is the Player. We skip her so she doesn't speed up.
                if (anim.gameObject.layer == 9)
                {
                    anim.speed = 1.0f; 
                    continue;
                }
                anim.speed = newSpeed;
            }
        }
    }
}
