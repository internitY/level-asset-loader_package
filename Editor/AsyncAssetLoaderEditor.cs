using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace Agent.AssetLoader
{
    [CustomEditor((typeof(AsyncAssetLoader)))]
    public class AsyncAssetLoaderEditor : Editor
    {
        private static AsyncAssetLoader _myTarget;

        //SceneView
        private static Vector2 buttonSize = new Vector2(70, 30);
        private static Color loadColor = new Color(0, 100, 0, 0.1f);
        private static Color unloadColor = new Color(100, 0, 0, 0.1f);

        #region unity loop
        private void OnEnable()
        {
            _myTarget = (AsyncAssetLoader)target;

            SceneView.duringSceneGui += v => CastSceneViewEditor(_myTarget);

            if (SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.Repaint();
        }

        private void OnDisable()
        {
            // if you don't do this Unity throws a hissy fit when you select another object
            SceneView.duringSceneGui -= v => CastSceneViewEditor(_myTarget);
        }

        //Show GUI in Inspector
        public override void OnInspectorGUI()
        {
            if (_myTarget == null)
                return;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load All"))
            {
                _myTarget.LoadAllAssets();
            }

            if (GUILayout.Button("Unload All"))
            {
                _myTarget.UnloadAllAssets();
            }

            EditorGUILayout.EndHorizontal();


            //draw inspector after custom buttons
            DrawDefaultInspector();
        }
        #endregion

        #region helper methods
        private static void CastSceneViewEditor(AsyncAssetLoader _myTarget)
        {
            if (_myTarget == null)
                return;

            Handles.BeginGUI();

            float screenHeight = SceneView.currentDrawingSceneView.position.size.y;

            Vector3 targetPos = _myTarget.gameObject.transform.position;
            Vector3 screenPoint = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(targetPos);

            // this prevents the GUI control from being drawn if you aren't looking at it
            if (screenPoint.z > 0)
            {
                Vector2 buttonPos1 = new Vector2(screenPoint.x - buttonSize.x * 0.5f, screenHeight - screenPoint.y - buttonSize.y - 50);

                if (!_myTarget.AssetsLoaded)
                {
                    GUI.backgroundColor = loadColor;
                    GUI.contentColor = Color.black;
                    if (GUI.Button(new Rect(buttonPos1, buttonSize), "Load!"))
                    {
                        _myTarget.LoadAllAssets();

                        if (!Application.isPlaying)
                        {
                            Debug.LogWarning("LOADING Assets called. Actually this is may not working properly in Edit Mode.");
                        }
                        else
                        {
                            Debug.Log("LOADING Assets called.");
                        }
                    }
                }
                else
                {
                    GUI.backgroundColor = unloadColor;
                    GUI.contentColor = Color.black;
                    if (GUI.Button(new Rect(buttonPos1, buttonSize), "Unload!"))
                    {
                        _myTarget.UnloadAllAssets();

                        if (!Application.isPlaying)
                        {
                            Debug.LogWarning("UNLOADING Assets called. Actually this is may not working properly in Edit Mode.");
                        }
                        else
                        {
                            Debug.Log("UNLOADING Assets called.");
                        }
                    }
                }
            }

            Handles.EndGUI();
        }

        [MenuItem("GameObject/Agent/Create Asset Loader..")]
        public static void CreateLoader()
        {
            Debug.Log("Creating a level loader..");

            SceneView.lastActiveSceneView.Focus();

            float screenHeight = SceneView.lastActiveSceneView.camera.pixelHeight;
            float screenWidth = SceneView.lastActiveSceneView.camera.pixelWidth;
            Vector2 screenCenter = new Vector2(screenWidth * 0.5f, screenHeight * 0.5f);

            Ray ray = SceneView.lastActiveSceneView.camera.ScreenPointToRay(new Vector3(screenCenter.x, screenCenter.y, 1f));
            RaycastHit hit;

            Vector3 targetPos;

            if (Physics.Raycast(ray, out hit))
            {
                targetPos = hit.point;
            }
            else
            {
                targetPos = Vector3.zero;
            }

            GameObject newLoader = new GameObject();
            newLoader.name = "LevelLoaderAsset";
            newLoader.AddComponent<AsyncAssetLoader>();
            newLoader.transform.position = targetPos;
        }
        #endregion
    }
}
