# Async Asset Loader

This is a small asset loader by using Unitys addressables system and trigger methods to load/unload assets dynamically. This can help to organize your scenes and save resources of the running platform. 

## Table of Contents

- [Installation](#Installation)
- [How To](#How To)
- [Building](#Building)
- [Values](#Values)
- [License](#License)
- [Contact](#Contact)

## Installation
Install this package by one of these steps:
- Download this repository and add it as a local package via the asset package window in unity.
- Add this repository as a git package via the asset package window by using the git url.

Dependencies:
- Unity Physics 1.0.0
- Addressables 1.16.1

## How To
Follow these steps:
- Add an asset loader gameobject to your scene by using the menu "GameObject/AssetLoader/Create.." or add the AsyncAssetLoader component to an existing gameobject.
- Adjust the boundaries of the box collider and the collidable layer value to define where and who is triggering the loading/unloading process.
- Mark the prefabs you want to use as Addressable inside your asset folder (Project Window).
- Adjust the list lengt depending how many assets you want to load with this loader object.
- Select the Addressable reference inside the list element.
- Adjust the instantiation position, rotation and parenting.
- You can preview the loading and unloading process by pressing the "Load!" or "Unload!" scene view button or click the "Load All" or "Unload All" button inside the component in inspector. Changing the transform target while the asset is loaded, will not result in a position and rotation change of the loaded asset. Therefore you have to click the "Refresh Asset Data" button inside the component after adjusting the transform target. Enabling helper GUI at the debug section can help to evaluate the transform target or the override position and rotation.
- OnLoadingDone() or OnUnloadingDone() can be overwritten to implement some own code. Notice: OnLoadingDone() and OnUnloadingDone() gets only called once and does currently not check if the loading or unloading process was successfully done by the addressable system.

## Building
If you build your game, keep in mind:
Addressables works only with builded content. It will get an empty addressable reference, if your asset was not built. Be sure to build your content before building.
**See also:** https://docs.unity3d.com/Packages/com.unity.addressables@0.7/manual/AddressableAssetsGettingStarted.html

## Values
Value desciption of the AsyncAssetLoader component. Some informations are also available as hover tooltip inside the inspector.

- **Collidable Layers**: List of Layers. Gameobjects with one of this layers can trigger the loading/unloading process. Be sure to add a character controller or rigidbody component to this GameObject and enable the collision inside Unitys physics matrix. (**see also:** https://docs.unity3d.com/Manual/LayerBasedCollision.html **;and:** https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnTriggerEnter.html)
- **Unload When Trigger Exist**: This enables/disables the unloading by trigger exit.
- **Loadable Assets**: Every added asset to this list will be loaded/unloaded. Minimum requirement is the asset reference of the addressable.
- **Enable Debug**: Enables simple debug messages.
- **Enable Helper HUI**: Shows the coordination helper. It indicates the world position and rotation value of the transform target (yellow), or if this is empty, of the position and rotation override value (green).
- **Enable Deep Debug**: Enables more specific debug messages. Some debugs are only available in unity editor.

## License
- **[MIT license](http://opensource.org/licenses/mit-license.php)**

## Contact
If you have any questions, feel free to ask: eric.kirschstein@haw-hamburg.de