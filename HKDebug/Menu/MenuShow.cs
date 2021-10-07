using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HKDebug.Menu
{
    class MenuShow : MonoBehaviour
    {
        Texture2D bg = null;
        bool show = true;
        Vector2 v = Vector2.zero;
        void OnGUI()
        {
            if (bg == null)
            {
                bg = new Texture2D(1, 1);
                bg.SetPixel(0, 0, new Color(0, 0, 0, 0.5f));
            }
            if (!MenuManager.HasButton || !show) return;
            List<ButtonInfo> buttons = MenuManager.Buttons;
            if (buttons == null)
            {
                MenuManager.LeaveGroup();
                return;
            }
            if (buttons.Count == 0)
            {
                MenuManager.LeaveGroup();
                return;
            }
            GUI.DrawTexture(new Rect(20, 20, 256, 512), bg);
            GUILayout.BeginArea(new Rect(20, 20, 256, 512));
            v = GUILayout.BeginScrollView(v);
            GUILayout.BeginVertical();
            if (MenuManager.groups.Count != 0)
            {
                GUILayout.Label((MenuManager.select == -1 ? "->" : "  ") + "返回");
            }
            for (int i = 0; i < buttons.Count; i++)
            {
                GUILayout.Label((MenuManager.select == i ? "->" : "  ") + buttons[i].label);
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();

        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Home)) show = !show;
            if (!show) return;
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                MenuManager.select--;
                MenuManager.Check();
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                MenuManager.select++;
                MenuManager.Check();
            }
            if (Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                MenuManager.Submit();
            }
        }
    }
}
