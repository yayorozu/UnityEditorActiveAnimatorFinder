using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yorozu.EditorTool
{
    internal class AnimatorTreeView : TreeView
    {
	    private List<AnimatorTreeViewItem> _rows = new List<AnimatorTreeViewItem>();

	    internal AnimatorTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
	        showAlternatingRowBackgrounds = true;
	        showBorder = true;

	        Reload();
	        ExpandAll();
        }

	    internal void Refresh()
	    {
		    foreach (var row in _rows)
		    {
			    row.Refresh();
		    }
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
	        Selection.instanceIDs = new []
	        {
		        atvi.InstanceId,
		        atvi.GameObjectInstanceId,
	        };
	        var animationWindowType = typeof(UnityEditor.Graphs.Graph).Assembly.GetType("UnityEditor.Graphs.AnimatorControllerTool");
	        var findSceneView = Resources.FindObjectsOfTypeAll(animationWindowType);
	        if (findSceneView.Length > 0)
	        {
		        var editorWindow = findSceneView[0] as EditorWindow;
		        editorWindow.Focus();
	        }
        }

        /// <summary>
        /// ヘッダー情報一覧
        /// </summary>
        internal static MultiColumnHeaderState CreateMultiColumnHeaderState()
        {
	        var columns = new List<MultiColumnHeaderState.Column>();
	        var names = new string[]
	        {
		        "Name",
		        "LayerIndex",
		        "State",
		        "Length",
		        "NormalizedTime",
	        };
	        foreach (var name in names)
	        {
		        columns.Add(new MultiColumnHeaderState.Column()
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
				    maxWidth = 100,
			    });
	        }
	        columns[4].minWidth = columns[4].width = 150f;
	        columns[4].maxWidth = 200f;
	        
		    return new MultiColumnHeaderState(columns.ToArray());
	    }
    }

    internal class AnimatorTreeViewItem : TreeViewItem
    {
	    private Animator _animator;
	    private string _layer;
	    private int _layerIndex;

	    private string _currentStateName;
	    private float _currentNormalizedTime;
	    private float _length;
	    private List<ClipData> _clips;

	    internal int InstanceId => _animator.GetInstanceID();
	    internal int GameObjectInstanceId => _animator.gameObject.GetInstanceID();

	    private class ClipData
	    {
		    internal string Name;
		    internal int NameHash;
		    internal float Length;
	    }

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
				    NameHash = state.state.nameHash,
				    Length = clip != null ? clip.length : 0f,
			    });
		    }
	    }
	    
	    internal void Refresh()
	    {
		    if (_animator == null || _clips == null)
			    return;

		    var si = _animator.GetCurrentAnimatorStateInfo(_layerIndex);
		    _currentNormalizedTime = si.normalizedTime % 1f;
		    _currentStateName = "";
		    _length = 0f;
		    foreach (var clip in _clips)
		    {
			    if (si.IsName(clip.Name))
			    {
				    _currentStateName = clip.Name;
				    _length = clip.Length;
				    break;
			    }
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
				    EditorGUI.Slider(rect, _currentNormalizedTime, 0f, 1f);
				    break;
		    }
	    }
    }
}