using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamicTreeView
{
    public class DynamicTreeNodeCollection :
        IList<DynamicTreeNode>, IEnumerable<DynamicTreeNode>
    {
        private List<DynamicTreeNode> nodes = new List<DynamicTreeNode>();
        private List<DynamicTreeNode> visibleNodesCache;
        private List<DynamicTreeNode> allNodesCache;

        private DynamicTreeView view;
        public DynamicTreeView View { get { return view; } }

        private DynamicTreeNode node;
        public DynamicTreeNode Node { get { return node; } }

        public DynamicTreeNodeCollection(DynamicTreeView view, DynamicTreeNode node = null)
            : base()
        {
            this.view = view;
            this.node = node;
        }

        public void Refresh()
        {
            visibleNodesCache = null;
            allNodesCache = null;
            if (Node != null && Node.ParentNodes != null)
                Node.ParentNodes.Refresh();
            view.Invalidate();
        }

        public int IndexOf(DynamicTreeNode item)
        {
            return nodes.IndexOf(item);
        }

        public void Insert(int index, DynamicTreeNode item)
        {
            item.ParentNodes = this;
            nodes.Insert(index, item);
            OnCollectionChanged();
        }

        public void RemoveAt(int index)
        {
            nodes.RemoveAt(index);
            OnCollectionChanged();
        }

        public DynamicTreeNode this[int index]
        {
            get
            {
                return nodes[index];
            }
            set
            {
                value.parentNodes = this;
                nodes[index] = value;
                OnCollectionChanged();
            }
        }

        public void Add(DynamicTreeNode item)
        {
            item.ParentNodes = this;
            nodes.Add(item);
            OnCollectionChanged();
        }

        public DynamicTreeNode Add(IDynamicTreeNodeDataProxy data)
        {
            DynamicTreeNode n = new DynamicTreeNode(this, data);
            Add(n);
            return n;
        }

        public DynamicTreeNode Add(string value)
        {
            var data = new DynamicTreeNodeTextProxy(value);
            return Add(data);
        }

        public DynamicTreeLinkNode AddLink(DynamicTreeNode linkTo)
        {
            DynamicTreeLinkNode link = new DynamicTreeLinkNode(this, linkTo);
            Add(link);
            return link;
        }

        public void Clear()
        {
            nodes.Clear();
            OnCollectionChanged();
        }

        public bool Contains(DynamicTreeNode item)
        {
            return nodes.Contains(item);
        }

        public void CopyTo(DynamicTreeNode[] array, int arrayIndex)
        {
            nodes.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get
            {
                DynamicTreeLinkNode link = Node as DynamicTreeLinkNode;
                if (link != null && link.Link != null)
                    return link.Link.Nodes.Count;
                return nodes.Count;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(DynamicTreeNode item)
        {
            bool success = nodes.Remove(item);
            if (success)
                Refresh();
            return success;
        }

        public int RemoveAll(Predicate<DynamicTreeNode> predicate)
        {
            List<DynamicTreeNode> list = this.Where(n => predicate(n)).ToList();
            foreach (DynamicTreeNode n in list)
                Remove(n);
            return list.Count;
        }

        public IEnumerator<DynamicTreeNode> GetEnumerator()
        {
            return nodes.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return nodes.GetEnumerator();
        }

        IEnumerator<DynamicTreeNode> IEnumerable<DynamicTreeNode>.GetEnumerator()
        {
            return nodes.GetEnumerator();
        }

        //list of all nodes as a recursive tree parse
        public List<DynamicTreeNode> AllNodes
        {
            get
            {
                if (allNodesCache != null)
                    return allNodesCache;

                List<DynamicTreeNode> l = new List<DynamicTreeNode>();
                foreach (DynamicTreeNode n in this)
                {
                    l.Add(n);
                    foreach (DynamicTreeNode n2 in n.Nodes.AllNodes)
                        l.Add(n2);
                }
                allNodesCache = l;
                return l;
            }
        }

        //same thing, but with visible nodes (subnodes of an expanded node) only
        public List<DynamicTreeNode> VisibleNodes
        {
            get
            {
                if (visibleNodesCache != null)
                    return visibleNodesCache;

                List<DynamicTreeNode> l = new List<DynamicTreeNode>();
                foreach (DynamicTreeNode n in this)
                {
                    if (n.Visible)
                        l.Add(n);
                    l.AddRange(n.Nodes.VisibleNodes);
                }
                visibleNodesCache = l;
                return l;
            }
        }

        public event DynamicTreeNodeCollectionChangeHandler CollectionChanged;

        public virtual void OnCollectionChanged()
        {
            if (CollectionChanged != null)
                CollectionChanged(this);
            Refresh();
        }
    }

    public delegate void DynamicTreeNodeCollectionChangeHandler(DynamicTreeNodeCollection collection);
}
