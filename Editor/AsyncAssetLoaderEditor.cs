using UnityEngine;
using UnityEditor;

namespace Agent.AssetLoader
{
    [CustomEditor((typeof(AsyncAssetLoader)))]
    public class AsyncAssetLoaderEditor : Editor
    {
        private static AsyncAssetLoader _myTarget;

        //SceneView
        private static readonly Vector2 buttonSize = new Vector2(70, 30);
        private static readonly Color loadColor = new Color(0, 100, 0, 0.1f);
        private static readonly Color unloadColor = new Color(100, 0, 0, 0.1f);

        #region unity loop
        private void OnEnable()
        {
            _myTarget = (AsyncAssetLoader)target;

            SceneView.duringSceneGui += v => CastSceneViewEditor(_myTarget);

            if (SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.Repaint();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= v => CastSceneViewEditor(_myTarget);
        }

        //Show GUI in Inspector
        public override void OnInspectorGUI()
        {
            if (_myTarget == null)
                return;

            //check if the target is selected in hierarchy/inspector
            if (Selection.activeGameObject != _myTarget.gameObject)
                return;

            //HANDLE INSPECTOR GUI BUTTONS
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


            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Asset Data"))
            {
                _myTarget.RefreshAssetData();
            }
            EditorGUILayout.EndHorizontal();

            //DRAW CUSTOM INSPECTOR
            DrawDefaultInspector();
        }
        #endregion

        #region helper methods
        private static void CastSceneViewEditor(AsyncAssetLoader _myTarget)
        {
            if (_myTarget == null)
                return;

            //check if the target is selected in hierarchy/inspector
            if (Selection.activeGameObject != _myTarget.gameObject)
                return;

            CheckHotKey(_myTarget);

            Handles.BeginGUI();

            float screenHeight = SceneView.currentDrawingSceneView.position.size.y;

            Vector3 targetPos = _myTarget.gameObject.transform.position;
            Vector3 screenPoint = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(targetPos);

            // this prevents the GUI control from being drawn if you aren't looking at it
            if (screenPoint.z > 0)
            {
                Vector2 buttonPos1 = new Vector2(screenPoint.x - buttonSize.x * 0.5f, screenHeight - screenPoint.y - buttonSize.y - 50);

                if (!_myTarget.AssetsAreLoaded)
                {
                    GUI.backgroundColor = loadColor;
                    GUI.contentColor = Color.black;
                    if (GUI.Button(new Rect(buttonPos1, buttonSize), "Load!"))
                    {
                        _myTarget.LoadAllAssets();
                        Debug.Log("LOADING Assets called.");
                    }
                }
                else
                {
                    GUI.backgroundColor = unloadColor;
                    GUI.contentColor = Color.black;
                    if (GUI.Button(new Rect(buttonPos1, buttonSize), "Unload!"))
                    {
                        _myTarget.UnloadAllAssets();
                    }
                }
            }

            Handles.EndGUI();
        }

        /// <summary>
        /// This Method is linked to the Unity Menu to create a new Asset Loader.
        /// </summary>
        [MenuItem("GameObject/AssetLoader/Create..", false, 1)]
        public static void CreateLoader()
        {
            Debug.Log("Creating a level loader..");

            //focus scene view when created
            SceneView.lastActiveSceneView.Focus();

            //handle creation position
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

            //create new asset loader
            GameObject newLoader = new GameObject();
            newLoader.name = "LevelLoaderAsset";
            newLoader.AddComponent<AsyncAssetLoader>();
            newLoader.transform.position = targetPos;
        }

        /// <summary>
        /// placeholder to implement a shortcut
        /// </summary>
        public static void CheckHotKey(AsyncAssetLoader target)
        {
            if (target == null)
                return;

            //HANDLE TARGET TRANSFORM/OVERRIDES VISUALIZATION
            Event e = Event.current;
            switch (e.type)
            {
                case EventType.KeyUp:
                {
                    if (e.keyCode == (KeyCode.A))
                    {
                        Debug.Log("refreshing asset data..");
                        target.RefreshAssetData();
                    }
                    break;
                }
            }
        }
        #endregion
    }
}
