using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HKDebug.Menu
{
    public delegate void ButtonSubmit(ButtonInfo button);
    public class ButtonInfo
    {
        public string label = "";
        public ButtonSubmit submit = null;
    }
    public static class MenuManager
    {
        public static List<ButtonInfo> buttons = new List<ButtonInfo>();
        public static int select = 0;
        public static bool HasButton => buttons.Count != 0;
        
        public static void Check() => select = Mathf.Clamp(select, 0, buttons.Count - 1);
        public static void AddButton(ButtonInfo but)
        {
            buttons.Add(but);
        }
        public static void Submit()
        {
            Check();
            var but = buttons[select];
            but.submit?.Invoke(but);
        }
    }
}
