# ScreenSwipe

ScrollRect that snaps pages to screen size

## Install

### OpenUPM
[![openupm](https://img.shields.io/npm/v/com.brogan89.screenswipe?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.brogan89.screenswipe/)

- Install with [OpenUPM CLI](https://openupm.com/) `openupm add com.brogan89.screenswipe`

### UPM
- In package manager window `Window > Package Manager` click + icon and choose "Add package from git URL...", then paste in the URL `https://github.com/brogan89/ScreenSwipe.git`

- Alternatively add a dependency to your manifest.json in `./Packages` folder.
```
"com.brogan89.screenswipe": "https://github.com/brogan89/ScreenSwipe.git"
```

## Usage
- Right click in hierarchy choose UI > Screen Swipe > Screen Swipe. 
- Then do the same for Pagination. 
- Drag Pagination gameObject into Pagination field in the Screen Swipe inspector.
- Do the same for the buttons if you want.
- Add more pages if you want.
- Make sure pagination toggle count is the same as the number of pages you have.
- Press play, and check it out.

See the Demo scene for best implementation.

## Notes
If you want a SrollRect as child of this script then use [ScrollRectEx](https://bitbucket.org/UnityUIExtensions/unity-ui-extensions/src/release/Runtime/Scripts/Utilities/ScrollRectEx.cs) from Unity UI Extensions. It will pass the PointerData up the heirarchy in the OnDrag events. For some reason Unity's vanilla ScrollRect doesn't allow this.


# References
This script used bits of code from and inspired by:
- Unity's [ScrollRect.cs](https://bitbucket.org/Unity-Technologies/ui/src/0bd08e22bc17bdf80bf7b997a4b43877ae4ee9ac/UnityEngine.UI/UI/Core/ScrollRect.cs?at=5.2&fileviewer=file-view-default#ScrollRect.cs-12,178) 
- Unity-UI-Extensions [HorizontalScrollSnap](https://bitbucket.org/UnityUIExtensions/unity-ui-extensions/src/8b8c6f5c3adb0a953f04d8b74d4a12c004929458/Scripts/Layout/HorizontalScrollSnap.cs?at=master&fileviewer=file-view-default#HorizontalScrollSnap.cs-12)
