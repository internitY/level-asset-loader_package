using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Agent.AssetLoader
{
    [ExecuteAlways]
    [RequireComponent(typeof(BoxCollider))]
    public class AsyncAssetLoader : MonoBehaviour
    {
        #region instancing
        private BoxCollider _triggerCollider;
        private BoxCollider _customTestCollider;

        [Space(10)]
        [Tooltip("Colliders with this Layer can trigger the Asset Loader. Make sure this is possible by the physics matix in project settings.")]
        [SerializeField] private LayerMask collidableLayers;
        [Space(10)]
        [SerializeField] private bool unloadWhenTriggerExit;

        public bool UnloadWhenTriggerExit
        {
            get => unloadWhenTriggerExit;
            set => unloadWhenTriggerExit = value;
        }

        [Header("Assets")]
        [SerializeField] private List<AssetData> loadableAssets = new List<AssetData>();
        [SerializeField] private List<LoadReference> currentlyLoadedInstances = new List<LoadReference>();

        [System.Serializable]
        public struct LoadReference
        {
            public AssetReference assetReference;
            public GameObject instance;
            public Transform targetedParent;
            public Transform targetedTransform;
            public Vector3 targetedPosition;
            public Quaternion targetedRotation;
        }

        [Header("Debug")]
        [SerializeField] private bool enableDebug = false;
        [SerializeField] private bool enableHelperGUI = false;
        [SerializeField] private bool enableDeepDebug = false;
        #endregion

        #region unity loop
        private void Awake()
        {
            _triggerCollider = GetComponent<BoxCollider>();
        }

        private void OnEnable()
        {
            UnloadAllAssets();
        }

        private void OnDisable()
        {
            UnloadAllAssets();
        }

        private void OnValidate()
        {
            if(_triggerCollider == null)
                _triggerCollider = GetComponent<BoxCollider>();

            //set collider always to trigger
            if (_triggerCollider != null)
            {
                _triggerCollider.enabled = true;
                _triggerCollider.isTrigger = true;
            }

            //subscribe refresher to avoid sendMessage warnings
            UnityEditor.EditorApplication.delayCall += RefreshAssetData;

        }

        private void OnTriggerEnter(Collider col)
        {
            if (enableDeepDebug)
                Debug.Log("TriggerEnter noticed: >" + col.name + "<");

            if (loadableAssets.Count <= 0)
                return;

            if (LayerMaskContainsLayer(collidableLayers, col.gameObject.layer))
            {
                LoadAllAssets();
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

            if (currentlyLoadedInstances.Count <= 0)
            {
                if (enableDeepDebug)
                    Debug.Log("No currently loaded assets to unload.");
                return;
            }

            if (!unloadWhenTriggerExit)
                return;

            if (LayerMaskContainsLayer(collidableLayers, col.gameObject.layer))
            {
                UnloadAllAssets();
            }
            else
            {
                if (enableDebug)
                    Debug.Log("Noticed Collider >" + col.name + "< didnt contain the right layer");
            }
        }
        #endregion

        #region loading methods
        /// <summary>
        /// Loads all registered asset references inside the loadableAssets list. Currently this only works while application is running.
        /// </summary>
        public void LoadAllAssets()
        {
            if (loadableAssets.Count <= 0)
            {
                if(enableDebug)
                    Debug.LogWarning("No loadable assets found. Loading returned");
                return;
            }

            for (int i = 0; i < loadableAssets.Count; i++)
            {
                AssetData _assetData = loadableAssets[i];

                if (_assetData.assetRef == null)
                {
                    if (enableDebug)
                        Debug.LogError("Tried to load asset wihtout addressable reference.");
                    return;
                }

                if (enableDeepDebug)
                    Debug.Log("Load asset called for: " + _assetData.assetRef.editorAsset.gameObject.transform.name);

                //check if this asset is already loaded
                if (!AssetIsCurrentlyLoaded(_assetData.assetRef))
                {
                    //get the instantiation data
                    Vector3 pos = _assetData.transformTarget == null
                        ? _assetData.positionOverride
                        : _assetData.transformTarget.position;

                    Quaternion rot = _assetData.transformTarget == null
                        ? _assetData.rotationOverride
                        : _assetData.transformTarget.rotation;

                    //instantiate addressable
                    Addressables.InstantiateAsync(_assetData.assetRef, pos, rot, _assetData.parentTarget).Completed += obj => InstantiateAssetIsDone(obj, _assetData);
                }
                else
                {
                    if (enableDebug)
                        Debug.LogWarning("asset >" + _assetData.assetRef.editorAsset.gameObject.transform.name + "< is currently loaded");
                }
            }
        }

        private void InstantiateAssetIsDone(AsyncOperationHandle<GameObject> _obj, AssetData _data)
        {
            if (_obj.Status == AsyncOperationStatus.Failed || _obj.Status == AsyncOperationStatus.None)
            {
                if (enableDebug)
                    Debug.LogWarning("Instantiaten of object: " + _obj.DebugName + " failed.");
            }

            //check if loaded
            if (_obj.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log("asset loaded: " + _obj.Result.transform.name);

                if (_obj.Result != null)
                {
                    LoadReference reference;
                    reference.instance = _obj.Result;
                    reference.assetReference = _data.assetRef;
                    reference.targetedTransform = _data.transformTarget;
                    reference.targetedPosition = _data.positionOverride;
                    reference.targetedRotation = _data.rotationOverride;
                    reference.targetedParent = _data.parentTarget;

                    //add the reference to current loaded assets
                    currentlyLoadedInstances.Add(reference);

                    //public callback
                    OnLoadingDone();
                }
            }
        }

        /// <summary>
        /// OnLoadingDone() gets called after all loadedable assets was instantiated by Addressables InstantiateAsync.
        /// </summary>
        public virtual void OnLoadingDone()
        {
            if (enableDeepDebug)
                Debug.Log("OnLoadingDone() called.");
        }
        #endregion

        #region unloading methods
        /// <summary>
        /// Unloads all cached instantiated instances.
        /// </summary>
        public void UnloadAllAssets()
        {
            if (enableDeepDebug)
                Debug.Log("Unload assets called.");

            if (currentlyLoadedInstances.Count <= 0)
            {
                if (enableDebug)
                    Debug.LogWarning("No currently loaded assets found. Unloading returned.");
                return;
            }

            //unload all cached loaded assets
            for (int i = 0; i < currentlyLoadedInstances.Count; i++)
            {
                if (currentlyLoadedInstances != null)
                {
                    //Release Assets
                    Addressables.ReleaseInstance(currentlyLoadedInstances[i].instance);
                }
            }

            //clear the cache list
            currentlyLoadedInstances.Clear();

            //public callback
            OnUnloadingDone();
        }

        /// <summary>
        /// OnUnloadingDone() gets called after all loaded assets gets unloaded by Addressables ReleaseInstance.
        /// </summary>
        public virtual void OnUnloadingDone()
        {
            if(enableDeepDebug)
                Debug.Log("OnUnloadingDone() called.");
        }
        #endregion

        #region helpers
        /// <summary>
        /// Return alls currently loaded Assets. This can be null, if no asset is loaded.
        /// </summary>
        private LoadReference[] GetCurrentLoadedAssets => currentlyLoadedInstances.ToArray();

        /// <summary>
        /// Return the first laoded asset by the asset reference.
        /// </summary>
        private LoadReference GetLoadedAsset(AssetReference assetRef)
        {
            return currentlyLoadedInstances.Find(i => i.assetReference == assetRef);
        }

        /// <summary>
        /// Checks if there are some loaded assets inside the currentlyLoadedInstances List.
        /// </summary>
        public bool AssetsAreLoaded => currentlyLoadedInstances.Count > 0;

        /// <summary>
        /// Checks if the specific asset reference is loaded by this loader.
        /// </summary>
        private bool AssetIsCurrentlyLoaded(AssetReference assetRef)
        {
            if (currentlyLoadedInstances.Any(i => i.assetReference == assetRef))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the layer contains to the layermask.
        /// </summary>
        private static bool LayerMaskContainsLayer(LayerMask layermask, int layer)
        {
            return layermask == (layermask | (1 << layer));
        }
        #endregion

        #region data resetter
        /// <summary>
        /// Resets the position to the target position and rotation. This method is linked to the refresher button in inspector.
        /// </summary>
        public void RefreshAssetData()
        {
            if (!AssetsAreLoaded)
            {
                if(enableDebug)
                    Debug.LogWarning("No currently loaded assets found.");
                return;
            }

            if(enableDebug)
                Debug.Log("Repositioning loaded assets..");

            for (int i = 0; i < currentlyLoadedInstances.Count; i++)
            {
                //find the targeted reference and create new one
                LoadReference reference = currentlyLoadedInstances.Find(x => x.assetReference == currentlyLoadedInstances[i].assetReference);

                //find the origin loadable of the currently loaded reference
                AssetData origin = loadableAssets.Find(x => x.assetRef == reference.assetReference);

                if (origin != null && reference.instance != null)
                {
                    //handle position and rotation
                    //handle by given target
                    if (origin.transformTarget != null)
                    {
                        //reset the target (this may occur if the target was changed while runtime
                        reference.targetedTransform = origin.transformTarget;

                        //reset the pos and rot by the target pos and rot
                        reference.instance.transform.position = reference.targetedTransform.position;
                        reference.instance.transform.rotation = reference.targetedTransform.rotation;
                    }
                    //otherwise handle by pos and rot overrides
                    else
                    {
                        reference.targetedTransform = null;

                        //BUG: this seems not to work?
                        //re-cache pos and rot
                        reference.targetedPosition = origin.positionOverride;
                        reference.targetedRotation = origin.rotationOverride;

                        //set the instanced gameobject to cached pos and rot
                        reference.instance.transform.position = reference.targetedPosition;
                        reference.instance.transform.rotation = reference.targetedRotation;
                    }

                    //handle parenting
                    if (origin.parentTarget != null)
                    {
                        reference.targetedParent = origin.parentTarget;
                        reference.instance.transform.parent = reference.targetedParent;
                    }
                    else
                    {
                        reference.targetedParent = null;
                        reference.instance.transform.parent = null;
                    }

                    //recache new reference
                    currentlyLoadedInstances[i] = reference;
                }
                else
                {
                    //Debug.LogError("The Resetter could not found a origin and/or a loaded instance: " + origin + " | " + reference.instance);
                }
            }
        }
        #endregion

        #region bound casting methods
        /// <summary>
        /// placeholder: Configuring a custom trigger collider to hide unitys box collider component.
        /// </summary>
        private void InitializeTriggerVolume()
        {
            _customTestCollider = new BoxCollider();
            _customTestCollider.center = transform.position;
            _customTestCollider.size = Vector3.one;

            _customTestCollider.isTrigger = true;
        }

        #endregion

        #region GUI
        private void OnGUI()
        {
            if (!enableDeepDebug)
                return;

            // Show loading button
            if (GUI.Button(new Rect(5, 5, 150, 30), "Load All"))
            {
                LoadAllAssets();
            }

            // Show loading button
            if (GUI.Button(new Rect(5, 50, 150, 30), "Unload All"))
            {
                UnloadAllAssets();
            }
        }

        private void OnDrawGizmos()
        {
            //Show trigger bounds
            if (_triggerCollider != null)
            {
                Gizmos.color = AssetsAreLoaded ? new Color(0, 255, 0, 0.2f) : new Color(255, 0, 0, 0.2f);
                Gizmos.DrawCube(_triggerCollider.bounds.center, _triggerCollider.bounds.size);
            }

            if (loadableAssets.Count <= 0)
                return;

            if (enableHelperGUI)
            {
                for (int i = 0; i < loadableAssets.Count; i++)
                {
                    //visualize override pos
                    if (loadableAssets[i].transformTarget == null)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireSphere(loadableAssets[i].positionOverride, 0.2f);
                    }
                    //visualize targets
                    else
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireSphere(loadableAssets[i].transformTarget.position, 0.2f);
                    }
                }
            }
        }
        #endregion
    }
}

