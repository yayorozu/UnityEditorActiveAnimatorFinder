using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Yorozu.EditorTool
{
    internal class ActiveAnimatorFinderWindow : EditorWindow
    {
        [MenuItem("Tools/ActiveAnimatorFinder")]
        private static void ShowWindow()
        {
            var window = GetWindow<ActiveAnimatorFinderWindow>("ActiveAnimatorFinder");
            window.Show();
        }

        private float _refreshInterval = 1f;
        private DateTime _date;

        private void OnEnable()
        {
            EditorApplication.update += Refresh;
            _date = _date.AddSeconds(_refreshInterval);
        }

        private AnimatorTreeView _treeView;
        [SerializeField]
        private TreeViewState _state;
        [SerializeField]
        private MultiColumnHeaderState _columnHeaderState;
        
        private void InitIfNeeded()
        {
            _state ??= new TreeViewState();

            if (_treeView == null)
            {
                var headerState = AnimatorTreeView.CreateMultiColumnHeaderState();
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(_columnHeaderState, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(_columnHeaderState, headerState);
                _columnHeaderState = headerState;

                var multiColumnHeader = new MultiColumnHeader(_columnHeaderState);
                multiColumnHeader.ResizeToFit();
                _treeView ??= new AnimatorTreeView(_state, multiColumnHeader);
            }
        }

        private void AddInterval()
        {
            _date = DateTime.Now.AddMilliseconds(_refreshInterval * 1000f);
        }

        private void Refresh()
        {
            if (DateTime.Now < _date)
                return;
            
            if (!EditorApplication.isPlaying)
                return;
               
            if (_refreshInterval <= 0)
                return;
            
            AddInterval();
            _treeView?.Refresh();
            Repaint();
        }
        
        private void OnGUI()
        {
            InitIfNeeded();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Rebuild", EditorStyles.toolbarButton))
                {
                    _treeView = null;
                    EditorGUIUtility.ExitGUI();
                }
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Manual Refresh", EditorStyles.toolbarButton))
                {
                    _treeView?.Refresh();
                }

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    _refreshInterval = EditorGUILayout.Slider(new GUIContent("Refresh Interval", "0 is Stop"), _refreshInterval, 0f, 10f);
                    if (check.changed)
                    {
                        AddInterval();
                    }
                }
                
            }
            
            var rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
            _treeView?.OnGUI(rect);
        }
    }
}