using UnityEngine;
using UnityEditor;

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
            _myTarget = (AsyncAssetLoader)target;

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

                if (!_myTarget.CurrentlyAssetsLoaded)
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
        #endregion
    }
}
