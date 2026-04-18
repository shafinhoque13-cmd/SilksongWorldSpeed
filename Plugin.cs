using BepInEx;
using UnityEngine;
using System;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.0.0")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        private bool _showMenu = false;
        private Rect _bubbleRect = new Rect(20, 300, 120, 60); 
        private Rect _windowRect = new Rect(100, 100, 400, 500);
        
        private int _selectedMode = 2; 
        private float _level = 1.0f;
        private float _currentSpeedMult = 1.0f;

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
            else _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "CONTROL PANEL");
        }

        void DrawBubble(int windowID) {
            if (GUI.Button(new Rect(0, 0, _bubbleRect.width, _bubbleRect.height), "SPEED MOD")) _showMenu = true;
            GUI.DragWindow();
        }

        void DrawMainWindow(int windowID) {
            GUILayout.BeginVertical();
            float displayVal = (_selectedMode == 1) ? (1f / Mathf.Floor(_level)) : Mathf.Floor(_level);
            GUILayout.Label($"World Mult: {displayVal:F2}x");

            if (GUILayout.Toggle(_selectedMode == 0, " INCREASE")) _selectedMode = 0;
            if (GUILayout.Toggle(_selectedMode == 1, " DECREASE")) _selectedMode = 1;
            if (GUILayout.Toggle(_selectedMode == 2, " NORMAL")) _selectedMode = 2;

            _level = GUILayout.HorizontalSlider(_level, 1f, 5f);
            
            GUILayout.Space(20);
            if (GUILayout.Button("FORCE RESET PLAYER", GUILayout.Height(50))) { ApplyGlobalSpeed(); }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("SAVE & CLOSE", GUILayout.Height(60))) { SaveSettings(); _showMenu = false; }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        void Update()
        {
            float floorLevel = Mathf.Floor(_level);
            _currentSpeedMult = _selectedMode == 0 ? floorLevel : (_selectedMode == 1 ? 1.0f / floorLevel : 1.0f);

            ApplyGlobalSpeed();
        }

        private void ApplyGlobalSpeed()
        {
            // 1. Set Global Time Scale (Enemies, NPCs, Projectiles)
            Time.timeScale = _currentSpeedMult;
            
            // 2. Adjust Physics Heartbeat (Crucial for smooth Slow-Mo)
            Time.fixedDeltaTime = 0.02f * (Time.timeScale < 1.0f ? Time.timeScale : 1.0f);

            // 3. ISOLATE HORNET (The Player)
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            
            // If tag fails, we find by Layer 9 (The standard Player Layer)
            if (player == null) {
                GameObject[] all = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach(var o in all) { if (o.layer == 9) { player = o; break; } }
            }

            if (player != null) {
                // Force Animator to follow the real-world clock, not our mod clock
                var pAnim = player.GetComponent<Animator>();
                if (pAnim != null) {
                    pAnim.updateMode = AnimatorUpdateMode.UnscaledTime;
                    pAnim.speed = 1.0f; 
                }

                // Correct Gravity for Hornet so jumping feels normal
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null) {
                    // Gravity math: Speeding up time increases perceived gravity.
                    // We divide gravity by the square of speed to balance it out.
                    rb.gravityScale = 1.0f / (Time.timeScale * Time.timeScale);
                }
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
