using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace MissNibiru.Narrative.Editor
{
    internal sealed class NarrativeGraphView : GraphView
    {
        private readonly Action<NarrativeNode> _selected;
        private readonly Dictionary<string, NarrativeNodeView> _views =
            new Dictionary<string, NarrativeNodeView>();
        private NarrativeStory _story;
        private bool _loading;

        public NarrativeGraphView(Action<NarrativeNode> selected)
        {
            _selected = selected;
            style.flexGrow = 1f;
            style.backgroundColor = new Color(0.055f, 0.04f, 0.075f);
            GridBackground grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
            SetupZoom(0.25f, 2f);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            graphViewChanged = HandleGraphViewChanged;
        }

        public void LoadStory(NarrativeStory story)
        {
            _loading = true;
            _story = story;
            DeleteElements(graphElements.ToList());
            _views.Clear();

            if (story != null)
            {
                foreach (NarrativeNode node in story.Nodes)
                {
                    if (node == null || _views.ContainsKey(node.Id))
                        continue;

                    NarrativeNodeView view = new NarrativeNodeView(
                        node,
                        selectedView => _selected?.Invoke(selectedView.Model));
                    _views.Add(node.Id, view);
                    AddElement(view);
                }

                foreach (NarrativeNode node in story.Nodes)
                    AddSavedEdges(node);
            }

            _loading = false;
        }

        public NarrativeNode GetSelectedNode()
        {
            return selection.OfType<NarrativeNodeView>()
                .FirstOrDefault()?.Model;
        }

        public Vector2 GetCreationPosition()
        {
            Vector2 local = contentViewContainer.WorldToLocal(
                worldBound.center);
            return local;
        }

        public override List<Port> GetCompatiblePorts(
            Port startPort,
            NodeAdapter nodeAdapter)
        {
            return ports.Where(port =>
                    port != startPort &&
                    port.direction != startPort.direction &&
                    port.node != startPort.node)
                .ToList();
        }

        private void AddSavedEdges(NarrativeNode node)
        {
            if (node == null || !_views.TryGetValue(
                    node.Id, out NarrativeNodeView source))
            {
                return;
            }

            for (int i = 0; i < source.Outputs.Count; i++)
            {
                string targetId =
                    NarrativeNodeConnectionUtility.GetTarget(node, i);

                if (string.IsNullOrWhiteSpace(targetId) ||
                    !_views.TryGetValue(
                        targetId, out NarrativeNodeView target) ||
                    target.Input == null)
                {
                    continue;
                }

                Edge edge = source.Outputs[i].ConnectTo(target.Input);
                AddElement(edge);
            }
        }

        private GraphViewChange HandleGraphViewChanged(
            GraphViewChange change)
        {
            if (_loading || _story == null)
                return change;

            if (change.elementsToRemove != null)
            {
                foreach (Edge edge in change.elementsToRemove.OfType<Edge>())
                {
                    if (!(edge.output?.node is NarrativeNodeView source))
                        continue;

                    int index = edge.output.userData is int value
                        ? value
                        : 0;
                    Undo.RecordObject(source.Model, "Disconnect Narrative Node");
                    NarrativeNodeConnectionUtility.SetTarget(
                        source.Model, index, string.Empty);
                    EditorUtility.SetDirty(source.Model);
                }
            }

            if (change.edgesToCreate != null)
            {
                foreach (Edge edge in change.edgesToCreate)
                {
                    if (!(edge.output?.node is NarrativeNodeView source) ||
                        !(edge.input?.node is NarrativeNodeView target))
                    {
                        continue;
                    }

                    int index = edge.output.userData is int value
                        ? value
                        : 0;
                    Undo.RecordObject(source.Model, "Connect Narrative Node");
                    NarrativeNodeConnectionUtility.SetTarget(
                        source.Model, index, target.Model.Id);
                    EditorUtility.SetDirty(source.Model);
                }
            }

            if (change.movedElements != null)
            {
                foreach (NarrativeNodeView view in
                         change.movedElements.OfType<NarrativeNodeView>())
                {
                    Undo.RecordObject(view.Model, "Move Narrative Node");
                    view.Model.SetEditorPosition(view.GetPosition().position);
                    EditorUtility.SetDirty(view.Model);
                }
            }

            EditorUtility.SetDirty(_story);
            return change;
        }
    }
}
