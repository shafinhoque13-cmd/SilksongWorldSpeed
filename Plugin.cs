using BepInEx;
using UnityEngine;
using System;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.2.1")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        private bool _showMenu = false;
        private Rect _bubbleRect = new Rect(50, 300, 200, 100); 
        private Rect _windowRect = new Rect(100, 100, 550, 650);
        
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
            {
                _bubbleRect = GUI.Window(99, _bubbleRect, DrawBubble, "DRAG");
            }
            else
            {
                _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "SWIFT SPEED CONTROL");
                DrawEntityScanner();
            }
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
            
            GUILayout.Label($"<size=35>Target: {_currentSpeed:F2}x</size>");

            if (GUILayout.Toggle(_selectedMode == 0, " FAST")) _selectedMode = 0;
            if (GUILayout.Toggle(_selectedMode == 1, " SLOW")) _selectedMode = 1;
            if (GUILayout.Toggle(_selectedMode == 2, " NORMAL")) _selectedMode = 2;

            _level = GUILayout.HorizontalSlider(_level, 1f, 10f);
            
            GUILayout.Space(30);
            if (GUILayout.Button("FORCE SWIFT SPEED", GUILayout.Height(90))) { ApplyToNpc(); }
            
            if (GUILayout.Button("SAVE & EXIT", GUILayout.Height(70))) { 
                PlayerPrefs.SetFloat("Mod_BubbleX", _bubbleRect.x);
                PlayerPrefs.SetFloat("Mod_BubbleY", _bubbleRect.y);
                PlayerPrefs.Save();
                _showMenu = false; 
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        void DrawEntityScanner()
        {
            GameObject[] all = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int yOffset = 0;
            GUI.Box(new Rect(10, 700, 750, 350), "ACTIVE NEARBY ENTITIES");
            
            foreach (var obj in all)
            {
                if (obj == null || !obj.activeInHierarchy) continue;

                float dist = Vector3.Distance(obj.transform.position, Camera.main.transform.position);
                if (dist < 15f) 
                {
                    string color = obj.name.ToUpper().Contains("SWIFT") ? "yellow" : "white";
                    GUI.Label(new Rect(20, 740 + (yOffset * 30), 700, 30), $"<color={color}>[{obj.layer}] {obj.name}</color>");
                    yOffset++;
                    if (yOffset > 10) break; 
                }
            }
        }

        void Update()
        {
            _pulseTimer += Time.deltaTime;
            if (_pulseTimer >= 1.5f) 
            {
                ApplyToNpc();
                _pulseTimer = 0;
            }
        }

        private void ApplyToNpc()
        {
            GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            for (int i = 0; i < allObjects.Length; i++)
            {
                GameObject obj = allObjects[i];
                if (obj == null) continue;
                
                string n = obj.name.ToUpper();

                if (n.Contains("SPRINT") || n.Contains("SPEED") || n.Contains("SWIFT"))
                {
                    // Logic speed
                    obj.SendMessage("SetFsmSpeed", _currentSpeed, SendMessageOptions.DontRequireReceiver);
                    obj.SendMessage("SetFsmTimeScale", _currentSpeed, SendMessageOptions.DontRequireReceiver);

                    // Animation speed
                    obj.SendMessage("set_timeScale", _currentSpeed, SendMessageOptions.DontRequireReceiver);
                    
                    Animator[] anims = obj.GetComponentsInChildren<Animator>(true);
                    for (int j = 0; j < anims.Length; j++) 
                    { 
                        anims[j].speed = _currentSpeed; 
                    }

                    // Physics movement
                    obj.SendMessage("SetSpeed", _currentSpeed, SendMessageOptions.DontRequireReceiver);
                    obj.SendMessage("set_speed", _currentSpeed, SendMessageOptions.DontRequireReceiver);
                }
            }
        }
    }
}
