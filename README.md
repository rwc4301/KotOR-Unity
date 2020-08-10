# KotOR-Unity

KotOR-Unity is a conversion layer between BioWare's Odyssey engine, which was used to develop Star Wars Knights of the Old Republic (2), and the Unity engine. The goal is to implement features of the original games inside Unity.

## Status

Currently the following things are implemented:
 - Extract files from BIF, ERF, and RIM archives
 - Parsing of various aurora/odyssey file types into memory
 - Binary models (MDL/MDX) are fully loaded and viewable in-game with limited animation support
 - Materials are loaded from textures in TPC and TGA format with lightmap support
 - GFF files loaded and exportable to JSON format
 - Module layouts can be loaded
 - Audio files can be loaded
 - Full modules can be loaded with correct room placement, ambient music, and characters, doors, and placeables
 - Modules can be traversed in-game using standard player controllers
 
The following things are in the pipeline:
 - Further material support including specular maps and bump maps
 - Player interaction with placeables, doors, characters, and items
 - Better animation support
 - Support for waypoints
 - Loading files from MOD archives and the override folder
 
 Other things, notably scripting and combat, are not currently on the roadmap. If you'd like to see them implemented, you can help by contributing!

## Installing Dependencies

The project uses [NAudio](https://github.com/naudio/NAudio) and [fastJSON](https://github.com/mgholam/fastJSON) to support audio playback and JSON serialization respectively. Either clone and import the source into your Unity project or build the dlls and import those.

## Getting Started

1) First things first, you'll need a copy of the target game, KotOR or TSL. You'll also need to download the Unity game engine.
2) Clone or download the repository.
3) Open the KotOR-Unity directory you just downloaded as a Unity project.
4) Inside Unity, load the sample scene from Assets -> Scenes -> SampleScene
5) The GameManager component is the entry point for loading everything, you'll need to change the Kotor Dir property to match the root of your kotor installation, and the Target Game property to either KotOR or TSL.
6) Enter the name of the Entry Module you wish to load, module names can be found in {KotOR Directory}/Modules.
7) Hit play, you should be able to see the desired module load in the scene and move around with a basic 3rd person controller.

## VR Support

VR is working with the oculus rift, future releases will try to generalise this to any headset. In order to run in VR mode, just drag and drop the OVRPlayerController from Assets -> Resources -> PlayerControllers into the scene and make sure to remove any other player controllers.

## Contributing

Everyone is welcome to contribute, if you have something to add, submit a pull request or otherwise feel free to request a feature.

## Licensing

This project is released under the GNU GPLv3, meaning you are free to copy, distribute, and modify the source however you like, including for commercial use, so long as any derivative work is also released under the terms of the license. See LICENSE.md for further details.