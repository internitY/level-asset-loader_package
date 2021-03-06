﻿using System.Collections.Generic;
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
        private BoxCollider _customTestCollider;

        [Header("GENERAL")]
        /// <summary>
        /// https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnTriggerEnter.html
        /// https://docs.unity3d.com/Manual/LayerBasedCollision.html
        /// </summary>
        [Tooltip("Colliders/Rigidbodies with this Layer triggers asset loading/unloading. Make sure this is possible by the physics matix in your project settings.")]
        [SerializeField] private LayerMask collidableLayers = 0;
        [Space(5)]
        [Tooltip("If this is true, collidables can unload by existing the trigger volume.")]
        [SerializeField] private bool unloadWhenTriggerExit;
        public bool UnloadWhenTriggerExit
        {
            get => unloadWhenTriggerExit;
            set => unloadWhenTriggerExit = value;
        }

        [Header("ASSETS TO LOAD")]
        [Tooltip("List of Assets that will be loaded.")]
        [SerializeField] private List<AssetData> loadableAssets = new List<AssetData>();
        [Space(20)]
        [Tooltip("List of currently loaded assets.")]
        [SerializeField] private List<LoadReference> currentlyLoadedInstances = new List<LoadReference>();

        [Header("DEBUG")]
        [Tooltip("Enables/Disables simple Debug Messages.")]
        [SerializeField] private bool enableDebug = false;
        [Tooltip("Show/Hide the GUI coordination helper (yellow = transform target, green = pos and rot override).")]
        [SerializeField] private bool enableHelperGUI = false;
        [Tooltip("Enables/Disables more specific Debug Messages.")]
        [SerializeField] private bool enableDeepDebug = false;

            #if UNITY_EDITOR
        private Mesh coordMesh = null;
        
        public Mesh CoordArrows
        {
            private set => coordMesh = value;
            get
            {
                if (coordMesh == null)
                {
                    coordMesh = (Mesh)UnityEditor.AssetDatabase.LoadAssetAtPath("Packages/com.level.loader/Assets/coord arrows.FBX", typeof(Mesh));

                    if (enableDeepDebug)
                        Debug.Log("gizmo mesh found: " + coordMesh.name);
                }

                return coordMesh;
            }
        }
            #endif

        #endregion

        #region unity loop
        private void Awake()
        {
            _triggerCollider = GetComponent<BoxCollider>();
        }

        private void Start()
        {
            #if UNITY_EDITOR
            CoordArrows = (Mesh)UnityEditor.AssetDatabase.LoadAssetAtPath("Packages/com.level.loader/Assets/coord arrows.FBX", typeof(Mesh));
            #endif
        }

        private void OnEnable()
        {
            UnloadAllAssets();

            #if UNITY_EDITOR
            //subscribe play mode checker to unload before exiting play mode to avoid warning
            UnityEditor.EditorApplication.playModeStateChanged += state => LogPlayModeState(state, this);
            #endif
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

            RefreshAssetData();
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
                        Debug.LogError("Addressable reference is null.");
                    continue;
                }

                #if UNITY_EDITOR
                if (enableDeepDebug)
                    Debug.Log("Load asset called for: " + _assetData.assetRef.editorAsset.gameObject.transform.name);
                #endif

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
                        Addressables.InstantiateAsync(_assetData.assetRef, pos, rot, _assetData.parentTarget).Completed
                            += obj => InstantiateAssetIsDone(obj, _assetData);
                }
                else
                {
                    #if UNITY_EDITOR
                    if (enableDebug)
                        Debug.LogWarning("asset >" + _assetData.assetRef.editorAsset.gameObject.transform.name + "< is currently loaded");
                    #endif
                }
            }

            //public callback
            OnLoadingDone();
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
                    LoadReference reference = new LoadReference
                    {
                        instance = _obj.Result,
                        assetReference = _data.assetRef,
                        targetedTransform = _data.transformTarget,
                        targetedPosition = _data.positionOverride,
                        targetedRotation = _data.rotationOverride,
                        targetedParent = _data.parentTarget
                    };

                    //add the reference to current loaded assets
                    currentlyLoadedInstances.Add(reference);
                }
            }
        }

        /// <summary>
        /// OnLoadingDone() gets called after all loadedable assets was instantiated by Addressables InstantiateAsync.
        /// </summary>
        public virtual void OnLoadingDone()
        {
            if (enableDebug)
                Debug.Log("OnLoadingDone() called by instance: " + transform.name);
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
                    Debug.Log("asset unloading: " + currentlyLoadedInstances[i].instance.transform.name);

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
            if(enableDebug)
                Debug.Log("OnUnloadingDone() called by instance: " + transform.name);
        }
        #endregion

        #region helpers
        /// <summary>
        /// Returns all currently loaded Assets. This can be null, if no asset is loaded.
        /// </summary>
        private LoadReference[] GetCurrentLoadedAssets => currentlyLoadedInstances.ToArray();

        /// <summary>
        /// Returns the first laoded asset by the asset reference.
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
        /// future feature: Configuring a custom trigger collider to hide unitys box collider component.
        /// </summary>
        private void InitializeTriggerVolume()
        {
            _customTestCollider = new BoxCollider();
            _customTestCollider.center = transform.position;
            _customTestCollider.size = Vector3.one;

            _customTestCollider.isTrigger = true;
        }

        #endregion

        #if UNITY_EDITOR
        private static void LogPlayModeState(UnityEditor.PlayModeStateChange state, AsyncAssetLoader loader)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                loader.UnloadAllAssets();
            }
        }

        private void OnGUI()
        {
            if (!enableDeepDebug)
                return;

            //show game view buttons
            if (GUI.Button(new Rect(5, 5, 150, 30), "Load All"))
            {
                LoadAllAssets();
            }

            if (GUI.Button(new Rect(5, 50, 150, 30), "Unload All"))
            {
                UnloadAllAssets();
            }
        }

        private void OnDrawGizmos()
        {
            //Show trigger volume bounds
            if (_triggerCollider != null)
            {
                Gizmos.color = AssetsAreLoaded ? new Color(0, 255, 0, 0.2f) : new Color(255, 0, 0, 0.2f);
                Gizmos.DrawCube(_triggerCollider.bounds.center, _triggerCollider.bounds.size);
            }

            if (loadableAssets.Count <= 0)
                return;

            //if gizmo helper enabled and mesh found
            if (enableHelperGUI && CoordArrows != null)
            {
                //cast coordination arrow helper gizmo for target transform or override pos/rot case
                for (int i = 0; i < loadableAssets.Count; i++)
                {
                     Matrix4x4 _matrix = loadableAssets[i].transformTarget == null
                        ? Matrix4x4.TRS(loadableAssets[i].positionOverride, loadableAssets[i].rotationOverride.normalized,
                            Vector3.one)
                        : Matrix4x4.TRS(loadableAssets[i].transformTarget.position,
                            loadableAssets[i].transformTarget.rotation, loadableAssets[i].transformTarget.localScale);

                    Gizmos.matrix = _matrix;

                    Gizmos.color = loadableAssets[i].transformTarget == null
                        ? new Color(0, 255, 0, 0.7f)
                        : new Color(255, 255, 0, 0.7f);

                    //fallback: gizmo with not holding the mesh material (graphics.drawmesh is actually buggy)
                    Gizmos.DrawMesh(CoordArrows, Vector3.zero, Quaternion.identity);

                    //to be continue: draw mesh with material to highlight x, y and z axis differently
                }
            }
        }
        #endif
    }
}

