using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Agent.AssetLoader
{
    [RequireComponent(typeof(BoxCollider))]
    public class AsyncAssetLoader : MonoBehaviour
    {
        #region instancing
        private BoxCollider _triggerCollider;

        public bool enableLoader = true;
        [Space(10)]
        [Tooltip("Colliders with this Layer can trigger the Asset Loader. Make sure this is possible by the physics matix in project settings.")]
        [SerializeField] private LayerMask collidableLayers;
        [Space(10)]
        [SerializeField] private bool unloadWhenTriggerExit;

        [Header("Assets")]
        [SerializeField] private List<AssetData> loadableAssets = new List<AssetData>();

        [SerializeField] private List<AssetData> currentlyLoadedAssets = new List<AssetData>();

        [Header("Debug")]
        [SerializeField] private bool enableDebug = false;
        [SerializeField] private bool enableDeepDebug = false;


        [System.Serializable]
        private struct AssetData
        {
            [Tooltip("The Addressable Reference. Tip: Be sure that your level prefab is marked as addressable. If NULL the level loader will return an error.")]
            public AssetReferenceGameObject assetRef;

            [Tooltip("The transform this level asset will be parented. If null, it will be instantiated in the root of the scene.")]
            public Transform parentTarget;

            [Tooltip("This transform will be used to get the instantiation position. It is more handy to use a transform instead of a vector3 to work in your scene. If this component is null, the level loader will return an error.")]
            public Transform positionTarget;

            [Tooltip("This is the gameobject reference when the asset was loaded.")]
            public GameObject sceneObjectReference;

            public string name;
        }
        #endregion

        #region unity loop
        private void Awake()
        {
            _triggerCollider = GetComponent<BoxCollider>();
        }

        private void OnEnable()
        {
            //set collider always to trigger
            if(_triggerCollider != null)
            {
                _triggerCollider.enabled = true;
                _triggerCollider.isTrigger = true;
            }
        }

        private void OnDisable()
        {

        }

        private void OnValidate()
        {
            //set collider always to trigger
            if (_triggerCollider != null)
            {
                _triggerCollider.enabled = true;
                _triggerCollider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider col)
        {
            if (enableDeepDebug)
                Debug.Log("TriggerEnter noticed: >" + col.name + "<");

            if (LayerMaskContainsLayer(collidableLayers, col.gameObject.layer))
            {
                for (int i = 0; i < loadableAssets.Count; i++)
                {
                    LoadAsset(loadableAssets[i]);
                }
            }
            else
            {
                if (enableDebug)
                    Debug.Log("Noticed Collider >" + col.name + "< didnt contain the right layer");
            }
        }

        private void OnTriggerExit(Collider col)
        {
            if(enableDeepDebug)
                Debug.Log("TriggerExit noticed: >" + col.name + "<");

            if (currentlyLoadedAssets.Count <= 0)
                return;

            if (!unloadWhenTriggerExit)
                return;

            if (LayerMaskContainsLayer(collidableLayers, col.gameObject.layer))
            {
                for (int i = 0; i < currentlyLoadedAssets.Count; i++)
                {
                    UnloadAsset(currentlyLoadedAssets[i]);
                }
            }
            else
            {
                if (enableDebug)
                    Debug.Log("Noticed Collider >" + col.name + "< didnt contain the right layer");
            }
        }
        #endregion

        #region loading methods
        private void LoadAsset(AssetData _asset)
        {
            if (_asset.assetRef == null)
            {
                if (enableDebug)
                    Debug.LogError("Tried to load asset wihtout addressable reference.");
                return;
            }

            if (enableDeepDebug)
                Debug.Log("Load asset called for: " + _asset.name);

            //check if this asset is already loaded
            //BUG
            if (!currentlyLoadedAssets.Contains(_asset))
            {
                //load addressable
                Addressables.LoadAssetAsync<GameObject>(_asset.assetRef).Completed += obj => LoadAssetIsDone(obj, _asset);
            }
        }

        private void UnloadAsset(AssetData _asset)
        {
            if (enableDeepDebug)
                Debug.Log("Unload asset called for: " + _asset);

            Addressables.ReleaseInstance(_asset.sceneObjectReference);
        }
        #endregion

        #region callbacks
        private void LoadAssetIsDone(AsyncOperationHandle<GameObject> _obj, AssetData _assetData)
        {
            //check if loaded
            if (_obj.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log("asset loaded: " + _obj.Result.transform.name);

                //get the instantiation data
                Vector3 pos = _assetData.positionTarget == null
                    ? transform.position
                    : _assetData.positionTarget.position;

                Quaternion rot = _assetData.positionTarget == null
                    ? transform.rotation
                    : _assetData.positionTarget.rotation;

                //instantiate the asset prefab
                if (_assetData.parentTarget != null)
                {
                    _assetData.sceneObjectReference = Instantiate(_obj.Result, pos, rot, _assetData.parentTarget);
                }
                else
                {
                    _assetData.sceneObjectReference = Instantiate(_obj.Result, pos, rot);
                }

                //add this stage to current loaded stage cache
                //BUG
                if (!currentlyLoadedAssets.Contains(_assetData))
                {
                    currentlyLoadedAssets.Add(_assetData);
                }
            }
        }

        private void UnloadAssetIsDone(AsyncOperationHandle<GameObject> _obj, AssetData _AssetData)
        {
            //check if loaded
            if (_obj.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log("asset unloaded: " + _obj);

                /*
                //add this stage to current loaded stage cache
                if (!currentlyLoadedAssets.Contains(_AssetData))
                {
                    currentlyLoadedAssets.Add(_AssetData);
                }
                */
            }
        }
        #endregion

        #region helper methods
        private AssetData[] GetCurrentLoadedAssets
        {
            get
            {
                AssetData[] assets = currentlyLoadedAssets.ToArray();
                return assets;
            }
        }

        private  bool AssetDataIsInconsistent(AssetData data1, AssetData data2)
        {
            return data1.assetRef == data2.assetRef;
        }

        private void CheckDataConsistency()
        {
            
        }

        /// <summary>
        /// Checks if the layer contains to the layermask.
        /// </summary>
        public static bool LayerMaskContainsLayer(LayerMask layermask, int layer)
        {
            return layermask == (layermask | (1 << layer));
        }
        #endregion

        #region GUI
        private void OnGUI()
        {
            if (!enableDebug)
                return;

            // Show loading button
            if (GUI.Button(new Rect(5, 5, 150, 30), "Load"))
            {
                for (int i = 0; i < loadableAssets.Count; i++)
                {
                    LoadAsset(loadableAssets[i]);
                }
            }

            // Show loading button
            if (GUI.Button(new Rect(5, 50, 150, 30), "Unload"))
            {
                for (int i = 0; i < currentlyLoadedAssets.Count; i++)
                {
                    UnloadAsset(currentlyLoadedAssets[i]);
                }
            }
        }

        private void OnDrawGizmos()
        {
            //Show trigger bounds
            if (_triggerCollider != null)
            {
                bool assetsLoaded = GetCurrentLoadedAssets.Length > 0;

                Gizmos.color = assetsLoaded ? new Color(0, 255, 0, 0.2f) : new Color(255, 0, 0, 0.2f);
                Gizmos.DrawCube(_triggerCollider.bounds.center, _triggerCollider.bounds.size);
            }
        }
        #endregion
    }
}

