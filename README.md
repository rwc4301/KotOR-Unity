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
 
## Getting Started

1) First things first, you'll need a copy of the target game, KotOR or TSL. You'll also need to download the Unity game engine.
2) Clone or download the repository.
3) Create a new Unity project and point it to the KotOR-Unity directory you just downloaded.
4) Inside Unity, load the sample scene from Assets -> Scenes -> SampleScene
5) The GameManager component is the entry point for loading everything, you'll need to change the Kotor Dir property to match the root of your kotor installation, and the Target Game property to either KotOR or TSL.
6) Enter the name of the Entry Module you wish to load, module names can be found in {KotOR Directory}/Modules.
7) Hit play, you should see the desired module load in the scene.
8) In order to move through the scene you'll need a player controller, you can use one supplied in Assets -> Resources -> PlayerControllers, or one of your own, just so long as it is tagged as 'Player'.

## Contributing

Everyone is welcome to contribute, if you have something to add, submit a pull request or otherwise feel free to request a feature.

