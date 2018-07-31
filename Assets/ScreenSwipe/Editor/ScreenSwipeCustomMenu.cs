using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ScreenSwipeCustomMenu : MonoBehaviour
{
	private static void AddToUndo(GameObject go, MenuCommand menuCommand)
	{
		// Ensure it gets reparented if this was a context click (otherwise does nothing)
		GameObjectUtility.SetParentAndAlign(go, (GameObject)menuCommand.context);

		// Register the creation in the undo system
		Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
		Selection.activeObject = go;
	}

	private static GameObject CreateUIObject(string name, GameObject parent)
	{
		GameObject go = new GameObject(name);
		go.AddComponent<RectTransform>();
		GameObjectUtility.SetParentAndAlign(go, parent);
		return go;
	}

	[MenuItem("GameObject/UI/Screen Swipe/Screen Swipe", false, 10)]
	private static void AddScreenSwipe(MenuCommand menuCommand)
	{
		Canvas canvas = FindObjectOfType<Canvas>();

		GameObject go = CreateUIObject("Screen Swipe", canvas.gameObject);

		var rect = go.transform as RectTransform;
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2();
		rect.anchorMin = new Vector2();


		// content
		GameObject content = CreateUIObject("Content", go);

		// first page
		GameObject firstPage = CreateUIObject("Page_01", content);
		firstPage.transform.SetParent(content.transform);
		firstPage.AddComponent<Image>();

		// first page text
		GameObject fistPageText = CreateUIObject("Text", firstPage);
		fistPageText.transform.SetParent(firstPage.transform);
		var text = fistPageText.AddComponent<Text>();
		text.text = "Page_01";

		// screen swipe
		var screenSwipe = go.AddComponent<ScreenSwipe>();
		screenSwipe.Content = content.transform as RectTransform;

		// add to undo
		AddToUndo(go, menuCommand);
	}

	[MenuItem("GameObject/UI/Screen Swipe/Pagination", false, 10)]
	private static void AddPagination(MenuCommand menuCommand)
	{
		Canvas canvas = FindObjectOfType<Canvas>();

		GameObject go = CreateUIObject("Pagination", canvas.gameObject);
		go.AddComponent<ToggleGroup>();

		// layout
		var layout = go.AddComponent<HorizontalLayoutGroup>();
		layout.spacing = 10;
		layout.childAlignment = TextAnchor.MiddleCenter;

		// content size fitter
		var sizeFitter = go.AddComponent<ContentSizeFitter>();
		sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

		for (int i = 0; i < 3; i++)
		{
			// toggle
			GameObject firstToggle = CreateUIObject("Toggle_" + i, go);
			var toggle = firstToggle.AddComponent<Toggle>();

			// toggle background
			GameObject toggleBackGround = CreateUIObject("Background", firstToggle);
			var bgImage = toggleBackGround.AddComponent<Image>();
			bgImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
			bgImage.type = Image.Type.Sliced;

			// check mark
			GameObject toggleCheckmark = CreateUIObject("Checkmark", toggleBackGround);
			var checkmark = toggleCheckmark.AddComponent<Image>();
			checkmark.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
			checkmark.type = Image.Type.Simple;

			// set toggle component
			toggle.targetGraphic = bgImage;
			toggle.graphic = checkmark;
		}

		AddToUndo(go, menuCommand);
	}
}
