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
        void OnGUI()
        {
            if (bg == null)
            {
                bg = new Texture2D(1, 1);
                bg.SetPixel(0, 0, new Color(0, 0, 0, 0.5f));
            }
            if (!MenuManager.HasButton || !show) return;
            GUI.DrawTexture(new Rect(20, 20, 256, 512), bg);
            GUILayout.BeginArea(new Rect(20, 20, 256, 512));
            GUILayout.BeginVertical();
            for(int i = 0; i < MenuManager.buttons.Count; i++)
            {
                GUILayout.Label((MenuManager.select == i ? "->" : "  ") + MenuManager.buttons[i].label);
            }
            GUILayout.EndVertical();
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
