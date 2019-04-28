using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Mask), typeof(Image))]
public class ScreenSwipe : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
	public RectTransform rectTransform { get { return (RectTransform)transform; } }

	public enum SwipeType
    {
        Horizontal,
        Vertical
    }

	[Header("Swipe")]
	[SerializeField]
	private SwipeType swipeType = SwipeType.Horizontal;

	[SerializeField, Tooltip("Time a swipe must happen within (s)")]
	private float swipeTime = 0.5f;
	private float startTime;
	private bool isSwipe;
    
	private Vector2 velocity;

	[SerializeField, Tooltip("Velocity required to change screen")]
	private int swipeVelocityThreshold = 50;

	[Header("Content")]
	[SerializeField, Tooltip("Will contents be masked?")]
	private bool maskContent = true;

	private Mask _mask;
	private Mask Mask
	{
		get
		{
			if (_mask == null)
				_mask = GetComponent<Mask>();
			return _mask;
		}
	}

	private Image _maskImage;
	private Image MaskImage
	{
		get
		{
			if (_maskImage == null)
				_maskImage = GetComponent<Image>(); ;
			return _maskImage;
		}
	}

	[SerializeField, Tooltip("Starting screen. Note: a 0 indexed array")]
	private int startingScreen;

	[SerializeField]
	private int currentScreen;
	public int CurrentScreen { get { return currentScreen; } }

	[SerializeField, Tooltip("Parent object which contain the screens")]
	private RectTransform content;
	public RectTransform Content { get { return content; } set { content = value; } }

	[SerializeField, Tooltip("Distance between screens")]
	private float spacing = 20;
	public float Spacing
	{
		get { return spacing; }
		set
		{
			spacing = value;
			SetScreenPositionsAndContentWidth();
		}
	}

	[SerializeField]
	private List<RectTransform> screens;
	public int ScreenCount { get { return screens.Count; } }

	// screen orientation change events
	[Tooltip("Will poll for changes in screen orientation changes. (Mobile)")]
	public bool pollForScreenOrientationChange;

	[SerializeField, Tooltip("A key for testing orientation change event in the editor")]
	private KeyCode editorRefreshKey = KeyCode.F1;
	private ScreenOrientation screenOrientation;

    [SerializeField, Tooltip("Toggle Group to display pagination. (Optional)")]
	private ToggleGroup pagination;
	private Toggle _toggleMockPrefab;
	private List<Toggle> toggles;

	[Header("Controls (Optional)")]
	[Tooltip("True = Acts like a normal screenRect but with snapping\nFalse = Can only change screens with buttons or from another script")]
	public bool isInteractable = true;

	[SerializeField]
	private Button nextButton;
	public Button NextButton { get { return nextButton; } }

	[SerializeField]
	private Button previousButton;
	public Button PreviousButton { get { return previousButton; } }

	[SerializeField, Tooltip("Previous button disables when current screen is at 0. Next button disables when current screen is at screen count")]
	private bool disableButtonsAtEnds;

	[Header("Tween")]
	[SerializeField, Tooltip("Length of the tween (s)")]
	private float tweenTime = 0.5f;

	[SerializeField]
	private AnimationCurve ease = AnimationCurve.Linear(0, 0, 1, 1);

	// bounds
	private Bounds contentBounds;
	private Bounds viewBounds;

	// start positions
	private Vector2 pointerStartLocalCursor;
	private Vector2 dragStartPos;

	// events
	[Serializable]
	public class ScreenEvent : UnityEvent<int> { }

	[Space]
	public UnityEvent onScreenDragBegin;
	public ScreenEvent onScreenChanged;
	public ScreenEvent onScreenTweenEnd;

	private void Start()
	{
		SetScreenPositionsAndContentWidth();
		Pagination_Init();
		GoToScreen(startingScreen);

		// button listeners
		if (previousButton)
			previousButton.onClick.AddListener(GoToPreviousScreen);

		if (nextButton)
			nextButton.onClick.AddListener(GoToNextScreen);
	}

    /// <summary>
    /// Checks for orientation changes
    /// </summary>
    /// <returns>coroutine</returns>
	private IEnumerator CheckForOrientationChange()
	{
		// set initial orientation
		screenOrientation = Screen.orientation;

		while (enabled)
		{
			if (screenOrientation != Screen.orientation || (Application.isEditor && Input.GetKeyDown(editorRefreshKey)))
			{
				screenOrientation = Screen.orientation;

				Debug.LogFormat("ScreenSwipe Orientation change: {0}", screenOrientation);

				// refresh contents on the change
				RefreshContents();
			}
			yield return null;
		}
	}

	private void OnEnable()
	{
		// refresh contents on screen change
		if (pollForScreenOrientationChange)
			StartCoroutine(CheckForOrientationChange());
	}

	private void OnValidate()
	{
		Mask.showMaskGraphic = false;
		Mask.enabled = maskContent;
		MaskImage.enabled = maskContent;
	}

	private void Reset()
	{
		maskContent = true;
		content = transform.GetChild(0) as RectTransform;
		swipeTime = 0.5f;
		swipeVelocityThreshold = 50;
		spacing = 20;
	}

	#region Pagination
	/// <summary>
	/// Initializes the pagination toggles
	/// </summary>
	private void Pagination_Init()
	{
        if (!pagination) return;

        toggles = pagination.GetComponentsInChildren<Toggle>().ToList();

        // store the first toggle to use for instantiating later
        if (toggles[0] != null && _toggleMockPrefab == null)
            _toggleMockPrefab = toggles[0];

        // loop through and assign toggle properties
        for (int i = 0; i < toggles.Count; i++)
        {
            toggles[i].isOn = false;
            toggles[i].group = pagination;
            toggles[i].onValueChanged.AddListener(PaginationToggleCallback);
        }
    }

    /// <summary>
    /// Turns a toggle on
    /// </summary>
	private void SelectToggle()
    {
        if (!pagination) return;

        try
        {
            toggles[currentScreen].isOn = true;
        }
        catch (ArgumentOutOfRangeException e)
        {
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// Adds a pagination toggle
    /// </summary>
	private void AddPaginationToggle()
	{
        if (!pagination) return;

        Toggle newToggle = Instantiate(_toggleMockPrefab, pagination.transform);
        newToggle.group = pagination;
        newToggle.isOn = false;

        // for some reason shit gets turned off so this is a just in case thing
        newToggle.gameObject.SetActive(true);
        newToggle.enabled = true;
        newToggle.GetComponentInChildren<Image>().enabled = true;
        
        toggles.Add(newToggle);
    }

    /// <summary>
    /// Removes a pagination toggle
    /// </summary>
    /// <param name="index">Index to remove toggle</param>
	private void RemovePaginationToggle(int index)
	{
        if (!pagination) return;

        // remove from list
        toggles.RemoveAt(index);

        // destroy gameObject
        Destroy(toggles[index].gameObject);
    }

    /// <summary>
    /// Removes all pagination toggles
    /// </summary>
	private void RemoveAllPaginationToggles()
	{
        if (!pagination) return;

        // clear toggle list
        toggles.Clear();

        for (int i = 0; i < pagination.transform.childCount; i++)
        {
            // destroy gameObject
            Destroy(pagination.transform.GetChild(i).gameObject);
        }
    }

	/// <summary>
	/// Callback function from pagination toggles to change screen upon clicking toggle
	/// </summary>
	/// <param name="isOn">Is the toggle on</param>
	private void PaginationToggleCallback(bool isOn)
    {
        if (!isOn) return;
        if (isSwipe) return;

        for (int i = 0; i < toggles.Count; i++)
        {
            if (toggles[i].isOn)
            {
                GoToScreen(i);
                break;
            }
        }
    }
	#endregion

	#region Screen Mangement / Public API
	/// <summary>
	/// Sets the screens positions and calculates the contents size
	/// </summary>
	private void SetScreenPositionsAndContentWidth()
	{
		Vector2 screenSize = rectTransform.rect.size;

		screens = new List<RectTransform>();

        if (!content) return;

        for (int i = 0; i < content.childCount; i++)
        {
            // assign to list
            screens.Add(content.GetChild(i).transform as RectTransform);

            // pivot and anchors
            screens[i].pivot = screens[i].anchorMin = screens[i].anchorMax =
                swipeType == SwipeType.Horizontal
                    ? new Vector2(0, 0.5f)
                    : new Vector2(0.5f, 0);

            // size
            screens[i].sizeDelta = screenSize;

            // scale
            screens[i].localScale = Vector3.one;

            // position
            screens[i].anchoredPosition = swipeType == SwipeType.Horizontal
                ? new Vector2((screenSize.x * i) + (spacing * i), 0)
                : new Vector2(0, (screenSize.y * i) + (spacing * i));
        }

        // set content anchors and pivot
        content.pivot = content.anchorMin = content.anchorMax =
            swipeType == SwipeType.Horizontal
                ? new Vector2(0, 0.5f)
                : new Vector2(0.5f, 0);

        // set content size
        content.sizeDelta = swipeType == SwipeType.Horizontal
            ? new Vector2((screenSize.x + spacing) * screens.Count - spacing, screenSize.y)
            : new Vector2(screenSize.x, (screenSize.y + spacing) * screens.Count - spacing);
    }

	/// <summary>
	/// Calls private coroutine RefreshContentsCoroutine()
	/// <para>Waits until end of frame and then resets screens and pagination</para>
	/// </summary>
	public void RefreshContents()
	{
		StartCoroutine(RefreshContentsCoroutine());
	}

	/// <summary>
	/// Waits until end of frame and then resets screens and pagination
	/// </summary>
	/// <returns>Coroutine</returns>
	private IEnumerator RefreshContentsCoroutine()
	{
		yield return new WaitForEndOfFrame();
		SetScreenPositionsAndContentWidth();
		Pagination_Init();
	}

	/// <summary>
	/// Adds a screen to the list then recalculates the contents width
	/// </summary>
	/// <param name="newScreen">Screen to add</param>
	/// <param name="screenNumber">Default as last screen. index 0 - </param>
	public void AddScreen(RectTransform newScreen, int screenNumber = -1)
	{
		newScreen.transform.SetParent(content);

		if (IsWithinScreenCount(screenNumber))
			newScreen.SetSiblingIndex(screenNumber);
		else
			newScreen.SetAsLastSibling();

		// add to list
		screens.Add(newScreen);

		// pagination
		AddPaginationToggle();

		//refresh
		StartCoroutine(RefreshContentsCoroutine());
	}

    /// <summary>
    /// Removes screen from list and recalculates contents width 
    /// </summary>
    /// <param name="screenNumber">Screen number to remove</param>
    public void RemoveScreen(int screenNumber)
	{
		if (IsWithinScreenCount(screenNumber))
		{
			// remove from list
			screens.RemoveAt(screenNumber);

			// destroy gameObject
			Destroy(content.GetChild(screenNumber).gameObject);

			// pagination
			RemovePaginationToggle(screenNumber);

			// refresh
			StartCoroutine(RefreshContentsCoroutine());
		}
		else
			Debug.LogWarningFormat("ScreenNumber: '{0}' doesn't exist", screenNumber);
	}

    /// <summary>
    /// Removes all screens
    /// </summary>
	public void RemoveAllScreens()
	{
		Debug.Log("Removing Screens : " + content.childCount);

		// clear list
		screens.Clear();

		for (int i = 0; i < content.childCount; i++)
		{
			// destroy screen game object
			Destroy(content.GetChild(i).gameObject);
		}

		RemoveAllPaginationToggles();
	}

	/// <summary>
	/// Tweens to a specific screen
	/// </summary>
	/// <param name="screenNumber">Screen number to tween to</param>
	public void GoToScreen(int screenNumber)
	{
		if (IsWithinScreenCount(screenNumber))
		{
			// set current screen
			currentScreen = screenNumber;

			// pagination
			SelectToggle();

			// tween screen
			StartCoroutine(TweenPage(-screens[currentScreen].anchoredPosition));

			// disable buttons if ends are reached
			if (disableButtonsAtEnds && previousButton != null && nextButton != null)
			{
				if (currentScreen == 0)
				{
					previousButton.gameObject.SetActive(false);
					nextButton.gameObject.SetActive(true);
				}
				else if (currentScreen == ScreenCount - 1)
				{
					nextButton.gameObject.SetActive(false);
					previousButton.gameObject.SetActive(true);
				}
				else
				{
					previousButton.gameObject.SetActive(true);
					nextButton.gameObject.SetActive(true);
				}
			}
		}
		else
			Debug.LogErrorFormat("Invalid screen number '{0}'. Must be between 0 and {1}", screenNumber, screens.Count - 1);
	}

    /// <summary>
    /// Goes to the next screen
    /// </summary>
	public void GoToNextScreen()
	{
		if (IsWithinScreenCount(CurrentScreen + 1))
			GoToScreen(CurrentScreen + 1);
	}

    /// <summary>
    /// Goes to the previous screen
    /// </summary>
	public void GoToPreviousScreen()
	{
		if (IsWithinScreenCount(CurrentScreen - 1))
			GoToScreen(CurrentScreen - 1);
	}

    /// <summary>
    /// Is the index within the screen count
    /// </summary>
    /// <param name="index">Index to check if within the screen count</param>
    /// <returns>True if within the screen count</returns>
	private bool IsWithinScreenCount(int index)
	{
		return index >= 0 && index < screens.Count;
	}
	#endregion

	#region Swipe and Drag Controlls
	public void OnBeginDrag(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Left || !isInteractable)
			return;

		if (onScreenDragBegin != null)
			onScreenDragBegin.Invoke();

		// cancel tweening
		StopCoroutine("TweenPage"); //TODO: should have coroutine cached, this is expensive

		// get start data
		dragStartPos = eventData.position;
		startTime = Time.time;

		pointerStartLocalCursor = Vector2.zero;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(content, eventData.position, eventData.pressEventCamera, out pointerStartLocalCursor);
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Left || !isInteractable)
			return;
        
		DragContent(eventData);

		// validate swipe boolean
		isSwipe = SwipeValidator(eventData.position);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Left || !isInteractable)
			return;

		// validate screen change and sets current screen 
		ScreenChangeValidate();

		// got to screen
		GoToScreen(currentScreen);

		isSwipe = false;
	}

	/// <summary>
	/// Validates whether or not a swipe was make.
	/// Reasons for failure is swipe timer expired, or threshold is not met
	/// </summary>
	/// <param name="currentPosition">Cursor position</param>
	/// <returns>True if valid swipe</returns>
	private bool SwipeValidator(Vector2 currentPosition)
	{
		// get velocity
		velocity = currentPosition - dragStartPos;

		// is within time
		bool isWithinTime = Time.time - startTime < swipeTime;

		// set to true if it is the swipe type we wanted
		bool isSwipeTypeAndEnoughVelocity;

		// get absolute values of both velocity axis
		var velX = Mathf.Abs(velocity.x);
		var velY = Mathf.Abs(velocity.y);

		if (swipeType == SwipeType.Horizontal)
			isSwipeTypeAndEnoughVelocity = velX > velY && velX > swipeVelocityThreshold;
		else
			isSwipeTypeAndEnoughVelocity = velY > velX && velY > swipeVelocityThreshold;

		// return true if both are true
		return isWithinTime && isSwipeTypeAndEnoughVelocity;
	}

	/// <summary>
	/// Validates whether or not a screen can be changed.
	/// Reasons for failure is we're at the end of the screens list
	/// </summary>
	private void ScreenChangeValidate()
	{
        if (!isSwipe) return;

        int newPageNo = -1;

        if (swipeType == SwipeType.Horizontal)
        {
            // get direction of swipe
            var leftSwipe = velocity.x < 0;

            // assign new page number
            newPageNo = leftSwipe ? currentScreen + 1 : currentScreen - 1;
        }
        else
        {
            // get direction of swipe
            var upSwipe = velocity.y < 0;

            // assign new page number
            newPageNo = upSwipe ? currentScreen + 1 : currentScreen - 1;
        }

        // if valid pageNo then update current page and invoke event
        if (IsWithinScreenCount(newPageNo))
        {
            // change current page
            currentScreen = newPageNo;

            // invoke changed event
            if (onScreenChanged != null)
                onScreenChanged.Invoke(currentScreen);
        }
    }

	/// <summary>
	/// Tweens the contents position
	/// </summary>
	/// <param name="toPos">Vector to tween to</param>
	private IEnumerator TweenPage(Vector2 toPos)
	{
		Vector2 from = content.anchoredPosition;
		Vector2 pos = new Vector2();
		float t = 0;

		while (pos != toPos || t < 1)
		{
			pos = Vector2.Lerp(from, toPos, ease.Evaluate(t));
			t += Time.deltaTime / tweenTime;
			SetContentAnchoredPosition(pos);
			yield return null;
		}

		if (onScreenTweenEnd != null)
			onScreenTweenEnd.Invoke(currentScreen);
	}
	#endregion

	#region Functions From Unity ScrollRect

	/* Note:
     * Everything in this region I sourced from Unity's ScrollRect script and rejigged to work in this script
     * Sources from: https://bitbucket.org/Unity-Technologies/ui 
     * Folder path: UI/UnityEngine.UI/UI/Core/ScrollRect.cs
     */

	/// <summary>
	/// Wrapper function <see cref="ScrollRect.OnDrag(PointerEventData)"/>
	/// </summary>
	/// <param name="eventData"></param>
	private void DragContent(PointerEventData eventData)
	{
		Vector2 localCursor;
		if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(content, eventData.position, eventData.pressEventCamera, out localCursor))
			return;

		UpdateContentBounds();

		var pointerDelta = localCursor - pointerStartLocalCursor;
		Vector2 position = content.anchoredPosition + pointerDelta;

		// Offset to get content into place in the view.
		Vector2 offset = CalculateOffset(pointerDelta);
		position += offset;

		if (offset.x != 0)
			position.x = position.x - RubberDelta(offset.x, viewBounds.size.x);
		if (offset.y != 0)
			position.y = position.y - RubberDelta(offset.y, viewBounds.size.y);


		SetContentAnchoredPosition(position);
	}

    /// <summary>
    /// Sets the contents anchored position
    /// </summary>
    /// <param name="position">Position to set the content to</param>
	private void SetContentAnchoredPosition(Vector2 position)
	{
		if (swipeType == SwipeType.Vertical)
			position.x = content.anchoredPosition.x;

		if (swipeType == SwipeType.Horizontal)
			position.y = content.anchoredPosition.y;

		if (position != content.anchoredPosition)
		{
			content.anchoredPosition = position;
			UpdateContentBounds();
		}
	}

    /// <summary>
    /// Updates the bounds of the scroll view content
    /// </summary>
	private void UpdateContentBounds()
	{
		viewBounds = new Bounds(rectTransform.rect.center, rectTransform.rect.size);
		contentBounds = GetContentBounds();

		// Make sure content bounds are at least as large as view by adding padding if not.
		// One might think at first that if the content is smaller than the view, scrolling should be allowed.
		// However, that's not how scroll views normally work.
		// Scrolling is *only* possible when content is *larger* than view.
		// We use the pivot of the content rect to decide in which directions the content bounds should be expanded.
		// E.g. if pivot is at top, bounds are expanded downwards.
		// This also works nicely when ContentSizeFitter is used on the content.
		Vector3 contentSize = contentBounds.size;
		Vector3 contentPos = contentBounds.center;
		Vector3 excess = viewBounds.size - contentSize;
		if (excess.x > 0)
		{
			contentPos.x -= excess.x * (content.pivot.x - 0.5f);
			contentSize.x = viewBounds.size.x;
		}

		contentBounds.size = contentSize;
		contentBounds.center = contentPos;
	}

    /// <summary>
    /// Gets the bounds of the scroll view content
    /// </summary>
    /// <returns>Content Bounds</returns>
	private Bounds GetContentBounds()
	{
		var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

		var toLocal = rectTransform.worldToLocalMatrix;

		Vector3[] m_Corners = new Vector3[4];
		content.GetWorldCorners(m_Corners);
		for (int j = 0; j < 4; j++)
		{
			Vector3 v = toLocal.MultiplyPoint3x4(m_Corners[j]);
			vMin = Vector3.Min(v, vMin);
			vMax = Vector3.Max(v, vMax);
		}

		var bounds = new Bounds(vMin, Vector3.zero);
		bounds.Encapsulate(vMax);
		return bounds;
	}

    /// <summary>
    /// Offset to get content into place in the view.
    /// </summary>
    /// <param name="pointerDelta">Pointer delta</param>
    /// <returns>Offset to get content into place in the view.</returns>
	private Vector2 CalculateOffset(Vector2 pointerDelta)
	{
		Vector2 offset = Vector2.zero;

		Vector2 min = contentBounds.min;
		Vector2 max = contentBounds.max;

		min.x += pointerDelta.x;
		max.x += pointerDelta.x;
		if (min.x > viewBounds.min.x)
			offset.x = viewBounds.min.x - min.x;
		else if (max.x < viewBounds.max.x)
			offset.x = viewBounds.max.x - max.x;
        
		return offset;
	}

    /// <summary>
    /// 
    /// </summary>
    /// <param name="overStretching"></param>
    /// <param name="viewSize"></param>
    /// <returns></returns>
	private float RubberDelta(float overStretching, float viewSize)
	{
		return (1 - (1 / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1))) * viewSize * Mathf.Sign(overStretching);
	}
	#endregion

	#region Editing
	[Header("Editing")]
	[SerializeField, Tooltip("Screen you want to be showing in Game view. Note: 0 indexed array")]
	private int editingScreen;

    /// <summary>
    /// Changes the screen being edited
    /// <para><see cref="editingScreen"/> needs to be set before this is called</para>
    /// </summary>
	public void EditingScreen()
	{
		SetScreenPositionsAndContentWidth();

		if (editingScreen >= 0 && editingScreen < screens.Count)
		{
			content.anchoredPosition = -screens[editingScreen].anchoredPosition;
			Debug.LogFormat("Editing: GoToScreen {0}", editingScreen);
		}
		else
			Debug.LogErrorFormat("Invalid editingScreen value. '{0}'", editingScreen);
	}
	#endregion
}
