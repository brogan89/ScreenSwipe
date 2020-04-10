using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScreenSwipe))]
public class ScreenSwipeEditor : Editor
{
	// editing
	private SerializedProperty _editingScreen;

	//swipe
	private SerializedProperty _swipeType;
	private SerializedProperty _swipeTime;
	private SerializedProperty _swipeVelocityThreshold;

	// content
	private SerializedProperty _maskContent;
	private SerializedProperty _content;
	private SerializedProperty _spacing;
	private SerializedProperty _pagination;
	private SerializedProperty _currentScreen;
	private SerializedProperty _startingScreen;
	private SerializedProperty _screens;

	// screen change events
	private SerializedProperty _pollForScreenOrientationChange;

	// controlls
	private SerializedProperty _isInteractable;
	private SerializedProperty _nextButton;
	private SerializedProperty _previousButton;
	private SerializedProperty _disableButtonsAtEnds;

	// tween
	private SerializedProperty _tweenTime;
	private SerializedProperty _ease;

	// events
	private SerializedProperty _onScreenDrag;
	private SerializedProperty _onScreenChanged;

	private void OnEnable()
	{
		// editing
		_editingScreen = serializedObject.FindProperty("editingScreen");

		// swipe
		_swipeType = serializedObject.FindProperty("swipeType");
		_swipeTime = serializedObject.FindProperty("swipeTime");
		_swipeVelocityThreshold = serializedObject.FindProperty("swipeVelocityThreshold");

		// content
		_maskContent = serializedObject.FindProperty("maskContent");
		_content = serializedObject.FindProperty("content");
		_spacing = serializedObject.FindProperty("spacing");
		_pagination = serializedObject.FindProperty("pagination");
		_currentScreen = serializedObject.FindProperty("currentScreen");
		_startingScreen = serializedObject.FindProperty("startingScreen");
		_screens = serializedObject.FindProperty("screens");

		// screen change
		_pollForScreenOrientationChange = serializedObject.FindProperty("pollForScreenOrientationChange");

		// controlls
		_isInteractable = serializedObject.FindProperty("isInteractable");
		_nextButton = serializedObject.FindProperty("nextButton");
		_previousButton = serializedObject.FindProperty("previousButton");
		_disableButtonsAtEnds = serializedObject.FindProperty("disableButtonsAtEnds");

		// tween
		_tweenTime = serializedObject.FindProperty("tweenTime");
		_ease = serializedObject.FindProperty("ease");

		// events
		_onScreenDrag = serializedObject.FindProperty("onScreenDragBegin");
		_onScreenChanged = serializedObject.FindProperty("onScreenChanged");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		var _target = (ScreenSwipe)target;

		// editing
		EditorGUILayout.PropertyField(_editingScreen);
		if (GUILayout.Button("GoToScreen"))
			_target.EditingScreen();

		//swipe
		EditorGUILayout.PropertyField(_swipeType);
		EditorGUILayout.PropertyField(_swipeTime);
		EditorGUILayout.PropertyField(_swipeVelocityThreshold);

		// contents
		EditorGUILayout.PropertyField(_maskContent);
		EditorGUILayout.PropertyField(_content);
		EditorGUILayout.PropertyField(_spacing);
		EditorGUILayout.PropertyField(_pagination);
		EditorGUILayout.PropertyField(_currentScreen);
		EditorGUILayout.PropertyField(_startingScreen);
		EditorGUILayout.PropertyField(_screens, true);

		//screen
		EditorGUILayout.PropertyField(_pollForScreenOrientationChange);

		// controlls
		EditorGUILayout.PropertyField(_isInteractable);
		EditorGUILayout.PropertyField(_nextButton);
		EditorGUILayout.PropertyField(_previousButton);
		if (_target.NextButton != null || _target.PreviousButton != null)
			EditorGUILayout.PropertyField(_disableButtonsAtEnds);

		// tween
		EditorGUILayout.PropertyField(_tweenTime);
		EditorGUILayout.PropertyField(_ease);

		//events
		EditorGUILayout.PropertyField(_onScreenDrag);
		EditorGUILayout.PropertyField(_onScreenChanged);

		serializedObject.ApplyModifiedProperties();
	}
}