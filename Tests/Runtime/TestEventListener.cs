using UnityEngine;

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
		screenSwipe.onScreenChanged.AddListener(OnScreenChanged);
	}

	private static void OnScreenDragBegin()
	{
		Debug.Log("OnScreenDragBegin");
	}

	private static void OnScreenChanged(int screenNo)
	{
		Debug.Log($"OnScreenChanged: {screenNo}");
	}
}