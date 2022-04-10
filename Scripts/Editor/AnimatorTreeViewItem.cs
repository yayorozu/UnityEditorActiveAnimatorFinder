using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Yorozu.EditorTool
{

    internal class AnimatorTreeViewItem : TreeViewItem
    {
        private readonly Animator _animator;
        private readonly List<ClipData> _clips;
        private float _currentNormalizedTime;

        private string _currentStateName;
        private readonly string _layer;
        private readonly int _layerIndex;
        private float _length;

        internal AnimatorTreeViewItem(int id, Animator animator) : base(id, 0, animator.gameObject.name)
        {
            _animator = animator;
        }

        internal AnimatorTreeViewItem(Animator animator, int id, int layerIndex) : base(id, 1, animator.gameObject.name)
        {
            _animator = animator;
            _layerIndex = layerIndex;

            _clips = new List<ClipData>();
            var rac = animator.runtimeAnimatorController as AnimatorController;

            var layer = rac.layers[layerIndex];
            _layer = $"{layerIndex}: {layer.name}";

            foreach (var state in layer.stateMachine.states)
            {
                var clip = state.state.motion as AnimationClip;
                if (clip == null)
                    continue;

                _clips.Add(new ClipData
                {
                    Name = state.state.name,
                    Length = clip != null ? clip.length : 0f
                });
            }
        }

        internal int InstanceId => _animator.GetInstanceID();
        internal int GameObjectInstanceId => _animator.gameObject.GetInstanceID();

        internal void Refresh()
        {
            if (_animator == null || _clips == null)
                return;

            var si = _animator.GetCurrentAnimatorStateInfo(_layerIndex);
            _currentNormalizedTime = si.normalizedTime % 1f;
            _currentStateName = "";
            _length = 0f;
            foreach (var clip in _clips)
                if (si.IsName(clip.Name))
                {
                    _currentStateName = clip.Name;
                    _length = clip.Length;
                    break;
                }
        }

        internal void OnGUI(Rect rect, int columnIndex)
        {
            if (depth == 0)
            {
                EditorGUI.LabelField(rect, displayName);
                return;
            }

            switch (columnIndex)
            {
                // LayerIndex
                case 1:
                    EditorGUI.LabelField(rect, _layer);
                    break;
                // State
                case 2:
                    EditorGUI.LabelField(rect, _currentStateName);
                    break;
                // Length
                case 3:
                    EditorGUI.LabelField(rect, _length.ToString());
                    break;
                // NormalizedTime
                case 4:
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUI.Slider(rect, _currentNormalizedTime, 0f, 1f);
                    }

                    break;
            }
        }

        private class ClipData
        {
            internal float Length;
            internal string Name;
        }
    }
}