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
        private Rect _windowRect = new Rect(100, 100, 400, 550);
        
        private int _selectedMode = 2; 
        private float _level = 1.0f;
        private float _currentSpeedMult = 1.0f;
        private float _timer = 0f;

        void Awake()
        {
            _bubbleRect.x = PlayerPrefs.GetFloat("BubbleX", 20);
            _bubbleRect.y = PlayerPrefs.GetFloat("BubbleY", 300);
            _selectedMode = PlayerPrefs.GetInt("Mod_SpeedMode", 2);
            _level = PlayerPrefs.GetFloat("Mod_SpeedLevel", 1.0f);
        }

        void OnGUI()
        {
            if (!_showMenu) _bubbleRect = GUI.Window(99, _bubbleRect, DrawBubble, "");
            else _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "ENEMY SPEED CONTROL");
        }

        void DrawBubble(int windowID)
        {
            if (GUI.Button(new Rect(0, 0, _bubbleRect.width, _bubbleRect.height), "SPEED MOD")) _showMenu = true;
            GUI.DragWindow();
        }

        void DrawMainWindow(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Label($"Speed: {(_selectedMode == 1 ? (1f/Mathf.Floor(_level)).ToString("F2") : Mathf.Floor(_level).ToString())}x");
            
            if (GUILayout.Toggle(_selectedMode == 0, " INCREASE")) _selectedMode = 0;
            if (GUILayout.Toggle(_selectedMode == 1, " DECREASE")) _selectedMode = 1;
            if (GUILayout.Toggle(_selectedMode == 2, " NORMAL")) _selectedMode = 2;

            _level = GUILayout.HorizontalSlider(_level, 1f, 5f);

            GUILayout.Space(20);
            
            // MANUAL REFRESH: Fixes performance and forces NPCs to update
            if (GUILayout.Button("REFRESH ENEMIES NOW", GUILayout.Height(50))) { ApplyDeepHooks(); }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("SAVE & CLOSE", GUILayout.Height(60))) { SaveSettings(); _showMenu = false; }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        void Update()
        {
            float floorLevel = Mathf.Floor(_level);
            _currentSpeedMult = _selectedMode == 0 ? floorLevel : (_selectedMode == 1 ? 1.0f / floorLevel : 1.0f);

            // OPTIMIZATION: Only search for objects every 2 seconds to save FPS
            _timer += Time.deltaTime;
            if (_timer >= 2.0f)
            {
                ApplyDeepHooks();
                _timer = 0;
            }
        }

        private void ApplyDeepHooks()
        {
            // Specifically targeting enemy and NPC layers
            Animator[] anims = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var anim in anims)
            {
                if (anim == null) continue;
                int layer = anim.gameObject.layer;

                if (layer == 11 || layer == 12 || layer == 17)
                {
                    anim.speed = _currentSpeedMult;
                    // Trigger Spine/FSM updates
                    anim.gameObject.SendMessage("set_timeScale", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                    anim.gameObject.SendMessage("SetFsmSpeed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                }
                else if (layer == 9) { anim.speed = 1.0f; }
            }
        }

        void SaveSettings()
        {
            PlayerPrefs.SetInt("Mod_SpeedMode", _selectedMode);
            PlayerPrefs.SetFloat("Mod_SpeedLevel", _level);
            PlayerPrefs.SetFloat("BubbleX", _bubbleRect.x);
            PlayerPrefs.SetFloat("BubbleY", _bubbleRect.y);
            PlayerPrefs.Save();
            ApplyDeepHooks(); // Apply once immediately on save
        }
    }
}
