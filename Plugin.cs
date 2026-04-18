using BepInEx;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.0.0")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        private bool _showMenu = false;
        private Rect _bubbleRect = new Rect(20, 300, 160, 70); 
        private Rect _windowRect = new Rect(100, 100, 450, 600);
        
        private int _selectedMode = 2; 
        private float _level = 1.0f;
        private float _currentSpeedMult = 1.0f;
        private float _updateTimer = 0f;

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
            else _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "NPC & ENEMY SPEED CONTROL");
        }

        void DrawBubble(int windowID)
        {
            if (GUI.Button(new Rect(5, 5, 150, 60), "SPEED MOD\nMENU")) _showMenu = true;
            GUI.DragWindow();
        }

        void DrawMainWindow(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(10);
            
            float displayVal = (_selectedMode == 1) ? (1.0f / Mathf.Floor(_level)) : Mathf.Floor(_level);
            GUILayout.Label($"Multiplier: {displayVal:F2}x", GUI.skin.label);

            GUILayout.Space(20);
            if (GUILayout.Toggle(_selectedMode == 0, " [MODE] FAST ENEMIES")) _selectedMode = 0;
            if (GUILayout.Toggle(_selectedMode == 1, " [MODE] SLOW ENEMIES")) _selectedMode = 1;
            if (GUILayout.Toggle(_selectedMode == 2, " [MODE] NORMAL (1.0x)")) _selectedMode = 2;

            GUILayout.Space(10);
            GUILayout.Label($"Level: {(int)_level}");
            _level = GUILayout.HorizontalSlider(_level, 1f, 5f);

            GUILayout.Space(30);
            if (GUILayout.Button("FORCE SCAN AREA", GUILayout.Height(60))) { DeepUpdateNPCs(); }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("SAVE & APPLY", GUILayout.Height(70))) 
            { 
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

            _updateTimer += Time.deltaTime;
            if (_updateTimer >= 0.5f)
            {
                DeepUpdateNPCs();
                _updateTimer = 0;
            }
        }

        private void DeepUpdateNPCs()
        {
            Time.timeScale = 1.0f; // Lock player physics

            GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            foreach (GameObject obj in allObjects)
            {
                if (obj == null) continue;

                // Targeted Layers: 11=Enemy, 12=NPC, 17=Projectiles, 22=EnemyDetector
                if (obj.layer == 11 || obj.layer == 12 || obj.layer == 17 || obj.layer == 22)
                {
                    // Update Animators
                    Animator[] anims = obj.GetComponentsInChildren<Animator>();
                    foreach(var a in anims) { a.speed = _currentSpeedMult; }

                    // Update Physics (Handling both old and new Unity physics names)
                    Rigidbody2D[] rbs = obj.GetComponentsInChildren<Rigidbody2D>();
                    foreach(var rb in rbs)
                    {
                        try {
                            rb.linearVelocity *= _currentSpeedMult;
                        } catch {
                            #pragma warning disable CS0618
                            rb.velocity *= _currentSpeedMult;
                            #pragma warning restore CS0618
                        }
                    }

                    // Deep Script Hooks
                    obj.SendMessage("set_speed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                    obj.SendMessage("set_timeScale", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                    obj.SendMessage("SetFsmSpeed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                    obj.SendMessage("SetSpeed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
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
            DeepUpdateNPCs();
        }
    }
}
