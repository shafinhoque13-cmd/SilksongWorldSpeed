using BepInEx;
using UnityEngine;
using System;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.0.0")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        private bool _showMenu = false;
        private Rect _bubbleRect = new Rect(20, 300, 160, 70); 
        private Rect _windowRect = new Rect(100, 100, 450, 500);
        
        private int _selectedMode = 2; 
        private float _level = 1.0f;
        private float _currentSpeedMult = 1.0f;
        private float _scanTimer = 0f;

        void Awake()
        {
            _bubbleRect.x = PlayerPrefs.GetFloat("Mod_BubbleX", 20);
            _bubbleRect.y = PlayerPrefs.GetFloat("Mod_BubbleY", 300);
            _selectedMode = PlayerPrefs.GetInt("Mod_SpeedMode", 2);
            _level = PlayerPrefs.GetFloat("Mod_SpeedLevel", 1.0f);
        }

        void OnGUI()
        {
            if (!_showMenu) _bubbleRect = GUI.Window(99, _bubbleRect, DrawBubble, "");
            else _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "LITE SPEED CONTROL");
        }

        void DrawBubble(int windowID) {
            if (GUI.Button(new Rect(5, 5, 150, 60), "SPEED MOD")) _showMenu = true;
            GUI.DragWindow();
        }

        void DrawMainWindow(int windowID) {
            GUILayout.BeginVertical();
            float displayVal = (_selectedMode == 1) ? (1.0f / Mathf.Floor(_level)) : Mathf.Floor(_level);
            GUILayout.Label($"Speed: {displayVal:F2}x (FPS Optimized)");

            if (GUILayout.Toggle(_selectedMode == 0, " FAST")) _selectedMode = 0;
            if (GUILayout.Toggle(_selectedMode == 1, " SLOW")) _selectedMode = 1;
            if (GUILayout.Toggle(_selectedMode == 2, " NORMAL")) _selectedMode = 2;

            _level = GUILayout.HorizontalSlider(_level, 1f, 5f);
            
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("SAVE & CLOSE", GUILayout.Height(70))) { 
                SaveSettings(); 
                _showMenu = false; 
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        void Update()
        {
            float floorLevel = Mathf.Floor(_level);
            _currentSpeedMult = _selectedMode == 0 ? floorLevel : (_selectedMode == 1 ? 1.0f / floorLevel : 1.0f);

            // Optimization: Only scan every 1.5 seconds to save battery/FPS
            _scanTimer += Time.deltaTime;
            if (_scanTimer >= 1.5f) {
                FastNPCUpdate();
                _scanTimer = 0;
            }
        }

        private void FastNPCUpdate()
        {
            // Lock Global Time to 1.0 to protect Hornet
            Time.timeScale = 1.0f;

            // Only find objects with Rigidbodies (actual enemies/projectiles)
            // This is 100x faster than finding all GameObjects
            Rigidbody2D[] bodies = UnityEngine.Object.FindObjectsByType<Rigidbody2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            
            foreach (var rb in bodies)
            {
                if (rb == null || rb.gameObject.layer == 9) continue;

                // Apply speed to the Animator on this body
                var anim = rb.GetComponent<Animator>();
                if (anim != null) anim.speed = _currentSpeedMult;

                // Force individual script values
                GameObject target = rb.gameObject;
                target.SendMessage("set_timeScale", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                target.SendMessage("SetSpeed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                target.SendMessage("SetFsmSpeed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
            }
        }

        void SaveSettings() {
            PlayerPrefs.SetInt("Mod_SpeedMode", _selectedMode);
            PlayerPrefs.SetFloat("Mod_SpeedLevel", _level);
            PlayerPrefs.SetFloat("Mod_BubbleX", _bubbleRect.x);
            PlayerPrefs.SetFloat("Mod_BubbleY", _bubbleRect.y);
            PlayerPrefs.Save();
        }
    }
}
