using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Agent.AssetLoader
{
    [System.Serializable]
    public class LoadReference
    {
        public AssetReference assetReference;
        public GameObject instance;
        public Transform targetedParent;
        public Transform targetedTransform;
        public Vector3 targetedPosition;
        public Quaternion targetedRotation;
    }
}