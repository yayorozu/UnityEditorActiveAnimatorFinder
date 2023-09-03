using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Yorozu.EditorTool
{
    internal class AnimatorTreeView : TreeView
    {
        private readonly List<AnimatorTreeViewItem> _rows = new();

        internal AnimatorTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state,
            multiColumnHeader)
        {
            showAlternatingRowBackgrounds = true;
            showBorder = true;

            Reload();
            ExpandAll();
        }

        internal void Refresh()
        {
            foreach (var row in _rows) row.Refresh();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(0, -1, "root");
            _rows.Clear();
            var animators = Object.FindObjectsOfType<Animator>()
                    .Where(a => a.runtimeAnimatorController != null)
                ;

            var id = 1;
            foreach (var animator in animators)
            {
                var parent = new AnimatorTreeViewItem(id++, animator);
                _rows.Add(parent);
                root.AddChild(parent);

                for (var i = 0; i < animator.layerCount; i++)
                {
                    var item = new AnimatorTreeViewItem(animator, id++, i);
                    _rows.Add(item);
                    parent.AddChild(item);
                }
            }

            SetupDepthsFromParentsAndChildren(root);

            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            if (!root.hasChildren)
                return new List<TreeViewItem>();

            return base.BuildRows(root);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (AnimatorTreeViewItem) args.item;
            if (item.depth == 0)
            {
                base.RowGUI(args);
                return;
            }

            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                var rect = args.GetCellRect(i);
                CenterRectUsingSingleLineHeight(ref rect);
                item.OnGUI(rect, args.GetColumn(i));
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = GetRows().First(r => r.id == id);
            var atvi = item as AnimatorTreeViewItem;
            Selection.instanceIDs = new[]
            {
                atvi.InstanceId,
                atvi.GameObjectInstanceId
            };
            var animationWindowType = typeof(Graph).Assembly.GetType("UnityEditor.Graphs.AnimatorControllerTool");
            var findSceneView = Resources.FindObjectsOfTypeAll(animationWindowType);
            if (findSceneView.Length > 0)
            {
                var editorWindow = findSceneView[0] as EditorWindow;
                editorWindow.Focus();
            }
        }

        /// <summary>
        ///     ヘッダー情報一覧
        /// </summary>
        internal static MultiColumnHeaderState CreateMultiColumnHeaderState()
        {
            var columns = new List<MultiColumnHeaderState.Column>();
            var names = new[]
            {
                "Name",
                "LayerIndex",
                "State",
                "Length",
                "NormalizedTime"
            };
            foreach (var name in names)
                columns.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(name),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    autoResize = true,
                    allowToggleVisibility = false,
                    canSort = false,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 80,
                    minWidth = 50,
                    maxWidth = 100
                });
            columns[4].minWidth = columns[4].width = 150f;
            columns[4].maxWidth = 200f;

            return new MultiColumnHeaderState(columns.ToArray());
        }
    }
}