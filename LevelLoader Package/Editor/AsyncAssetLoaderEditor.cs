using UnityEngine;
using UnityEditor;

namespace Agent.AssetLoader
{
    [CustomEditor((typeof(AsyncAssetLoader)))]
    public class AsyncAssetLoaderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            AsyncAssetLoader myTarget = (AsyncAssetLoader)target;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load All"))
            {
                myTarget.LoadAllAssets();
            }

            if (GUILayout.Button("Unload All"))
            {
                myTarget.UnloadAllAssets();
            }
            EditorGUILayout.EndHorizontal();


            //draw inspector after custom buttons
            DrawDefaultInspector();
        }
    }
}
