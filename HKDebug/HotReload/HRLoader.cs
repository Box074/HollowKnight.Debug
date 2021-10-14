using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modding;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UObject = UnityEngine.Object;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using Logger = Modding.Logger;

namespace HKDebug.HotReload
{
    static class HRLoader
    {
        private static Dictionary<string, Dictionary<string, GameObject>> PreloadCaches =
            new Dictionary<string, Dictionary<string, GameObject>>();

        private static IEnumerator PreloadScenes(
            GameObject coroutineHolder,
            Dictionary<string, List<string>> toPreload
        )
        {
            yield return USceneManager.LoadSceneAsync("Quit_To_Menu");

            while (USceneManager.GetActiveScene().name != Constants.MENU_SCENE)
            {
                yield return new WaitForEndOfFrame();
            }
            // Mute all audio
            AudioListener.pause = true;

            // Create a blanker so the preloading is invisible
            GameObject blanker = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920, 1080));
            UObject.DontDestroyOnLoad(blanker);

            var nb = coroutineHolder.GetComponent<NonBouncer>();

            CanvasUtil.CreateImagePanel(
                    blanker,
                    CanvasUtil.NullSprite(new byte[] { 0x00, 0x00, 0x00, 0xFF }),
                    new CanvasUtil.RectData(Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one)
                )
                .GetComponent<Image>()
                .preserveAspect = false;

            // Create loading bar background
            CanvasUtil.CreateImagePanel(
                    blanker,
                    CanvasUtil.NullSprite(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }),
                    new CanvasUtil.RectData
                    (
                        new Vector2(1000, 100),
                        Vector2.zero,
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f)
                    )
                )
                .GetComponent<Image>()
                .preserveAspect = false;

            // Create actual loading bar
            GameObject loadingBar = CanvasUtil.CreateImagePanel(
                blanker,
                CanvasUtil.NullSprite(new byte[] { 0x99, 0x99, 0x99, 0xFF }),
                new CanvasUtil.RectData(
                    new Vector2(0, 75),
                    Vector2.zero,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f)
                )
            );

            loadingBar.GetComponent<Image>().preserveAspect = false;
            RectTransform loadingBarRect = loadingBar.GetComponent<RectTransform>();

            // Preload all needed objects
            int progress = 0;

            void updateLoadingBarProgress()
            {
                loadingBarRect.sizeDelta = new Vector2(
                    progress / (float)toPreload.Count * 975,
                    loadingBarRect.sizeDelta.y
                );
            }

            IEnumerator PreloadScene(string s)
            {

                updateLoadingBarProgress();
                yield return USceneManager.LoadSceneAsync(s, LoadSceneMode.Additive);
                updateLoadingBarProgress();

                Scene scene = USceneManager.GetSceneByName(s);
                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (var go in rootObjects)
                {
                    go.SetActive(false);
                }

                // Fetch object names to preload
                List<string> objNames = toPreload[s];


                foreach (string objName in objNames)
                {

                    // Split object name into root and child names based on '/'
                    string rootName;
                    string childName = null;

                    int slashIndex = objName.IndexOf('/');
                    if (slashIndex == -1)
                    {
                        rootName = objName;
                    }
                    else if (slashIndex == 0 || slashIndex == objName.Length - 1)
                    {
                        continue;
                    }
                    else
                    {
                        rootName = objName.Substring(0, slashIndex);
                        childName = objName.Substring(slashIndex + 1);
                    }

                    // Get root object
                    GameObject obj = rootObjects.FirstOrDefault(o => o.name == rootName);
                    if (obj == null)
                    {
                        continue;
                    }

                    // Get child object
                    if (childName != null)
                    {
                        Transform t = obj.transform.Find(childName);
                        if (t == null)
                        {
                            continue;
                        }

                        obj = t.gameObject;
                    }

                    // Create inactive duplicate of requested object
                    obj = UObject.Instantiate(obj);
                    UObject.DontDestroyOnLoad(obj);
                    obj.SetActive(false);

                    // Set object to be passed to mod
                    Dictionary<string, GameObject> g;
                    if (PreloadCaches.TryGetValue(s, out var v))
                    {
                        g = v;
                    }
                    else
                    {
                        g = new Dictionary<string, GameObject>();
                        PreloadCaches.Add(s, g);
                    }
                    g[objName] = obj;
                }


                // Update loading progress
                progress++;

                updateLoadingBarProgress();
                yield return USceneManager.UnloadSceneAsync(scene);
                updateLoadingBarProgress();
            }

            List<IEnumerator> batch = new List<IEnumerator>();
            int maxKeys = toPreload.Keys.Count;

            foreach (string sceneName in toPreload.Keys)
            {
                int batchCount = Math.Min(ModHooks.GlobalSettings.PreloadBatchSize, maxKeys);

                batch.Add(PreloadScene(sceneName));

                if (batch.Count < batchCount)
                    continue;

                Coroutine[] coros = batch.Select(nb.StartCoroutine).ToArray();

                foreach (var coro in coros)
                    yield return coro;

                batch.Clear();

                maxKeys -= batchCount;
            }

            // Reload the main menu to fix the music/shaders

            yield return USceneManager.LoadSceneAsync("Quit_To_Menu");

            while (USceneManager.GetActiveScene().name != Constants.MENU_SCENE)
            {
                yield return new WaitForEndOfFrame();
            }

            // Remove the black screen
            UObject.Destroy(blanker);

            // Restore the audio
            AudioListener.pause = false;
        }
    }
}
