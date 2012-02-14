using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace DynamicTreeView
{
    //attempts to mimic Explorer::TreeView's styles with a slight feature expansion (arbitrary selection color primarily)
    static class FakeNativeTreeStyleRenderer
    {
        //132, 172, 221 W7
        //154, 223, 254 Vista
        //defaulting to W7 for now
        public static readonly Color SelectionColor =
            Color.FromArgb(132, 172, 221);
        public static readonly Color NoFocusSelectionColor =
            Color.FromArgb(218, 218, 218);
        public static readonly Color ArrowHighlightColor =
            Color.FromArgb(28, 196, 247);
        public static readonly Color ArrowGrayColor =
            Color.FromArgb(166, 166, 166);
        public static readonly Color ArrowBlackColor =
            Color.FromArgb(89, 89, 89);

        public static Color AlphaBlend(int newAlpha, Color other)
        {
            return Color.FromArgb(newAlpha * other.A / 255, other);
        }

        public static GraphicsPath GetOpenedArrowPath(Rectangle r)
        {
            GraphicsPath gp = new GraphicsPath();

            r = Rectangle.Inflate(r, -5, -5);
            r.Y -= 1;

            gp.AddLine(r.Left, r.Bottom, r.Right, r.Bottom);
            gp.AddLine(r.Right, r.Bottom, r.Right, r.Top);
            gp.AddLine(r.Right, r.Top, r.Left, r.Bottom);

            return gp;
        }

        public static GraphicsPath GetClosedArrowPath(Rectangle r)
        {
            GraphicsPath gp = new GraphicsPath();

            r = Rectangle.Inflate(r, -5, -4);
            r.X += r.Width / 3;
            r.Width -= 2;

            gp.AddLine(r.Left, r.Top, r.Right, r.Y + r.Height / 2);
            gp.AddLine(r.Right, r.Y + r.Height / 2, r.Left, r.Bottom);
            gp.AddLine(r.Left, r.Bottom, r.Left, r.Top);

            return gp;
        }

        //this isn't per pixel accurate but it is damn close even when comparing side-by-side
        public static void DrawArrow(Graphics g, Rectangle r, bool expanded, bool highlight)
        {
            GraphicsPath gp = expanded ? GetOpenedArrowPath(r) : GetClosedArrowPath(r);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float LineThickness = 1.0f;

            //I'm not sure what would best simulate the highlight "glow" -- it may well be an ellipse, but this is the closest
            //and cleanest I've gotten it visually
            if (highlight)
            {
                if (expanded)
                {
                    r = Rectangle.Inflate(r, -1, -1);
                    r.Width += 1;
                    r.Height += 1;
                    gp.Dispose();
                    gp = GetOpenedArrowPath(r);

                    Color c = ArrowHighlightColor;
                    Brush brush = new LinearGradientBrush(r, AlphaBlend(20, c),
                        AlphaBlend(120, c), LinearGradientMode.Vertical);

                    r = Rectangle.Inflate(r, 2, 2);
                    r.X -= 1;
                    r.Y -= 1;
                    GraphicsPath gp2 = GetOpenedArrowPath(r);
                    g.FillPath(brush, gp2);
                    g.DrawPath(new Pen(c, LineThickness), gp);

                    gp2.Dispose();
                }
                else
                {
                    Color c = ArrowHighlightColor;
                    Brush brush = new LinearGradientBrush(r, AlphaBlend(20, c),
                        AlphaBlend(120, c), LinearGradientMode.Vertical);

                    r = Rectangle.Inflate(r, 1, 2);
                    GraphicsPath gp2 = GetClosedArrowPath(r);
                    g.FillPath(brush, gp2);
                    g.DrawPath(new Pen(c, LineThickness), gp);

                    gp2.Dispose();
                }
            }
            else
            {
                if (expanded)
                {
                    r.Width -= 1;
                    r.Height -= 1;
                    r.X += 1;
                    r.Y += 1;
                    GraphicsPath gp2 = GetOpenedArrowPath(r);
                    g.FillPath(new SolidBrush(ArrowBlackColor), gp);
                    g.SmoothingMode = SmoothingMode.None;
                    g.DrawPath(new Pen(Color.Black, 1.0f), gp2);
                    gp2.Dispose();
                }
                else
                {
                    g.DrawPath(new Pen(ArrowGrayColor, LineThickness), gp);
                }
            }

            gp.Dispose();
        }

        public static void DrawSelection(Graphics g, Rectangle r, Color c)
        {
            //we're attempting to closely emulate the Explorer::TreeView selection style
            //it renders to a rectangle 1 less wide and high
            r.Width -= 1;
            r.Height -= 1;

            float rounding = 2.0f;
            Brush brush = new LinearGradientBrush(r, AlphaBlend(20, c),
                AlphaBlend(120, c), LinearGradientMode.Vertical);
            g.FillRoundedRectangle(brush, r, rounding);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            brush = new SolidBrush(Color.FromArgb(127 * c.A / 255, Color.White));
            g.DrawRoundedRectangle(new Pen(brush, 1.0f), Rectangle.Inflate(r, -1, -1), rounding);
            brush = new SolidBrush(AlphaBlend(255, c));
            g.DrawRoundedRectangle(new Pen(brush, 1.0f), r, rounding);
        }

        //adapted from http://www.geekpedia.com/code112_Draw-Rounded-Corner-Rectangles-Using-Csharp.html
        //credit: Andrew Pociu
        public static GraphicsPath GetRoundedRectanglePath(float x, float y, float width, float height, float radius)
        {
            GraphicsPath gp = new GraphicsPath();

            gp.AddLine(x + radius, y, x + width - (radius * 2), y); // Line
            gp.AddArc(x + width - (radius * 2), y, radius * 2, radius * 2, 270, 90); // Corner
            gp.AddLine(x + width, y + radius, x + width, y + height - (radius * 2)); // Line
            gp.AddArc(x + width - (radius * 2), y + height - (radius * 2), radius * 2, radius * 2, 0, 90); // Corner
            gp.AddLine(x + width - (radius * 2), y + height, x + radius, y + height); // Line
            gp.AddArc(x, y + height - (radius * 2), radius * 2, radius * 2, 90, 90); // Corner
            gp.AddLine(x, y + height - (radius * 2), x, y + radius); // Line
            gp.AddArc(x, y, radius * 2, radius * 2, 180, 90); // Corner
            gp.CloseFigure();

            return gp;
        }

        public static void FillRoundedRectangle(this Graphics g, Brush b, float x, float y, float width, float height, float radius)
        {
            GraphicsPath gp = GetRoundedRectanglePath(x, y, width, height, radius);

            g.FillPath(b, gp);
            gp.Dispose();
        }

        public static void FillRoundedRectangle(this Graphics g, Brush b, Rectangle r, float radius)
        {
            FillRoundedRectangle(g, b, r.X, r.Y, r.Width, r.Height, radius);
        }

        public static void DrawRoundedRectangle(this Graphics g, Pen p, float x, float y, float width, float height, float radius)
        {
            GraphicsPath gp = GetRoundedRectanglePath(x, y, width, height, radius);

            g.DrawPath(p, gp);
            gp.Dispose();
        }

        public static void DrawRoundedRectangle(this Graphics g, Pen p, Rectangle r, float radius)
        {
            DrawRoundedRectangle(g, p, r.X, r.Y, r.Width, r.Height, radius);
        }
    }
}
