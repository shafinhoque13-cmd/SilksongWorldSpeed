using BepInEx;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.4.0")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        private bool _showMenu = false;
        private Rect _bubbleRect = new Rect(50, 300, 200, 100); 
        private Rect _windowRect = new Rect(100, 100, 600, 700);
        
        private int _selectedMode = 2; 
        private float _level = 1.0f;
        private float _currentSpeed = 1.0f;
        private float _pulseTimer = 0f;

        void Awake()
        {
            _bubbleRect.x = PlayerPrefs.GetFloat("Mod_BubbleX", 50);
            _bubbleRect.y = PlayerPrefs.GetFloat("Mod_BubbleY", 300);
            _selectedMode = PlayerPrefs.GetInt("Mod_SpeedMode", 2);
            _level = PlayerPrefs.GetFloat("Mod_SpeedLevel", 1.0f);
        }

        void OnGUI()
        {
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1));

            if (!_showMenu)
                _bubbleRect = GUI.Window(99, _bubbleRect, DrawBubble, "DRAG");
            else
                _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "SPRINTMASTER CONTROL");
        }

        void DrawBubble(int windowID)
        {
            if (GUI.Button(new Rect(10, 35, 180, 55), "MENU")) _showMenu = true;
            GUI.DragWindow(new Rect(0, 0, 200, 30));
        }

        void DrawMainWindow(int windowID)
        {
            GUILayout.BeginVertical();
            float val = Mathf.Floor(_level);
            _currentSpeed = (_selectedMode == 0) ? val : (_selectedMode == 1 ? 1f / val : 1f);
            
            GUILayout.Label($"<size=35>Race Speed: {_currentSpeed:F2}x</size>");

            if (GUILayout.Toggle(_selectedMode == 0, " SUPER FAST")) _selectedMode = 0;
            if (GUILayout.Toggle(_selectedMode == 1, " SUPER SLOW")) _selectedMode = 1;
            if (GUILayout.Toggle(_selectedMode == 2, " NORMAL")) _selectedMode = 2;

            _level = GUILayout.HorizontalSlider(_level, 1f, 10f);
            
            GUILayout.Space(20);
            if (GUILayout.Button("FORCE RACE START", GUILayout.Height(100))) { ApplyToRace(); }
            
            if (GUILayout.Button("CLOSE", GUILayout.Height(60))) { 
                PlayerPrefs.SetFloat("Mod_BubbleX", _bubbleRect.x);
                PlayerPrefs.SetFloat("Mod_BubbleY", _bubbleRect.y);
                PlayerPrefs.Save();
                _showMenu = false; 
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        void Update()
        {
            _pulseTimer += Time.deltaTime;
            if (_pulseTimer >= 0.5f) // Pulse very fast to stay ahead of the game logic
            {
                ApplyToRace();
                _pulseTimer = 0;
            }
        }

        private void ApplyToRace()
        {
            GameObject[] all = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            
            for (int i = 0; i < all.Length; i++)
            {
                GameObject obj = all[i];
                if (obj == null) continue;

                string n = obj.name.ToLower();
                
                // Targets based on your radar: Sprintmaster Runner (Layer 19) and Quest objects
                if (obj.layer == 19 || n.Contains("sprintmaster") || n.Contains("runner") || n.Contains("swift"))
                {
                    // Force the Master TimeScale of the object
                    obj.SendMessage("set_timeScale", _currentSpeed, SendMessageOptions.DontRequireReceiver);
                    
                    // Force PlayMaker Quest Logic
                    obj.SendMessage("SetFsmSpeed", _currentSpeed, SendMessageOptions.DontRequireReceiver);
                    obj.SendMessage("SetFsmTimeScale", _currentSpeed, SendMessageOptions.DontRequireReceiver);
                    
                    // Force the actual Runner speed
                    obj.SendMessage("SetSpeed", _currentSpeed, SendMessageOptions.DontRequireReceiver);
                    obj.SendMessage("set_speed", _currentSpeed, SendMessageOptions.DontRequireReceiver);

                    // Reach deep into the animations
                    Animator[] anims = obj.GetComponentsInChildren<Animator>(true);
                    for (int j = 0; j < anims.Length; j++) 
                    { 
                        anims[j].speed = _currentSpeed; 
                    }
                }
            }
        }
    }
}
