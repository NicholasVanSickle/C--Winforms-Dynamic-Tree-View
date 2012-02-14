using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace DynamicTreeView
{
    //Represents how the elements in tree nodes are rendered - Simple roughly approximates a WinForms TreeView while Native uses the Windows Vista/7 tree view rendering elements. FakeNative approximates them.
    public enum DynamicTreeViewRenderStyle
    {
        Simple,
        Native,
        FakeNative
    }

    //Displays a tree of elements in a manner similar to Windows Explorer, allowing for, among other things, custom text formatting and line heights
    public class DynamicTreeView : ContainerControl
    {
        public Size NodeSpacing { get; set; } //Gets or sets the spacing between nodes
        //public bool ShowLines { get; set; }
        public DynamicTreeViewRenderStyle SelectionRenderStyle { get; set; } //Gets or sets the rendering method used to draw selection rectangles
        public DynamicTreeViewRenderStyle ArrowRenderStyle { get; set; } //Gets or sets the rendering method used to draw the expand/collapse toggle elements

        private DynamicTreeNode root = null;
        public DynamicTreeNode Root { get { return root; } set { root = value; if (root != null) root.Expanded = true; Invalidate(); } } //Gets or sets a root node for the view. Its nodes will be shown as the root's child nodes.

        public void SetRoot(IDynamicTreeNodeDataProxy data)
        {
            Root = data != null ? new DynamicTreeNode(new DynamicTreeNodeCollection(this), data) : null;
        }

        private DynamicTreeNodeCollection nodes;
        public DynamicTreeNodeCollection Nodes
        {
            get
            {
                if (Root != null)
                    return Root.Nodes;
                return nodes;
            }
            private set
            {
                nodes = value;
            }
        }

        public DynamicTreeView()
            : base()
        {
            NodeSpacing = new Size(4, 4);
            SelectionRenderStyle = ArrowRenderStyle = DynamicTreeViewRenderStyle.Native;

            Nodes = new DynamicTreeNodeCollection(this);

            DoubleBuffered = true;
            ResizeRedraw = true;
            AutoScroll = true;

            VerticalScroll.SmallChange = Font.Height * 3;
            VerticalScroll.LargeChange = Font.Height * 10;
        }

        private DynamicTreeNode highlightNode = null;
        public DynamicTreeNode HighlightNode
        {
            get
            {
                return highlightNode;
            }

            set
            {
                highlightNode = value;

                if (highlightNode != null)
                    HighlightArrow = null;

                Invalidate();
            }
        }

        private DynamicTreeNode highlightArrow = null;
        private Rectangle highlightArrowRect;
        public DynamicTreeNode HighlightArrow
        {
            get
            {
                return highlightArrow;
            }

            set
            {
                if (highlightArrow != value)
                {
                    highlightArrow = value;
                    if (highlightArrow != null)
                        highlightArrowRect = highlightArrow.ArrowBox;
                }

                if (highlightArrow != null)
                    HighlightNode = null;

                Invalidate();
            }
        }

        public event Action<DynamicTreeView> SelectedNodeChanged;

        private DynamicTreeNode selectedNode;
        public DynamicTreeNode SelectedNode
        {
            get
            {
                return selectedNode;
            }

            set
            {
                if (selectedNode != value)
                {
                    selectedNode = value;
                    if (SelectedNodeChanged != null)
                        SelectedNodeChanged(this);
                }

                if (selectedNode != null)
                {
                    if (selectedNode.Bounds.Y < 0)
                    {
                        VerticalScroll.Value = Math.Max(0, selectedNode.Bounds.Y - DisplayRectangle.Y - NodeSpacing.Height);
                        AdjustFormScrollbars(true);
                    }
                    else if (selectedNode.Bounds.Bottom > ClientSize.Height)
                    {
                        VerticalScroll.Value = selectedNode.Bounds.Bottom + NodeSpacing.Height - DisplayRectangle.Y - ClientSize.Height;
                        AdjustFormScrollbars(true);
                    }
                }

                Invalidate();
            }
        }

        private void ValidateSelectedNode()
        {
            if (SelectedNode != null)
            {
                if (!Nodes.VisibleNodes.Contains(SelectedNode))
                    SelectedNode = null;
            }
        }

        private Size? arrowSize = null;

        private void updateMouseInfo(Point location)
        {
            foreach (DynamicTreeNode n in Nodes.VisibleNodes)
            {
                if (n.ArrowBox.Contains(location))
                {
                    HighlightArrow = n;
                    return;
                }
                else if (n.Bounds.Contains(location))
                {
                    HighlightNode = n;
                    return;
                }
            }

            HighlightArrow = null;
            HighlightNode = null;
        }

        public DynamicTreeNode NodeAt(Point location)
        {
            foreach (DynamicTreeNode n in Nodes.VisibleNodes)
                if (n.Bounds.Contains(location))
                    return n;
            return null;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            HighlightArrow = null;
            HighlightNode = null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!arrowSize.HasValue)
                return;

            updateMouseInfo(e.Location);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (!Focused)
                Focus();

            updateMouseInfo(e.Location);
            if (e.Button == MouseButtons.Left)
            {
                if (highlightArrow != null)
                {
                    highlightArrow.Expanded = !highlightArrow.Expanded;
                    Invalidate();
                }

                if (highlightNode != null)
                {
                    SelectedNode = highlightNode;
                }
            }

            DynamicTreeNode n = NodeAt(e.Location);
            OnNodeMouseDown(new DynamicTreeNodeMouseEventArgs(n, e));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            DynamicTreeNode n = NodeAt(e.Location);
            OnNodeMouseUp(new DynamicTreeNodeMouseEventArgs(n, e));
        }

        private MouseEventArgs lastClick = null;

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            lastClick = e;

            DynamicTreeNode n = NodeAt(e.Location);
            OnNodeMouseClick(new DynamicTreeNodeMouseEventArgs(n, e));
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);

            if (lastClick == null)
                return;

            DynamicTreeNode n = NodeAt(lastClick.Location);
            OnNodeMouseDoubleClick(new DynamicTreeNodeMouseEventArgs(n, lastClick));
        }

        public event DynamicTreeNodeMouseEventHandler NodeMouseClick;
        public event DynamicTreeNodeMouseEventHandler NodeMouseDoubleClick;
        public event DynamicTreeNodeMouseEventHandler NodeMouseDown;
        public event DynamicTreeNodeMouseEventHandler NodeMouseUp;

        protected virtual void OnNodeMouseClick(DynamicTreeNodeMouseEventArgs e)
        {
            if (NodeMouseClick != null)
                NodeMouseClick(this, e);
        }

        protected virtual void OnNodeMouseDown(DynamicTreeNodeMouseEventArgs e)
        {
            if (NodeMouseDown != null)
                NodeMouseDown(this, e);
        }

        protected virtual void OnNodeMouseUp(DynamicTreeNodeMouseEventArgs e)
        {
            if (NodeMouseUp != null)
                NodeMouseUp(this, e);
        }

        protected virtual void OnNodeMouseDoubleClick(DynamicTreeNodeMouseEventArgs e)
        {
            if (NodeMouseDoubleClick != null)
                NodeMouseDoubleClick(this, e);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Down)
            {
                if (SelectedNode == null)
                {
                    if (Nodes.Count == 0)
                        return;
                    SelectedNode = Nodes.VisibleNodes.First();
                    return;
                }
                int index = Nodes.VisibleNodes.IndexOf(SelectedNode) + 1;
                index = Math.Min(Nodes.VisibleNodes.Count - 1, index);
                SelectedNode = Nodes.VisibleNodes[index];
            }
            else if (e.KeyCode == Keys.Up)
            {
                if (SelectedNode == null)
                {
                    if (Nodes.Count == 0)
                        return;
                    SelectedNode = Nodes.VisibleNodes.Last();
                    return;
                }
                int index = Nodes.VisibleNodes.IndexOf(SelectedNode) - 1;
                index = Math.Max(0, index);
                SelectedNode = Nodes.VisibleNodes[index];
            }
            else if (e.KeyCode == Keys.Left)
            {
                if (SelectedNode != null && SelectedNode.Expanded)
                {
                    SelectedNode.Expanded = false;
                    Invalidate();
                }
            }
            else if (e.KeyCode == Keys.Right)
            {
                if (SelectedNode != null && !SelectedNode.Expanded)
                {
                    SelectedNode.Expanded = true;
                    Invalidate();
                }
            }
            else if (e.KeyCode == Keys.Home)
            {
                SelectedNode = Nodes.VisibleNodes.First();
            }
            else if (e.KeyCode == Keys.End)
            {
                SelectedNode = Nodes.VisibleNodes.Last();
            }
            else if (e.KeyCode == Keys.PageDown)
            {
                if (SelectedNode == null)
                {
                    SelectedNode = Nodes.VisibleNodes.First();
                    return;
                }
                int index = Nodes.VisibleNodes.IndexOf(SelectedNode) + 10;
                index = Math.Min(Nodes.VisibleNodes.Count - 1, index);
                SelectedNode = Nodes.VisibleNodes[index];
            }
            else if (e.KeyCode == Keys.PageUp)
            {
                if (SelectedNode == null)
                {
                    SelectedNode = Nodes.VisibleNodes.Last();
                    return;
                }

                int index = Nodes.VisibleNodes.IndexOf(SelectedNode) - 10;
                index = Math.Max(0, index);
                SelectedNode = Nodes.VisibleNodes[index];
            }
            /*
            if (e.KeyCode == Keys.F)
            {
                if (ArrowRenderStyle == DynamicTreeViewRenderStyle.Native)
                {
                    ArrowRenderStyle = DynamicTreeViewRenderStyle.FakeNative;
                    SelectionRenderStyle = DynamicTreeViewRenderStyle.FakeNative;
                }
                else
                {
                    ArrowRenderStyle = DynamicTreeViewRenderStyle.Native;
                    SelectionRenderStyle = DynamicTreeViewRenderStyle.Native;
                }
                Invalidate();
            }*/
        }

        protected virtual void DrawArrow(Graphics g, Rectangle r, bool expanded, bool highlighted)
        {
            VisualStyleRenderer renderer = null;
            if (ArrowRenderStyle == DynamicTreeViewRenderStyle.Native)
                if (highlighted)
                    renderer = expanded ? ExplorerViewStyle.OpenedHover : ExplorerViewStyle.ClosedHover;
                else
                    renderer = expanded ? ExplorerViewStyle.Opened : ExplorerViewStyle.Closed;
            else if (ArrowRenderStyle == DynamicTreeViewRenderStyle.Simple)
                renderer = new VisualStyleRenderer(expanded ? VisualStyleElement.TreeView.Glyph.Opened : VisualStyleElement.TreeView.Glyph.Closed);

            if (renderer != null)
                renderer.DrawBackground(g, r);
            else
            {
                FakeNativeTreeStyleRenderer.DrawArrow(g, r, expanded, highlighted);
            }
        }

        protected virtual void DrawSelection(Graphics g, Rectangle r, Color? c = null)
        {
            bool fakeNative = SelectionRenderStyle == DynamicTreeViewRenderStyle.Native && c != null;
            if (SelectionRenderStyle == DynamicTreeViewRenderStyle.Native && c == null)
            {
                VisualStyleRenderer renderer = Focused ? ExplorerViewStyle.ItemSelect : ExplorerViewStyle.ItemSelectNoFocus;
                renderer.DrawBackground(g, r);
            }

            if (c == null)
                c = Focused ? FakeNativeTreeStyleRenderer.SelectionColor : FakeNativeTreeStyleRenderer.NoFocusSelectionColor;

            if (SelectionRenderStyle == DynamicTreeViewRenderStyle.FakeNative || fakeNative)
            {
                FakeNativeTreeStyleRenderer.DrawSelection(g, r, c.Value);
            }
            else if (SelectionRenderStyle == DynamicTreeViewRenderStyle.Simple)
            {
                g.FillRectangle(new SolidBrush(FakeNativeTreeStyleRenderer.AlphaBlend(50, c.Value)), r);
                g.DrawRectangle(new Pen(c.Value, 1.0f), r);
            }
        }

        protected virtual void DrawHighlight(Graphics g, Rectangle r, Color? c = null)
        {
            bool fakeNative = SelectionRenderStyle == DynamicTreeViewRenderStyle.Native && c != null;
            if (SelectionRenderStyle == DynamicTreeViewRenderStyle.Native && c == null)
            {
                VisualStyleRenderer renderer = ExplorerViewStyle.ItemHover;
                renderer.DrawBackground(g, r);
            }

            if (c == null)
                c = Color.FromArgb(100, FakeNativeTreeStyleRenderer.SelectionColor);
            else
                c = FakeNativeTreeStyleRenderer.AlphaBlend(100, c.Value);

            if (SelectionRenderStyle == DynamicTreeViewRenderStyle.FakeNative || fakeNative)
            {
                FakeNativeTreeStyleRenderer.DrawSelection(g, r, c.Value);
            }
            else if (SelectionRenderStyle == DynamicTreeViewRenderStyle.Simple)
            {
                g.FillRectangle(new SolidBrush(FakeNativeTreeStyleRenderer.AlphaBlend(50, c.Value)), r);
                g.DrawRectangle(new Pen(c.Value, 1.0f), r);
            }
        }

        public void ForceBoundsUpdate()
        {
            int y = NodeSpacing.Height + DisplayRectangle.Y;
            PaintNodes(null, Nodes, ref y);
        }

        protected virtual void PaintNodes(Graphics g, DynamicTreeNodeCollection nodes, ref int y)
        {
            foreach (DynamicTreeNode n in nodes.VisibleNodes)
            {
                bool draw = y + n.Bounds.Height > 0 && y < ClientSize.Height;

                int x = NodeSpacing.Width + n.Depth * arrowSize.Value.Width;

                if (draw && n.CanExpand)
                {
                    int f = (int)(Font.Size - 8.5);
                    Rectangle rect = new Rectangle(x, y - 2 + f, arrowSize.Value.Width, arrowSize.Value.Height);
                    n.ArrowBox = rect;
                    if (g != null)
                        DrawArrow(g, rect, n.Expanded, n == HighlightArrow);
                }
                x += arrowSize.Value.Width - 4;

                n.Position = new Point(x, y);

                if (n.Render && g != null)
                {
                    if (n == SelectedNode)
                    {
                        DrawSelection(g, Rectangle.Inflate(n.Bounds, -1, 2));
                    }

                    if (n == HighlightNode)
                    {
                        DrawHighlight(g, Rectangle.Inflate(n.Bounds, -1, 2));
                    }

                    if (SelectedNode != null && SelectedNode.HighlightWhenSelected(n.DataNode))
                    {
                        DrawSelection(g, Rectangle.Inflate(n.Bounds, -1, 2), Color.Yellow);
                    }

                    if (HighlightNode != null && HighlightNode.HighlightWhenSelected(n.DataNode))
                    {
                        DrawHighlight(g, Rectangle.Inflate(n.Bounds, -1, 2), Color.Yellow);
                    }

                    if (draw)
                        n.Draw(g);
                }
                //g.DrawRectangle(new Pen(Brushes.Gray, 1.0f), n.Bounds);
                y += n.Bounds.Height + NodeSpacing.Height;

                //if (n.Expanded)
                //    PaintNodes(g, n.Nodes, depth + 1, ref y);
            }
        }

        private Rectangle BorderRectangle { get { return new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            ValidateSelectedNode();

            if (!arrowSize.HasValue)
                arrowSize = ExplorerViewStyle.Opened.GetPartSize(e.Graphics, ThemeSizeType.Draw);

            if (SelectedNode != null && !SelectedNode.Visible)
                SelectedNode = SelectedNode.Parent;

            int y = NodeSpacing.Height + DisplayRectangle.Y;
            PaintNodes(e.Graphics, Nodes, ref y);
            if (AutoScrollMinSize.Height != y - DisplayRectangle.Y)
                AutoScrollMinSize = new Size(AutoScrollMinSize.Width, y - DisplayRectangle.Y);

            e.Graphics.DrawRectangle(new Pen(Brushes.Gray, 1.0f), BorderRectangle);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);

            e.Graphics.FillRectangle(Brushes.White, BorderRectangle);
        }

        //prevent some scroll flicker shit -- waits until the frame draws completely to swap the new buffer in
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            if (se.NewValue != se.OldValue)
            {
                HighlightArrow = null;
                HighlightNode = null;
                Invalidate();
            }
        }
    }
}
