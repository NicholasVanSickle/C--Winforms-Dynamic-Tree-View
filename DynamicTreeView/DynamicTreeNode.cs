using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace DynamicTreeView
{
    public class DynamicTreeNode
    {
        private IDynamicTreeNodeDataProxy clientData;
        public IDynamicTreeNodeDataProxy ClientData
        {
            get
            {
                return clientData;
            }

            set
            {
                if (clientData != null)
                {
                    foreach (IDynamicTreeNodeDataProxy d in clientData.Children)
                        RemoveDataChild(d);
                    clientData.UpdateNode -= UpdateFromData;
                    clientData.RemoveNode -= Remove;
                    if (clientData.Child != null)
                    {
                        clientData.Child.ChildRemoved -= RemoveDataChild;
                        clientData.Child.ChildAdded -= AddDataChild;
                        clientData.Child.ChildInserted -= InsertedDataChild;
                        clientData.Child.ChildInserted -= ChangedDataChild;
                    }
                }
                clientData = value;
                if (clientData != null)
                {
                    foreach (IDynamicTreeNodeDataProxy d in clientData.Children)
                        AddDataChild(d);
                    clientData.UpdateNode += UpdateFromData;
                    clientData.RemoveNode += Remove;
                    if (clientData.Child != null)
                    {
                        clientData.Child.ChildRemoved += RemoveDataChild;
                        clientData.Child.ChildAdded += AddDataChild;
                        clientData.Child.ChildInserted += InsertedDataChild;
                        clientData.Child.ChildInserted += ChangedDataChild;
                    }
                }
            }
        }

        private void RemoveDataChild(IDynamicTreeNodeDataProxy d)
        {
            Nodes.RemoveAll(n => n.ClientData == d);
        }

        private void AddDataChild(IDynamicTreeNodeDataProxy d)
        {
            Nodes.Add(new DynamicTreeNode(Nodes, d));
        }

        private void InsertedDataChild(int index, IDynamicTreeNodeDataProxy d)
        {
            Nodes.Insert(index, new DynamicTreeNode(Nodes, d));
        }

        private void ChangedDataChild(int index, IDynamicTreeNodeDataProxy d)
        {
            Nodes[index] = new DynamicTreeNode(Nodes, d);
        }

        private void UpdateFromData()
        {
            UpdateTextSize();
            Nodes.Refresh();
        }

        public DynamicTreeNode(DynamicTreeNodeCollection collection, IDynamicTreeNodeDataProxy data)
        {
            ParentNodes = collection;
            nodes = new DynamicTreeNodeCollection(collection.View, this);
            ClientData = data;
            Render = true;
        }

        private void updateRenderer()
        {
            int w = (int)View.ClientSize.Width - Position.X - 1;
            if (Icon != null)
                w -= Icon.Width + IconPadding * 2;
            updateTextSize = updateTextSize || clientWidth != w;
            if (updateTextSize)
            {
                clientWidth = w;
                int lines = WordWrap ? WordWrapLines : -1;
                if (ExpandText && Expanded)
                    lines = -1;
                renderer = new NodeTextRenderer(Font, TextRenderFlags, Text, w, lines);
                textSize = renderer.Dimensions();
                updateTextSize = false;
                View.Invalidate();
            }
        }

        protected NodeTextRenderer Renderer
        {
            get
            {
                updateRenderer();
                return renderer;
            }

            set
            {
                renderer = value;
                updateTextSize = false;
            }
        }

        private NodeTextRenderer renderer;
        private Size textSize;
        private int clientWidth = -1;
        private bool updateTextSize = true;

        public void UpdateTextSize()
        {
            if (!updateTextSize)
            {
                updateTextSize = true;
                updateRenderer();
            }
        }

        public TextFormatFlags TextRenderFlags = TextFormatFlags.NoPrefix | TextFormatFlags.TextBoxControl;

        public virtual bool WordWrap { get { return ClientData.WordWrap; } }

        public virtual int WordWrapLines { get { return ClientData.WordWrapLines; } }

        public virtual Bitmap Icon { get { return ClientData.Icon; } }

        public readonly int IconPadding = 4;

        public virtual Rectangle Bounds
        {
            get
            {
                if (View == null)
                    return new Rectangle();
                updateRenderer();
                Rectangle bounds = new Rectangle(Position, textSize);
                if (Icon != null)
                {
                    bounds.Width += Icon.Width + IconPadding * 2;
                    if (Icon.Height > bounds.Height)
                        bounds.Height = Icon.Height;
                }
                return bounds;
            }
        }

        public int Depth { get { return Parent != null && View != null && Parent != View.Root ? 1 + Parent.Depth : 0; } }

        public bool Render { get; set; }

        public virtual void Draw(Graphics g)
        {
            if (View == null)
                return;
            Point position = Position;
            if (Icon != null)
            {
                position.X += IconPadding;
                Icon.SetResolution(0.5f, 0.5f);
                g.DrawImage(Icon, position);
                position.X += Icon.Width;
            }
            Renderer.Draw(g, position);
        }

        public virtual bool Visible
        {
            get
            {
                return ParentNodes != null && ClientData.Visible && (Parent == null || (Parent.Expanded && Parent.Visible));
            }
        }

        public bool CanBeVisible
        {
            get
            {
                return clientData.Visible;
            }
        }

        public void Remove()
        {
            if (ParentNodes != null)
            {
                ParentNodes.Remove(this);
                parentNodes = null;
            }
        }

        public DynamicTreeNodeCollection parentNodes;
        public DynamicTreeNodeCollection ParentNodes
        {
            get
            {
                return parentNodes;
            }

            set
            {
                Remove();
                parentNodes = value;
                if (parentNodes != null)
                    parentNodes.Refresh();
            }
        }

        public DynamicTreeNode Parent { get { return ParentNodes == null ? null : ParentNodes.Node; } }
        public DynamicTreeView View { get { return ParentNodes == null ? null : ParentNodes.View; } }

        public virtual bool HighlightWhenSelected(DynamicTreeNode n)
        {
            return ClientData.HighlightWhenSelected.Contains(n.ClientData);
        }

        public bool ExpandText
        {
            get
            {
                return ClientData.ExpandText;
            }
        }

        private bool expanded;
        public bool Expanded
        {
            get
            {
                return expanded;
            }
            set
            {
                if (expanded != value)
                {
                    expanded = value;
                    Nodes.Refresh();

                    if (ExpandText)
                        updateTextSize = true;
                }
            }
        }

        public virtual bool CanExpand
        {
            get
            {
                if (Nodes.Any(n => n.CanBeVisible))
                    return true;

                if (ExpandText && Renderer.Components.Count > WordWrapLines)
                    return true;

                return false;
            }
        }

        public virtual string Text
        {
            get
            {
                return ClientData.Text;
            }
        }

        public virtual Font Font
        {
            get
            {
                return ClientData.Font ?? View.Font;
            }
        }

        //controlled by DynamicTreeView / DynamicTreeNodeCollection
        public Point Position;
        public Rectangle ArrowBox;

        private DynamicTreeNodeCollection nodes = null;
        public virtual DynamicTreeNodeCollection Nodes { get { return nodes; } }
        public virtual DynamicTreeNode DataNode { get { return this; } }
    }

    //symbolic link to another node, will attempt to mirror linked node's behavior and data while retaining its own state
    public sealed class DynamicTreeLinkNode : DynamicTreeNode
    {
        public DynamicTreeLinkNode(DynamicTreeNodeCollection collection, DynamicTreeNode link)
            : base(collection, null)
        {
            Link = link;
            nodesCache = new DynamicTreeNodeCollection(View, this);
        }

        public override DynamicTreeNode DataNode { get { return Link.DataNode; } }

        public static readonly bool AllowMetaLinks = true;

        public override bool WordWrap
        {
            get
            {
                return Link.WordWrap;
            }
        }

        public override int WordWrapLines
        {
            get
            {
                return Link.WordWrapLines;
            }
        }

        public override bool HighlightWhenSelected(DynamicTreeNode n)
        {
            return Link.HighlightWhenSelected(n);
        }

        private DynamicTreeNode link;
        public DynamicTreeNode Link
        {
            get
            {
                return link;
            }

            set
            {
                if (link != null)
                    link.Nodes.CollectionChanged -= linkNodesChanged;
                if (value == null)
                    throw new ArgumentException("Link should never be null.");
                link = value;
                while (!AllowMetaLinks && link is DynamicTreeLinkNode) //yo dog
                    link = (link as DynamicTreeLinkNode).Link;
                link.Nodes.CollectionChanged += linkNodesChanged;
            }
        }

        private void linkNodesChanged(DynamicTreeNodeCollection linkNodes)
        {
            if (Link.Nodes != linkNodes)
                return;
            linkNodesCache = null;
        }

        private DynamicTreeNodeCollection nodesCache;
        private List<DynamicTreeNode> linkNodesCache = null;
        public override DynamicTreeNodeCollection Nodes
        {
            get
            {
                if (!Expanded || Link == null)
                    return nodesCache;
                if (linkNodesCache == null)
                {
                    linkNodesCache = new List<DynamicTreeNode>(Link.Nodes);
                    nodesCache = new DynamicTreeNodeCollection(View, this);

                    foreach (DynamicTreeNode n in linkNodesCache)
                        nodesCache.Add(new DynamicTreeLinkNode(nodesCache, n));
                }
                return nodesCache;
            }
        }

        public override string Text
        {
            get
            {
                return Link.Text;
            }
        }

        public override Font Font
        {
            get
            {
                return Link.Font;
            }
        }

        public override bool Visible
        {
            get
            {
                return base.Visible && Link.Visible;
            }
        }
    }

    public interface IDynamicTreeNodeDataProxy
    {
        event Action UpdateNode;
        event Action RemoveNode;

        IListChangeEvents<IDynamicTreeNodeDataProxy> Child { get; }

        string Text { get; }
        bool WordWrap { get; }
        int WordWrapLines { get; }
        bool ExpandText { get; }
        bool Visible { get; }
        Font Font { get; }
        Bitmap Icon { get; }
        IEnumerable<IDynamicTreeNodeDataProxy> HighlightWhenSelected { get; }
        //Creating a node around this proxy will populate it with the contents of Children -- the contents will NOT be synchronized, use AddedChild/RemovedChild for that
        IEnumerable<IDynamicTreeNodeDataProxy> Children { get; }
    }

    public class DynamicTreeNodeTextProxy : IDynamicTreeNodeDataProxy
    {
        public event Action UpdateNode = () => { };
        public event Action RemoveNode = () => { };


        public IListChangeEvents<IDynamicTreeNodeDataProxy> Child { get { return null; } }

        public DynamicTreeNodeTextProxy(string text)
        {
            Text = text;
            HighlightWhenSelectedNodes = new List<IDynamicTreeNodeDataProxy>();
        }

        private string text;
        public virtual string Text
        {
            get { return text; }
            set { text = value; UpdateNode(); }
        }

        private bool wordWrap = true;
        public virtual bool WordWrap
        {
            get { return wordWrap; }
            set { wordWrap = value; UpdateNode(); }
        }

        private int wordWrapLines = 3;
        public virtual int WordWrapLines
        {
            get { return wordWrapLines; }
            set { wordWrapLines = value; UpdateNode(); }
        }

        private bool expandText = false;
        public virtual bool ExpandText
        {
            get { return expandText; }
            set { expandText = value; }
        }

        private bool visible = true;
        public virtual bool Visible
        {
            get { return visible; }
            set { visible = value; UpdateNode(); }
        }

        private Font font = null;
        public virtual Font Font
        {
            get { return font; }
            set { font = value; UpdateNode(); }
        }

        public Bitmap Icon
        {
            get;
            set;
        }

        public List<IDynamicTreeNodeDataProxy> HighlightWhenSelectedNodes { get; set; }

        public virtual IEnumerable<IDynamicTreeNodeDataProxy> HighlightWhenSelected
        {
            get { foreach (IDynamicTreeNodeDataProxy p in HighlightWhenSelectedNodes) yield return p; }
        }

        public IEnumerable<IDynamicTreeNodeDataProxy> Children
        {
            get { yield break; }
        }
    }

    public class DynamicTreeNodeMouseEventArgs : MouseEventArgs
    {
        private DynamicTreeNode node;

        public DynamicTreeNodeMouseEventArgs(DynamicTreeNode n, MouseEventArgs e)
            : base(e.Button, e.Clicks, e.X, e.Y, e.Delta)
        {
            node = n;
        }

        public DynamicTreeNode Node { get { return node; } }
    }

    public delegate void DynamicTreeNodeMouseEventHandler(object sender, DynamicTreeNodeMouseEventArgs e);
}
