# Extra UI Screens Mod
## _Use additional monitors_

This mod is designed to allow use any screen available in current PC to show other mods information. It also allows creating a new overlay over the game screen, both integrated with game and as a new separated layer (as it was an extra screen).

Modders can take advantage from this mod to create complexes UI, like report tables, map drawings or advanced mod setups. 

This mod is just a platform for other mods, it does almost nothing in the game alone.

## For mod creators

Check the base react project at my [GitHub](https://github.com/klyte45/EUIS-baseproj-fe) to get instructions about how to create a new frontend application into EUIS.

To have a reference about how integrate the react projects into a mod project, check this sample available [at Live Translation Editor mod GitHub](https://github.com/klyte45/CS2-LiveTranslationEditor/releases/tag/EUIS_BaseProject).
You can check the commits after this to get details on how to implementate endpoints for your applications.

Also there will have a way to add simple buttons to toggle tools in the vanilla UI in a dock group - like Unified UI (UUI) used to do in CS1. The mod window over vanilla UI explained above also will generate a button there.

Detailed tutorials soon!

## Feature roadmap
- ✅ Allow using extra monitors available as UI container for mods apps
- ✅ Allow selecting which app to be available each screen
- ✅ Allow creating a new layer over main screen (toggle using Ctrl+Tab when enabled)
- ✅ Allow each mod to create more than one app for different purpoises
- 🔜 Allow have more than one app open in some screen
- ✅ Disponibilize basic project for modders to create apps in extra screens/main UI overlay

### Notice!
Cities Skylines 2 uses Coherent UI to emulate a simplified version of Chromium to render the game UI, so not all common web features are available to use in game UI. For more information check the [Coherent UI documentation](https://docs.coherent-labs.com/unity-gameface/)

## Experimental mod warning!
Since it's a very complex mod, it may cause issues in the game due their early stage of development. However, by the nature of this mod it's very unlikely it to break after game updates - but watch out the mods that may be using this mod as UI platform because they may be sensible to game updates.

## Support

The most up to date information about installation and known issues and bugs is at the mod topic.