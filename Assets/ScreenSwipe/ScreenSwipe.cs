using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[AddComponentMenu("UI/ScreenSwipe")]
[RequireComponent(typeof(RectTransform), typeof(Mask), typeof(Image))]
public class ScreenSwipe : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    private RectTransform _rectTransform;
    public RectTransform rectTransform
    {
        get
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
            return _rectTransform;
        }
    }

    public enum SwipeType { Horizonal, Vertical }

    [Header("Swipe")]
    [SerializeField]
    private SwipeType swipeType = SwipeType.Horizonal;

    [SerializeField, Tooltip("Time a swipe must happen within (s)")]
    private float swipeTime = 0.5f;
    private float startTime;
    private bool isSwipe;

    //[SerializeField]
    private Vector2 velocity;

    [SerializeField, Tooltip("Velocity required to change screen")]
    private int swipeVelocityThreshold = 50;

    [SerializeField, Tooltip("Set to true if you want to be able to skip screens in one swipe. (Not fully tested)")]
    private bool skipScreen;

    [SerializeField, Tooltip("Velocity require to skip a screen is skipScreen = true")]
    private int skipScreenVelocityThreshold = 250;

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


    // screen orienation change events
    [Tooltip("Will poll for changes in screen orientation changes. (Mobile)")]
    public bool pollForScreenOrientationChange;
    [SerializeField, Tooltip("A key for testing orientation change event in the editor")]
    private KeyCode editorRefreshKey = KeyCode.F1;
    private ScreenOrientation screenOrientation;


    [SerializeField, Tooltip("Toggle Group to display pagination. (Optional)")]
    private ToggleGroup pagination;
    private Toggle _toggleMockPrfab;
    private List<Toggle> toggles;

    [Header("Controlls (Optional)")]
    [SerializeField]
    private Button nextButton;
    public Button NextButton { get { return nextButton; } }
    [SerializeField]
    private Button previousButton;
    public Button PreviousButton { get { return previousButton; } }

    [Tooltip("Only change screens with buttons")]
    public bool buttonsOnly;

    [SerializeField, Tooltip("Previous button disables when current screen is at 0. Next button disables when current screen is at screen count")]
    private bool disableButtonsAtEnds;

    [Header("Tween")]
    [SerializeField, Tooltip("Length of the tween (s)")]
    private float tweenTime = 0.5f;
    [SerializeField]
    private iTween.EaseType easeType = iTween.EaseType.easeOutExpo;

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

        // can only use buttons only if there buttons to press
        if (nextButton == null && previousButton == null)
            buttonsOnly = false;
    }

    private IEnumerator CheckForOrientationChange()
    {
        // set initial orientation
        screenOrientation = Screen.orientation;

        while (enabled)
        {
            if (screenOrientation != Screen.orientation || (Application.isEditor && Input.GetKeyDown(editorRefreshKey)))
            {
                screenOrientation = Screen.orientation;

                Debug.LogFormat("SwcreenSwipe Orientation change: {0}", screenOrientation);

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
        skipScreenVelocityThreshold = 250;
        easeType = iTween.EaseType.easeOutExpo;
        spacing = 20;
    }

    #region Pagination
    /// <summary>
    /// Initializes the pagination toggles
    /// </summary>
    private void Pagination_Init()
    {
        if (pagination)
        {
            toggles = pagination.GetComponentsInChildren<Toggle>().ToList();

            // store the first toggle to use for instantiating later
            if (toggles[0] != null && _toggleMockPrfab == null)
                _toggleMockPrfab = toggles[0];

            // loop through and assign toggle properties
            for (int i = 0; i < toggles.Count; i++)
            {
                toggles[i].isOn = false;
                toggles[i].group = pagination;
                toggles[i].onValueChanged.AddListener(PaginationToggleCallback);
            }
        }
    }

    private void SelectToggle(int index)
    {
        if (pagination)
        {
            try
            {
                toggles[currentScreen].isOn = true;
            }
            catch (ArgumentOutOfRangeException e)
            {
                Debug.LogError(e);
                Debug.LogError(index);
            }
        }
    }

    private void AddPaginationToggle()
    {
        if (pagination)
        {
            var newToggle = Instantiate(_toggleMockPrfab, pagination.transform);
            newToggle.group = pagination;
            newToggle.isOn = false;

            // for some reason shit gets turned off so this is a just in case thing
            newToggle.gameObject.SetActive(true);
            newToggle.enabled = true;
            newToggle.GetComponentInChildren<Image>().enabled = true;


            toggles.Add(newToggle);
        }
    }

    private void RemovePaginationToggle(int index)
    {
        // pagination
        if (pagination)
        {
            // remove from list
            toggles.RemoveAt(index);

            // destroy gameobject
            Destroy(toggles[index].gameObject);
        }
    }

    private void RemoveAllPaginationToggles()
    {
        if (pagination)
        {
            // clear toggle list
            toggles.Clear();

            for (int i = 0; i < pagination.transform.childCount; i++)
            {
                // destroy gameobject
                Destroy(pagination.transform.GetChild(i).gameObject);
            }
        }
    }

    /// <summary>
    /// Callback function from pagination toggles to change screen opon clicking toggle
    /// </summary>
    /// <param name="ison"></param>
    private void PaginationToggleCallback(bool ison)
    {
        if (ison)
        {
            for (int i = 0; i < toggles.Count; i++)
            {
                if (toggles[i].isOn)
                {
                    GoToScreen(i);
                    break;
                }
            }
        }
    }
    #endregion

    #region Screen Mangement
    /// <summary>
    /// Sets the screens positions and calculates the contents size
    /// </summary>
    private void SetScreenPositionsAndContentWidth()
    {
        Vector2 screenSize = rectTransform.rect.size;

        screens = new List<RectTransform>();

        if (content)
        {
            for (int i = 0; i < content.childCount; i++)
            {
                // assign to list
                screens.Add(content.GetChild(i).transform as RectTransform);

                // pivot and anchors
                screens[i].pivot = screens[i].anchorMin = screens[i].anchorMax =
                                    swipeType == SwipeType.Horizonal
                                    ? new Vector2(0, 0.5f)
                                    : new Vector2(0.5f, 0);

                // size
                screens[i].sizeDelta = screenSize;

                // scale
                screens[i].localScale = Vector3.one;

                // position
                screens[i].anchoredPosition = swipeType == SwipeType.Horizonal
                            ? new Vector2((screenSize.x * i) + (spacing * i), 0)
                            : new Vector2(0, (screenSize.y * i) + (spacing * i));
            }

            // set content anchords and pivot
            content.pivot = content.anchorMin = content.anchorMax =
                            swipeType == SwipeType.Horizonal
                            ? new Vector2(0, 0.5f)
                            : new Vector2(0.5f, 0);

            // set content size
            content.sizeDelta = swipeType == SwipeType.Horizonal
                            ? new Vector2((screenSize.x + spacing) * screens.Count - spacing, screenSize.y)
                            : new Vector2(screenSize.x, (screenSize.y + spacing) * screens.Count - spacing);
        }
    }

    /// <summary>
    /// Calls private corouitine RefreshContentsCoroutine()
    /// <para>Waits until end of frame and then resets screens and pagination</para>
    /// </summary>
    public void RefreshContents()
    {
        StartCoroutine(RefreshContentsCoroutine());
    }

    /// <summary>
    /// Waits until end of frame and then resets screens and pagination
    /// </summary>
    /// <returns></returns>
    private IEnumerator RefreshContentsCoroutine()
    {
        yield return new WaitForEndOfFrame();
        SetScreenPositionsAndContentWidth();
        Pagination_Init();
    }

    /// <summary>
    /// Adds a screen to the list then recalculates the contents width
    /// </summary>
    /// <param name="newScreen"></param>
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
    /// <param name="screenNumber"></param>
    public void RemoveScreen(int screenNumber, Action callback = null)
    {
        if (IsWithinScreenCount(screenNumber))
        {
            // remove from list
            screens.RemoveAt(screenNumber);

            // destroy gameobject
            Destroy(content.GetChild(screenNumber).gameObject);

            // pagination
            RemovePaginationToggle(screenNumber);

            // refresh
            StartCoroutine(RefreshContentsCoroutine());
        }
        else
            Debug.LogWarningFormat("ScreenNumber: '{0}' doesnt exist", screenNumber);
    }

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
    /// <param name="screenNumber"></param>
    public void GoToScreen(int screenNumber)
    {
        if (IsWithinScreenCount(screenNumber))
        {
            // set current screen
            currentScreen = screenNumber;

            // pagination
            SelectToggle(screenNumber);

            // tween screen
            TweenPage(-screens[currentScreen].anchoredPosition);

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

    public void GoToNextScreen()
    {
        if (IsWithinScreenCount(CurrentScreen + 1))
            GoToScreen(CurrentScreen + 1);
    }

    public void GoToPreviousScreen()
    {
        if (IsWithinScreenCount(CurrentScreen - 1))
            GoToScreen(CurrentScreen - 1);
    }

    private bool IsWithinScreenCount(int index)
    {
        return index >= 0 && index < screens.Count;
    }
    #endregion

    #region Swipe and Drag Controlls
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || buttonsOnly)
            return;

        if (onScreenDragBegin != null)
            onScreenDragBegin.Invoke();

        // cancel tweening
        iTween.Stop(gameObject);

        // get start data
        dragStartPos = eventData.position;
        startTime = Time.time;

        pointerStartLocalCursor = Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(content, eventData.position, eventData.pressEventCamera, out pointerStartLocalCursor);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || buttonsOnly)
            return;

        /// Wrapper fucntion <see cref="ScrollRect.OnDrag(PointerEventData)"/>
        DragContent(eventData);

        // validate swipe boolean
        isSwipe = SwipeValidator(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || buttonsOnly)
            return;

        // validate screen change and sets current screen 
        ScreenChangeValidate();

        // got to screen
        GoToScreen(currentScreen);
    }

    /// <summary>
    /// Validates whether or not a swipe was make.
    /// Reasons for failure is swipe timer expired, or threshold is not met
    /// </summary>
    /// <param name="currentPosition"></param>
    /// <returns></returns>
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

        if (swipeType == SwipeType.Horizonal)
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
        if (isSwipe)
        {
            // get absolute values of both velocity axis
            var velX = Mathf.Abs(velocity.x);
            var velY = Mathf.Abs(velocity.y);

            int newPageNo = -1;

            if (swipeType == SwipeType.Horizonal)
            {
                // get direction of swipe
                var leftSwipe = velocity.x < 0;

                // check if skip is possible
                var skip = CanSkipScreen(velX, leftSwipe);

                // get size of screen jump
                var screenJump = skip ? 2 : 1;

                // assign new page number
                newPageNo = leftSwipe ? currentScreen + screenJump : currentScreen - screenJump;
            }
            else
            {
                // get direction of swipe
                var upSwipe = velocity.y < 0;

                // check if skip is possible
                var skip = CanSkipScreen(velY, upSwipe);

                // get size of screen jump
                var screenJump = skip ? 2 : 1;

                // assign new page number
                newPageNo = upSwipe ? currentScreen + screenJump : currentScreen - screenJump;
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
    }

    /// <summary>
    /// Returns if it is possible to skip a page
    /// </summary>
    /// <param name="increase"></param>
    /// <returns></returns>
    private bool CanSkipScreen(float velocity, bool increase)
    {
        if (!skipScreen || skipScreenVelocityThreshold <= 0)
            return false;

        return increase ? CurrentScreen < ScreenCount - 2 : CurrentScreen > 1;
    }

    /// <summary>
    /// Tweens the contents position
    /// </summary>
    /// <param name="toPos"></param>
    private void TweenPage(Vector2 toPos)
    {
        iTween.ValueTo(gameObject, iTween.Hash(
            "from", content.anchoredPosition,
            "to", toPos,
            "easeType", easeType,
            "time", tweenTime,
            "onupdate", (Action<Vector2>)(x => SetContentAnchoredPosition(x)),
            "oncomplete", (Action<Vector2>)(_ =>
            {
                if (onScreenTweenEnd != null)
                    onScreenTweenEnd.Invoke(currentScreen);
            })
            ));
    }
    #endregion

    #region Functions From Unity ScrollRect

    /* Note:
        * Everything in this region I sourced from Unity's ScrollRect script and rejigged to work in this script
        * Sources from: https://bitbucket.org/Unity-Technologies/ui 
        * Folder path: UI/UnityEngine.UI/UI/Core/ScrollRect.cs
        */


    /// <summary>
    /// Wrapper fucntion <see cref="ScrollRect.OnDrag(PointerEventData)"/>
    /// </summary>
    /// <param name="eventData"></param>
    private void DragContent(PointerEventData eventData)
    {
        Vector2 localCursor;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(content, eventData.position, eventData.pressEventCamera, out localCursor))
            return;

        UpdateBounds();

        var pointerDelta = localCursor - pointerStartLocalCursor;
        Vector2 position = content.anchoredPosition + pointerDelta;

        // Offset to get content into place in the view.
        Vector2 offset = CalculateOffset(position - content.anchoredPosition);
        position += offset;

        if (offset.x != 0)
            position.x = position.x - RubberDelta(offset.x, viewBounds.size.x);
        if (offset.y != 0)
            position.y = position.y - RubberDelta(offset.y, viewBounds.size.y);


        SetContentAnchoredPosition(position);
    }

    private void SetContentAnchoredPosition(Vector2 position)
    {
        if (swipeType == SwipeType.Vertical)
            position.x = content.anchoredPosition.x;

        if (swipeType == SwipeType.Horizonal)
            position.y = content.anchoredPosition.y;

        if (position != content.anchoredPosition)
        {
            content.anchoredPosition = position;
            UpdateBounds();
        }
    }

    private void UpdateBounds()
    {
        viewBounds = new Bounds(rectTransform.rect.center, rectTransform.rect.size);
        contentBounds = GetBounds();

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

    private Bounds GetBounds()
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

    private Vector2 CalculateOffset(Vector2 delta)
    {
        Vector2 offset = Vector2.zero;

        Vector2 min = contentBounds.min;
        Vector2 max = contentBounds.max;

        min.x += delta.x;
        max.x += delta.x;
        if (min.x > viewBounds.min.x)
            offset.x = viewBounds.min.x - min.x;
        else if (max.x < viewBounds.max.x)
            offset.x = viewBounds.max.x - max.x;


        return offset;
    }

    private float RubberDelta(float overStretching, float viewSize)
    {
        return (1 - (1 / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1))) * viewSize * Mathf.Sign(overStretching);
    }
    #endregion

    #region Editing
    [Header("Editing")]
    [SerializeField]
    [Tooltip("Screen you want to be showing in Game view. Note: 0 indexed array")]
    private int editingScreen;
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

#if UNITY_EDITOR

#region Custom Editor
[CustomEditor(typeof(ScreenSwipe))]
public class ScreenSwipeEditor : Editor
{
    // editing
    SerializedProperty _editingScreen;

    //swipe
    SerializedProperty _swipeType;
    SerializedProperty _swipeTime;
    SerializedProperty _swipeVelocityThreshold;
    SerializedProperty _skipScreen;
    SerializedProperty _skipScreenVelocityThreshold;

    // content
    SerializedProperty _maskContent;
    SerializedProperty _content;
    SerializedProperty _spacing;
    SerializedProperty _pagination;
    SerializedProperty _currentScreen;
    SerializedProperty _screens;

    // screen change events
    SerializedProperty _pollForScreenOrientationChange;
    SerializedProperty _editorRefreshKey;

    // controlls
    SerializedProperty _nextButton;
    SerializedProperty _previousButton;
    SerializedProperty _buttonsOnly;
    SerializedProperty _disableButtonsAtEnds;

    // tween
    SerializedProperty _tweenTime;
    SerializedProperty _easeType;

    // events
    SerializedProperty _onScreenDrag;
    SerializedProperty _onScreenChanged;
    SerializedProperty _onScreenTweenEnd;

    private void OnEnable()
    {
        // editing
        _editingScreen = serializedObject.FindProperty("editingScreen");

        // swipe
        _swipeType = serializedObject.FindProperty("swipeType");
        _swipeTime = serializedObject.FindProperty("swipeTime");
        _swipeVelocityThreshold = serializedObject.FindProperty("swipeVelocityThreshold");
        _skipScreen = serializedObject.FindProperty("skipScreen");
        _skipScreenVelocityThreshold = serializedObject.FindProperty("skipScreenVelocityThreshold");

        // content
        _maskContent = serializedObject.FindProperty("maskContent");
        _content = serializedObject.FindProperty("content");
        _spacing = serializedObject.FindProperty("spacing");
        _pagination = serializedObject.FindProperty("pagination");
        _currentScreen = serializedObject.FindProperty("currentScreen");
        _screens = serializedObject.FindProperty("screens");

        // screen change
        _pollForScreenOrientationChange = serializedObject.FindProperty("pollForScreenOrientationChange");
        _editorRefreshKey = serializedObject.FindProperty("editorRefreshKey");

        // controlls
        _nextButton = serializedObject.FindProperty("nextButton");
        _previousButton = serializedObject.FindProperty("previousButton");
        _buttonsOnly = serializedObject.FindProperty("buttonsOnly");
        _disableButtonsAtEnds = serializedObject.FindProperty("disableButtonsAtEnds");

        // tween
        _tweenTime = serializedObject.FindProperty("tweenTime");
        _easeType = serializedObject.FindProperty("easeType");

        // events
        _onScreenDrag = serializedObject.FindProperty("onScreenDragBegin");
        _onScreenChanged = serializedObject.FindProperty("onScreenChanged");
        _onScreenTweenEnd = serializedObject.FindProperty("onScreenTweenEnd");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var _target = (ScreenSwipe)target;

        // editing
        EditorGUILayout.PropertyField(_editingScreen);
        if (GUILayout.Button("GoToScreen"))
        {
            _target.EditingScreen();
        }

        //swipe
        EditorGUILayout.PropertyField(_swipeType);
        EditorGUILayout.PropertyField(_swipeTime);
        EditorGUILayout.PropertyField(_swipeVelocityThreshold);
        EditorGUILayout.PropertyField(_skipScreen);
        if (_skipScreen.boolValue)
            EditorGUILayout.PropertyField(_skipScreenVelocityThreshold);

        // contents
        EditorGUILayout.PropertyField(_maskContent);
        EditorGUILayout.PropertyField(_content);
        EditorGUILayout.PropertyField(_spacing);
        EditorGUILayout.PropertyField(_pagination);
        EditorGUILayout.PropertyField(_currentScreen);
        EditorGUILayout.PropertyField(_screens, true);

        //screen
        EditorGUILayout.PropertyField(_pollForScreenOrientationChange);
        if (_target.pollForScreenOrientationChange)
            EditorGUILayout.PropertyField(_editorRefreshKey);


        // controlls
        EditorGUILayout.PropertyField(_nextButton);
        EditorGUILayout.PropertyField(_previousButton);
        if (_target.NextButton != null || _target.PreviousButton != null)
        {
            EditorGUILayout.PropertyField(_buttonsOnly);
            EditorGUILayout.PropertyField(_disableButtonsAtEnds);
        }

        // tween
        EditorGUILayout.PropertyField(_tweenTime);
        EditorGUILayout.PropertyField(_easeType);

        //events
        EditorGUILayout.PropertyField(_onScreenDrag);
        EditorGUILayout.PropertyField(_onScreenChanged);
        EditorGUILayout.PropertyField(_onScreenTweenEnd);

        serializedObject.ApplyModifiedProperties();
    }
}
#endregion

#region Custom Menu
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
#endregion

#endif