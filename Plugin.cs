using BepInEx;
using UnityEngine;
using System;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.0.0")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        private bool _showMenu = false;
        
        // Rects for the UI (Bubble and Main Window)
        private Rect _bubbleRect = new Rect(20, 300, 120, 60); 
        private Rect _windowRect = new Rect(100, 100, 400, 500);
        
        private int _selectedMode = 2; // 0=Inc, 1=Dec, 2=Norm
        private float _level = 1.0f;
        private float _currentSpeedMult = 1.0f;

        void Awake()
        {
            // Load all settings from internal phone memory
            _bubbleRect.x = PlayerPrefs.GetFloat("Mod_BubbleX", 20);
            _bubbleRect.y = PlayerPrefs.GetFloat("Mod_BubbleY", 300);
            _selectedMode = PlayerPrefs.GetInt("Mod_SpeedMode", 2);
            _level = PlayerPrefs.GetFloat("Mod_SpeedLevel", 1.0f);
            
            Logger.LogInfo("Speed Mod: Loaded and Ready");
        }

        void OnGUI()
        {
            if (!_showMenu)
            {
                // Unique Window ID 99 for the draggable bubble
                _bubbleRect = GUI.Window(99, _bubbleRect, DrawBubble, "");
            }
            else
            {
                // Main Control Window
                _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "ENEMY SPEED CONTROL");
            }
        }

        void DrawBubble(int windowID)
        {
            if (GUI.Button(new Rect(0, 0, _bubbleRect.width, _bubbleRect.height), "SPEED MOD"))
            {
                _showMenu = true;
            }
            // Allows the button to be dragged anywhere on the phone screen
            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        void DrawMainWindow(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(10);

            // Display current multiplier math
            float displayVal = (_selectedMode == 1) ? (1.0f / Mathf.Floor(_level)) : Mathf.Floor(_level);
            GUILayout.Label($"World Multiplier: {displayVal:F2}x", GUI.skin.label);

            if (GUILayout.Toggle(_selectedMode == 0, " [MODE] INCREASE SPEED")) _selectedMode = 0;
            if (_selectedMode == 1) GUILayout.Space(5);
            
            if (GUILayout.Toggle(_selectedMode == 1, " [MODE] DECREASE (SLOW-MO)")) _selectedMode = 1;
            if (_selectedMode == 0 || _selectedMode == 1)
            {
                GUILayout.Label($"Level: {(int)_level}");
                _level = GUILayout.HorizontalSlider(_level, 1f, 5f);
            }

            GUILayout.Space(15);
            if (GUILayout.Toggle(_selectedMode == 2, " [MODE] NORMAL")) _selectedMode = 2;

            GUILayout.FlexibleSpace();

            GUI.color = Color.green;
            if (GUILayout.Button("SAVE & CLOSE", GUILayout.Height(60)))
            {
                SaveSettings();
                _showMenu = false;
            }
            GUI.color = Color.white;

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        void Update()
        {
            // Calculate Multiplier
            float floorLevel = Mathf.Floor(_level);
            if (_selectedMode == 0) _currentSpeedMult = floorLevel;
            else if (_selectedMode == 1) _currentSpeedMult = 1.0f / floorLevel;
            else _currentSpeedMult = 1.0f;

            ApplyGlobalSpeed();
        }

        private void ApplyGlobalSpeed()
        {
            // 1. Force Engine Time (Affects NPCs, Enemies, and Projectiles)
            Time.timeScale = _currentSpeedMult;
            
            // 2. Fix Physics Heartbeat (Crucial for Smooth Slow-Motion)
            if (_currentSpeedMult < 1.0f)
            {
                // In slow-mo, we speed up the physics check to prevent stuttering
                Time.fixedDeltaTime = 0.02f * _currentSpeedMult;
            }
            else
            {
                // In fast-mo, we keep it at 0.02 to maintain high FPS
                Time.fixedDeltaTime = 0.02f;
            }

            // 3. Compensate Hornet (The Player)
            // We search for the player and force her to move at "1.0 / Mult" 
            // so she feels normal while the world is fast/slow.
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var pAnim = player.GetComponent<Animator>();
                if (pAnim != null) pAnim.speed = 1.0f / _currentSpeedMult;
            }
            else
            {
                // Fallback: Check for Hornet's layer (Layer 9)
                Animator[] allAnims = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
                foreach(var a in allAnims)
                {
                    if (a.gameObject.layer == 9)
                    {
                        a.speed = 1.0f / _currentSpeedMult;
                    }
                }
            }
        }

        void SaveSettings()
        {
            PlayerPrefs.SetInt("Mod_SpeedMode", _selectedMode);
            PlayerPrefs.SetFloat("Mod_SpeedLevel", _level);
            PlayerPrefs.SetFloat("Mod_BubbleX", _bubbleRect.x);
            PlayerPrefs.SetFloat("Mod_BubbleY", _bubbleRect.y);
            PlayerPrefs.Save();
        }
    }
}
