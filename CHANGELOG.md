# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.6] - 2020-09-13

### Fixed
- unload assets before exiting play mode to avoid warning messages of lost addressable.instances
- fixed Quaternion To Matrix conversion fails when overrideRotation was 0,0,0

## [0.1.5] - 2020-09-12

### Added
- custom mesh to show the position and rotation of the Asset Instantiation (transform target (yellow) or override possition and rotation (green)), when helper gui enabled

### Changed
- some script adjustments

## [0.1.4] - 2020-09-11

### Added
- refresh method + gui button (in inspector only) to reset the position, rotation and parent transform of a loaded asset to the edited one.
- debug gizmos can now be turned on in inspector, if you wanna visualize the target transform (yellow gizmo sphere) or the override position (green gizmo sphere). To show the override position, the target must be null. Helper Gizmos ignores the rotation at this moment.

### Changed
- gui only displays when the loader-gameobject is selected

## [0.1.3] - 2020-09-10

### Fixed
- double-call unload by scene view gui
- CHANGELOG versioning of previous push

## [0.1.2] - 2020-09-10

### Added
- MenuItem to create a new Asset Loader GameObject ("GameObject/Agent/Create Asset Loader..")

### Removed
- rejecting load/unload calls by GUI buttons when in edit mode because of addressable system bugs.

## [0.1.2]
- basic package