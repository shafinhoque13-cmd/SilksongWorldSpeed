using BepInEx;
using UnityEngine;
using System;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.0.0")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        private bool _showMenu = false;
        
        // Moveable Bubble Rect
        private Rect _bubbleRect = new Rect(20, 300, 120, 60); 
        private Rect _windowRect = new Rect(100, 100, 400, 500);
        
        private int _selectedMode = 2; // 0=Inc, 1=Dec, 2=Norm
        private float _level = 1.0f;
        private float _currentSpeedMult = 1.0f;

        void Awake()
        {
            // Load saved position and settings
            _bubbleRect.x = PlayerPrefs.GetFloat("BubbleX", 20);
            _bubbleRect.y = PlayerPrefs.GetFloat("BubbleY", 300);
            _selectedMode = PlayerPrefs.GetInt("Mod_SpeedMode", 2);
            _level = PlayerPrefs.GetFloat("Mod_SpeedLevel", 1.0f);
        }

        void OnGUI()
        {
            if (!_showMenu)
            {
                // DRAGGABLE BUBBLE LOGIC
                // We use a GUI.Window for the bubble too, so it can be dragged!
                _bubbleRect = GUI.Window(1, _bubbleRect, DrawBubble, "");
            }
            else
            {
                _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "ENEMY SPEED CONTROL");
            }
        }

        // The logic for the small floating bubble
        void DrawBubble(int windowID)
        {
            if (GUI.Button(new Rect(0, 0, _bubbleRect.width, _bubbleRect.height), "SPEED MOD"))
            {
                _showMenu = true;
            }
            // This line makes the bubble draggable on your screen
            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        void DrawMainWindow(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(10);

            // Logic: 1x to 5x
            if (GUILayout.Toggle(_selectedMode == 0, " [MODE] INCREASE SPEED")) _selectedMode = 0;
            if (_selectedMode == 0)
            {
                GUILayout.Label($"Enemy Action Speed: {(int)_level}X");
                _level = GUILayout.HorizontalSlider(_level, 1f, 5f);
            }

            GUILayout.Space(15);

            if (GUILayout.Toggle(_selectedMode == 1, " [MODE] DECREASE SPEED")) _selectedMode = 1;
            if (_selectedMode == 1)
            {
                GUILayout.Label($"Enemy Action Slowness: {(int)_level}");
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

        void SaveSettings()
        {
            PlayerPrefs.SetInt("Mod_SpeedMode", _selectedMode);
            PlayerPrefs.SetFloat("Mod_SpeedLevel", _level);
            PlayerPrefs.SetFloat("BubbleX", _bubbleRect.x);
            PlayerPrefs.SetFloat("BubbleY", _bubbleRect.y);
            PlayerPrefs.Save();
        }

        void Update()
        {
            if (_selectedMode == 0) _currentSpeedMult = (float)Math.Floor(_level);
            else if (_selectedMode == 1) _currentSpeedMult = 1.0f / (float)Math.Floor(_level);
            else _currentSpeedMult = 1.0f;

            ApplyTargetedSpeed();
        }

        private void ApplyTargetedSpeed()
        {
            Animator[] anims = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var anim in anims)
            {
                if (anim == null) continue;
                int layer = anim.gameObject.layer;

                // Targeted Layers: 11 (Enemy), 12 (NPC), 17 (Enemy Projectiles)
                if (layer == 11 || layer == 12 || layer == 17)
                {
                    anim.speed = _currentSpeedMult;
                }
                else if (layer == 9 || layer == 5) // Skip Player/UI
                {
                    anim.speed = 1.0f;
                }
            }
        }
    }
}
