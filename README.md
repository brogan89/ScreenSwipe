# ScreenSwipe

- Right click in hierarchy choose UI > Screen Swipe > Screen Swipe. 
- Then do the same for Pagination. 
- Drag Pagination gameObject into Pagination field in the Screen Swipe inspector.
- Do the same for the buttons if you want.
- Add more pages if you want.
- Make sure pagination toggle count is the same as the number of pages you have.
- Press play, and check it out.

See the Demo scene for best implementation.


# References
This script used bits of code from and inspired by:
- Unity's [ScrollRect.cs](https://bitbucket.org/Unity-Technologies/ui/src/0bd08e22bc17bdf80bf7b997a4b43877ae4ee9ac/UnityEngine.UI/UI/Core/ScrollRect.cs?at=5.2&fileviewer=file-view-default#ScrollRect.cs-12,178) 
- Unity-UI-Extensions [HorizontalScrollSnap](https://bitbucket.org/UnityUIExtensions/unity-ui-extensions/src/8b8c6f5c3adb0a953f04d8b74d4a12c004929458/Scripts/Layout/HorizontalScrollSnap.cs?at=master&fileviewer=file-view-default#HorizontalScrollSnap.cs-12)
- PIXELPLACEMENT's [iTween](https://assetstore.unity.com/packages/tools/animation/itween-84)

Note: The itween script I have in this repo is a slight tweek of the script that is in the asset store.
