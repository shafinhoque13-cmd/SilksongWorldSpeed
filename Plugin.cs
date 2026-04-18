using BepInEx;
using UnityEngine;
using System;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.0.0")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        private bool _showMenu = false;
        
        // Moveable UI Rects
        private Rect _bubbleRect = new Rect(20, 300, 120, 60); 
        private Rect _windowRect = new Rect(100, 100, 400, 500);
        
        private int _selectedMode = 2; // 0=Inc, 1=Dec, 2=Norm
        private float _level = 1.0f;
        private float _currentSpeedMult = 1.0f;
        private float _scanTimer = 0f;

        void Awake()
        {
            // Load settings and bubble position from internal memory
            _bubbleRect.x = PlayerPrefs.GetFloat("Mod_BubbleX", 20);
            _bubbleRect.y = PlayerPrefs.GetFloat("Mod_BubbleY", 300);
            _selectedMode = PlayerPrefs.GetInt("Mod_SpeedMode", 2);
            _level = PlayerPrefs.GetFloat("Mod_SpeedLevel", 1.0f);
            
            Logger.LogInfo("World Speed Controller: System Loaded");
        }

        void OnGUI()
        {
            if (!_showMenu)
            {
                // Unique ID 99 for the draggable bubble
                _bubbleRect = GUI.Window(99, _bubbleRect, DrawBubble, "");
            }
            else
            {
                _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "ENEMY SPEED CONTROL");
            }
        }

        void DrawBubble(int windowID)
        {
            if (GUI.Button(new Rect(0, 0, _bubbleRect.width, _bubbleRect.height), "SPEED MOD"))
            {
                _showMenu = true;
            }
            // Drag anywhere on the button to move it
            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        void DrawMainWindow(int windowID)
        {
            GUILayout.BeginVertical();
            
            // Display current math logic
            float displayValue = (_selectedMode == 1) ? (1.0f / Mathf.Floor(_level)) : Mathf.Floor(_level);
            GUILayout.Label($"Current Multiplier: {displayValue:F2}x");

            if (GUILayout.Toggle(_selectedMode == 0, " [MODE] INCREASE SPEED")) _selectedMode = 0;
            if (GUILayout.Toggle(_selectedMode == 1, " [MODE] DECREASE SPEED")) _selectedMode = 1;
            if (GUILayout.Toggle(_selectedMode == 2, " [MODE] NORMAL")) _selectedMode = 2;

            _level = GUILayout.HorizontalSlider(_level, 1f, 5f);

            GUILayout.Space(20);
            
            // Forces the mod to scan the area immediately
            if (GUILayout.Button("FORCE REFRESH NPCs", GUILayout.Height(50)))
            {
                ApplyDeepHooks();
            }

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
            // Update the multiplier math
            float floorLevel = Mathf.Floor(_level);
            if (_selectedMode == 0) _currentSpeedMult = floorLevel;
            else if (_selectedMode == 1) _currentSpeedMult = 1.0f / floorLevel;
            else _currentSpeedMult = 1.0f;

            // Optimization: Only scan for new NPCs every 2.5 seconds to fix FPS
            _scanTimer += Time.deltaTime;
            if (_scanTimer >= 2.5f)
            {
                ApplyDeepHooks();
                _scanTimer = 0;
            }
        }

        private void ApplyDeepHooks()
        {
            // Scans all game objects on targeted layers
            GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            foreach (GameObject obj in allObjects)
            {
                if (obj == null) continue;
                int layer = obj.layer;

                // Layers: 11 (Enemy), 12 (NPC), 17 (Projectiles)
                if (layer == 11 || layer == 12 || layer == 17)
                {
                    // Update Animator
                    var anim = obj.GetComponent<Animator>();
                    if (anim != null) anim.speed = _currentSpeedMult;

                    // Update Physics
                    var rb = obj.GetComponent<Rigidbody2D>();
                    if (rb != null) rb.simulated = true;

                    // Deep Component Hooks via Message
                    obj.SendMessage("set_speed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                    obj.SendMessage("set_timeScale", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                    obj.SendMessage("SetFsmSpeed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                }
                else if (layer == 9) // Maintain Hornet at 1.0x
                {
                    var anim = obj.GetComponent<Animator>();
                    if (anim != null) anim.speed = 1.0f;
                    obj.SendMessage("set_timeScale", 1.0f, SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        void SaveSettings()
        {
            // Persist data across game restarts
            PlayerPrefs.SetInt("Mod_SpeedMode", _selectedMode);
            PlayerPrefs.SetFloat("Mod_SpeedLevel", _level);
            PlayerPrefs.SetFloat("Mod_BubbleX", _bubbleRect.x);
            PlayerPrefs.SetFloat("Mod_BubbleY", _bubbleRect.y);
            PlayerPrefs.Save();
            ApplyDeepHooks();
        }
    }
}
