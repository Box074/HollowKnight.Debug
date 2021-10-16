using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;

namespace HKDebug
{
    class Logo
    {
        public static Texture2D logo = null;
        public static GameObject logoC = null;
        public static void Init()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HKDebug.logo.png"))
            {
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);
                logo = new Texture2D(1, 1);
                logo.LoadImage(buffer, true);
            }
            LogoTest();
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        }
        static void LogoTest()
        {
            if (logoC == null)
            {
                
                GameObject dlc = GameObject.Find("Hidden_Dreams_Logo");
                if (dlc != null)
                {
                    logoC = UnityEngine.Object.Instantiate(dlc, dlc.transform.parent);
                    logoC.SetActive(true);
                    var pos = dlc.transform.position;
                    logoC.transform.position = pos - new Vector3(1.4f,0.1f);
                    logoC.transform.localScale *= 0.1f;
                    logoC.GetComponent<SpriteRenderer>().sprite = Sprite.Create(logo,
                        new Rect(0, 0, logo.width, logo.height), Vector2.one * 0.5f);
                }
            }
        }

        private static void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
        {
            LogoTest();
        }
    }
}
