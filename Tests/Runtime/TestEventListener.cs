using UnityEngine;
using UnityEngine.EventSystems;

/*
 * Logs in console when events are invoked by ScreenSwipe
 * Functions are called from serialised event fields in the inspector 
 */

public class TestEventListener : MonoBehaviour
{
	public ScreenSwipe screenSwipe;

	private void Start()
	{
		if (!screenSwipe)
			return;

		screenSwipe.onScreenDragBegin.AddListener(OnScreenDragBegin);
		screenSwipe.onScreenDrag.AddListener(OnScreenDrag);
		screenSwipe.onScreenDragEnd.AddListener(OnScreenDragEnd);
		screenSwipe.onScreenChanged.AddListener(OnScreenChanged);
	}

	private static void OnScreenDragBegin(PointerEventData eventData)
	{
		Debug.Log("OnScreenDragBegin");
	}
	
	private static void OnScreenDrag(PointerEventData eventData)
	{
		Debug.Log("OnScreenDrag");
	}
	
	private static void OnScreenDragEnd(PointerEventData eventData)
	{
		Debug.Log("OnScreenDragEnd");
	}

	private static void OnScreenChanged(int screenNo)
	{
		Debug.Log($"OnScreenChanged: {screenNo}");
	}
}