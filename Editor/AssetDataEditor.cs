using UnityEngine;
using UnityEditor;

namespace Agent.AssetLoader
{
    [CustomEditor((typeof(AssetData)))]
    public class AssetDataEditor : Editor
    {
        #region instancing
        private static AssetData _myTarget;
        #endregion

        #region unity loop
        private void OnEnable()
        {
            //_myTarget = (AssetData)target;
        }

        private void OnDisable()
        {

        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginHorizontal();


            EditorGUILayout.EndHorizontal();
        }
        #endregion
    }
}
