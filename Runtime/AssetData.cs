using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Agent.AssetLoader
{
    [System.Serializable]
    public class AssetData
    {
        [Tooltip("The Addressable Reference. Tip: Be sure that your level prefab is marked as addressable. If NULL the level loader will return an error.")]
        public AssetReferenceGameObject assetRef;

        [Tooltip("The transform this level asset will be parented. If null, it will be instantiated in the root of the scene.")]
        public Transform parentTarget;

        [Tooltip("This transform will be used to get the instantiation position. It is more handy to use a transform instead of a vector3 to work in your scene. If this component is null, the level loader will return an error.")]
        public Transform positionTarget;
    }
}
