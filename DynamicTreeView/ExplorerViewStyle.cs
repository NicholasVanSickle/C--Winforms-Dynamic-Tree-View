using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms.VisualStyles;

namespace DynamicTreeView
{
    public class ExplorerViewStyle
    {
        private static Dictionary<int, Dictionary<int, VisualStyleRenderer>> renderers = new Dictionary<int, Dictionary<int, VisualStyleRenderer>>();

        private static VisualStyleRenderer getRenderer(int x, int y)
        {
            Dictionary<int, VisualStyleRenderer> subDict;
            try
            {
                subDict = renderers[x];
            }
            catch (KeyNotFoundException)
            {
                subDict = new Dictionary<int, VisualStyleRenderer>();
                renderers[x] = subDict;
            }

            VisualStyleRenderer renderer;
            try
            {
                renderer = subDict[y];
            }
            catch (KeyNotFoundException)
            {
                renderer = new VisualStyleRenderer("Explorer::TreeView", x, y);
                subDict[y] = renderer;
            }

            return renderer;
        }

        public static VisualStyleRenderer Opened { get { return getRenderer(2, 2); } }
        public static VisualStyleRenderer Closed { get { return getRenderer(2, 1); } }
        public static VisualStyleRenderer OpenedHover { get { return getRenderer(4, 2); } }
        public static VisualStyleRenderer ClosedHover { get { return getRenderer(4, 1); } }
        public static VisualStyleRenderer ItemHover { get { return getRenderer(1, 2); } }
        public static VisualStyleRenderer ItemSelect { get { return getRenderer(1, 3); } }
        public static VisualStyleRenderer ItemSelectNoFocus { get { return getRenderer(1, 5); } }
    }

}
