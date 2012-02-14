using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace DynamicTreeView
{
    public delegate T CacheFunction<U, T>(U key);

    internal static class DictionaryExtension
    {
        public static T CachedValue<U, T>(this Dictionary<U, T> dict, U key, CacheFunction<U, T> f)
        {
            if (dict.ContainsKey(key))
                return dict[key];
            return dict[key] = f(key);
        }

        public static Dictionary<X, Y> Subdictionary<T, X, Y>(this Dictionary<T, Dictionary<X, Y>> dict, T foo)
        {
            if (dict.ContainsKey(foo))
                return dict[foo];
            return dict[foo] = new Dictionary<X, Y>();
        }
    }

    public class NodeTextRenderer
    {
        public static readonly char FormatChar = '\f'; //I repurposed the form feed - if anyone has an issue 

        private struct FontStyleCacheKey
        {
            public FontStyle style;
            public Font font;
            public float height;

            public FontStyleCacheKey(FontStyle style, Font font, float height)
            {
                this.style = style;
                this.font = font;
                this.height = height;
            }
        }

        private class FontStyleCacheKeyComparer : IEqualityComparer<FontStyleCacheKey>
        {
            public bool Equals(FontStyleCacheKey x, FontStyleCacheKey y)
            {
                return x.style == y.style && x.font == y.font && x.height == y.height;
            }

            public int GetHashCode(FontStyleCacheKey obj)
            {
                return obj.font.GetHashCode() + obj.style.GetHashCode() + obj.height.GetHashCode();
            }
        }

        private static Dictionary<FontStyleCacheKey, Font> fontStyleCache =
            new Dictionary<FontStyleCacheKey, Font>(new FontStyleCacheKeyComparer());

        public static Font FontStyleCache(Font baseFont, FontStyle style, float height)
        {
            FontStyleCacheKey key = new FontStyleCacheKey(style, baseFont, height);
            CacheFunction<FontStyleCacheKey, Font> cache = x =>
            {
                Font f = baseFont;
                if (x.height != CachedHeight(f))
                    f = new Font(f.FontFamily, x.height);
                if (x.style != f.Style)
                    f = new Font(f, x.style);
                return f;
            };
            return fontStyleCache.CachedValue(key, cache);
        }

        private static Dictionary<Font, int> fontHeightCache = new Dictionary<Font, int>();

        public static int CachedHeight(Font font)
        {
            return fontHeightCache.CachedValue(font, x => x.Height);
        }

        public class RichTextData
        {
            private Color? color = null;
            public Color? Color { get { return color; } }

            private FontStyle? style = null;
            public FontStyle? Style { get { return style; } }

            private float? height = null;
            public float? Height { get { return height; } }

            public Font BaseFont { get; set; }
            public Font Font
            {
                get
                {
                    if (style == null && height == null)
                        return BaseFont;
                    return FontStyleCache(BaseFont, style ?? FontStyle.Regular, height ?? CachedHeight(BaseFont));
                }
            }

            public RichTextData(Font baseFont)
            {
                BaseFont = baseFont;
            }

            public RichTextData(RichTextData copy)
            {
                if (copy != null)
                {
                    color = copy.Color;
                    style = copy.Style;
                    height = copy.Height;
                    BaseFont = copy.BaseFont;
                }
            }

            private bool finished = false;
            public bool NeedsInput { get { return !finished; } }

            private char? type = null;
            private string data = "";
            private int expectedLength;

            private void ToggleStyle(FontStyle s)
            {
                if (style == null)
                    style = s;
                else
                {
                    if (style.Value.HasFlag(s))
                        style = style ^ s;
                    else
                        style = style.Value | s;
                }
            }

            public bool Process(char c)
            {
                if (type == null)
                {
                    type = char.ToLower(c);
                    switch (type)
                    {
                        case 'c':
                            if (color != null)
                            {
                                color = null;
                                finished = true;
                                return true;
                            }
                            expectedLength = 6;
                            finished = false;
                            return true;
                        case 'b':
                            finished = true;
                            ToggleStyle(FontStyle.Bold);
                            return true;
                        case 'i':
                            finished = true;
                            ToggleStyle(FontStyle.Italic);
                            return true;
                        case 'u':
                            finished = true;
                            ToggleStyle(FontStyle.Underline);
                            return true;
                        case 's':
                            finished = true;
                            ToggleStyle(FontStyle.Strikeout);
                            return true;
                        case 'h':
                            if(height != null)
                            {
                                height = null;
                                return finished = true;
                            }
                            finished = false;
                            expectedLength = 1;
                            return true;
                        case 'r':
                            finished = true;
                            style = null;
                            height = null;
                            color = null;
                            return true;
                        default:
                            throw new ArgumentException("Unrecognized format code: " + type);
                    }
                }
                else if (expectedLength > 0)
                {
                    data += c;
                    expectedLength--;
                }

                if (!finished && expectedLength == 0)
                {
                    switch (type)
                    {
                        case 'c':
                            int r = int.Parse(data.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                            int g = int.Parse(data.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                            int b = int.Parse(data.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                            color = System.Drawing.Color.FromArgb(r, g, b);
                            finished = true;
                            return true;
                        case 'h':
                            if (!char.IsNumber(c) && c != '.')
                            {
                                data = data.Substring(0, data.Length - 1);
                                height = float.Parse(data);
                                finished = true;
                                return c == '\\';
                            }
                            expectedLength = 1;
                            return true;
                    }
                }

                return !finished;
            }
        }

        public class TextChunk
        {
            public string text;
            public RichTextData data;
            public int? width = null;
            public bool overflow = false; //set to true when this would have had a Measure exceeding width, requiring text to be cut down

            public TextChunk(string text, RichTextData data)
            {
                this.text = text;
                if (data != null)
                    this.data = new RichTextData(data);
                else
                    data = null;
            }
        }

        private string text;
        private Font font;
        private TextFormatFlags flags;
        private int width;
        private int lines;

        public string Text { get { return text; } set { text = value; drawCacheStale = true; componentsCache = null; } }
        public Font Font { get { return font; } set { font = value; drawCacheStale = true; componentsCache = null; } }
        public TextFormatFlags Flags { get { return flags; } set { flags = value; drawCacheStale = true; componentsCache = null; } }
        public int Width { get { return width; } set { width = value; drawCacheStale = true; componentsCache = null; } }
        public int Lines { get { return lines; } set { lines = value; drawCacheStale = true; } }

        public NodeTextRenderer(Font font, TextFormatFlags flags, string text, int width, int lines)
        {
            this.font = font;
            this.text = text;
            this.flags = flags;
            this.width = width;
            this.lines = lines;
        }

        private struct MeasureCacheString
        {
            public string text;
            public Font font;
            public TextFormatFlags flags;

            public MeasureCacheString(string text, Font font, TextFormatFlags flags)
            {
                this.text = text;
                this.font = font;
                this.flags = flags;
            }
        }

        //probably not actually necessary, but I like the peace of mind
        private class MeasureCacheStringComparer : IEqualityComparer<MeasureCacheString>
        {
            public bool Equals(MeasureCacheString x, MeasureCacheString y)
            {
                return x.font.Equals(y.font) && x.flags == y.flags && (x.text == y.text || x.text != null && x.text.Equals(y.text));
            }

            public int GetHashCode(MeasureCacheString obj)
            {
                return (obj.text != null ? obj.text.GetHashCode() : 0) + obj.flags.GetHashCode() + obj.font.GetHashCode();
            }
        }

        private static Dictionary<MeasureCacheString, int> measureCache = new Dictionary<MeasureCacheString, int>(new MeasureCacheStringComparer());

        //ridiculously expensive, definitely the major optimization vector here
        public static int Measure(string s, Font font, TextFormatFlags flags)
        {
            MeasureCacheString key = new MeasureCacheString(s, font, flags);
            return measureCache.CachedValue(key, x => TextRenderer.MeasureText(s, font, new Size(), flags).Width);
        }

        public int PaddingWidth(Font font, TextFormatFlags flags)
        {
            int a = Measure(" ", font, flags);
            int b = Measure("  ", font, flags);
            return a - (b - a);
        }

        public int Measure(TextChunk chunk)
        {
            return chunk.text != "" ? Measure(chunk.text, chunk.data.Font, flags) - PaddingWidth(chunk.data.Font, flags) : 0;
        }

        public int Measure(List<TextChunk> chunks)
        {
            int padSize = 0;
            int total = 0;
            foreach (TextChunk c in chunks)
            {
                padSize = Math.Max(padSize, PaddingWidth(c.data.Font, flags));
                total += Measure(c);
            }
            return total + padSize;
        }

        public Size Dimensions()
        {
            int l = Math.Min(lines, Components.Count);
            int height = 0;
            foreach (List<TextChunk> line in Components)
            {
                int lineHeight = 0;
                foreach (TextChunk c in line)
                    lineHeight = Math.Max(CachedHeight(c.data.Font), lineHeight);
                height += lineHeight;
                if (--l == 0)
                    break;
            }
            return new Size(usedWidth, height);
        }

        private List<List<TextChunk>> drawCache;
        private bool drawCacheStale = true;
        public void Draw(Graphics g, Point p, Color defaultColor)
        {
            if (drawCacheStale)
            {
                drawCache = TrimmedComponents(lines);
                drawCacheStale = false;
            }

            g.SmoothingMode = SmoothingMode.HighQuality;
            int y = 0;
            foreach (List<TextChunk> line in drawCache)
            {
                int x = 0;
                int height = 0;
                foreach (TextChunk c in line)
                {
                    Point np = new Point(p.X + x, p.Y + y);
                    Font f = c.data.Font;
                    height = Math.Max(CachedHeight(f), height);
                    Color color = c.data.Color != null ? c.data.Color.Value : defaultColor;
                    TextRenderer.DrawText(g, c.text, f, np, color, flags);
                    x += Measure(c);
                }
                y += height;
            }
        }

        public void Draw(Graphics g, Point p)
        {
            Draw(g, p, Color.Black);
        }

        public int FitToLine(TextChunk c)
        {
            string s = c.text;
            for (int i = 1; i <= s.Length; i++)
            {
                if (Measure(s.Substring(0, i), c.data.Font, flags) > width)
                    return i - 1;
            }
            return s.Length;
        }

        private int usedWidth = 0;

        private void CalculateComponents()
        {
            componentsCache = new List<List<TextChunk>>();
            if (text == "")
                return;

            TextChunk lastPart = null;
            foreach (string s in text.Split('\n'))
            {
                List<TextChunk> line = new List<TextChunk>();

                TextChunk part = new TextChunk("", null);
                if (lastPart != null)
                    part.data = new RichTextData(lastPart.data);
                else
                    part.data = new RichTextData(font);
                lastPart = part;

                bool whitespace = false;

                //while the Measure of s will be wrong if there are formatting flags
                //it is guaranteed to be <= the Measure with them stripped
                //optimizing away the wrap check would require more processing
                //todo: determine if doing a rich text pass and a word wrap pass
                //with a binary search or something wins of performance
                bool wrap = Measure(s, font, flags) > width;
                bool parsingStyle = false;
                foreach (char c in (s + "\0").ToCharArray())
                {
                    parsingStyle = parsingStyle && part.data.NeedsInput && part.data.Process(c);
                    if (parsingStyle)
                        continue;

                    if (c == FormatChar)
                    {
                        parsingStyle = true;
                    }

                    if (parsingStyle || c == '\0' || part.text != "" && whitespace != char.IsWhiteSpace(c))
                    {
                        line.Add(part);
                        if (wrap && Measure(line) > width)
                        {
                            usedWidth = width;
                            if (!whitespace)
                            {
                                line.RemoveAt(line.Count - 1);
                                if (line.Count == 0)
                                {
                                    int len = FitToLine(part);
                                    TextChunk newPart = new TextChunk(part.text.Substring(0, len), part.data);
                                    newPart.overflow = true;
                                    line.Add(newPart);
                                    part = new TextChunk(part.text.Substring(len), part.data);
                                }
                            }

                            componentsCache.Add(line);
                            line = new List<TextChunk>();
                            if (!whitespace)
                            {
                                line.Add(part);
                            }
                        }
                        lastPart = part;
                        part = new TextChunk("", lastPart.data);
                    }

                    if (parsingStyle)
                        continue;

                    whitespace = char.IsWhiteSpace(c);
                    if (c == '\t')
                        part.text += "    ";
                    else
                        part.text += c;
                }
                if (line.Count > 0)
                {
                    if (Measure(line) > usedWidth)
                        usedWidth = Measure(line);
                    componentsCache.Add(line);
                }
            }

            for (int i = componentsCache.Count - 1; i >= 0; i--)
            {
                bool remove = true;
                foreach (TextChunk c in componentsCache[i])
                {
                    if (!string.IsNullOrWhiteSpace(c.text))
                    {
                        remove = false;
                        break;
                    }
                }
                if (!remove)
                    break;
                componentsCache.RemoveAt(i);
            }
        }

        List<List<TextChunk>> componentsCache;
        public List<List<TextChunk>> Components
        {
            get
            {
                if (componentsCache == null)
                    CalculateComponents();
                return componentsCache;
            }
        }

        private List<List<TextChunk>> TrimmedComponents(int lines, string lastWord = "...")
        {
            if (lines == -1 || Components.Count <= lines)
                return Components;
            List<List<TextChunk>> components = new List<List<TextChunk>>();
            if (Components.Count < lines)
                return Components;
            int i = 0;
            foreach (List<TextChunk> l in Components)
            {
                components.Add(l);
                if (++i == lines)
                    break;
            }
            List<TextChunk> lastLine = new List<TextChunk>(components.Last());

            //don't leave us with just a "..." -- hooray for edge cases
            if (lines == 1 && lastLine.Count == 1)
            {
                TextChunk lastChunk = lastLine[0];
                if (lastChunk.overflow)
                {
                    if (lastChunk.text.Length > lastWord.Length + 1)
                    {
                        lastChunk.text = lastChunk.text.Substring(0, lastChunk.text.Length - lastWord.Length - 1) + lastWord;
                        lastLine[0] = lastChunk;
                    }
                }
                return components;
            }

            if (lastLine != null && lastLine.Count > 0)
            {
                TextChunk c = lastLine.Last();
                if (string.IsNullOrWhiteSpace(c.text) && lastLine.Count > 1)
                {
                    lastLine.RemoveAt(lastLine.Count - 1);
                    c = lastLine.Last();
                }

                if (Measure(c) < Measure(lastWord, c.data.Font, flags))
                {
                    if (lastLine.Count > 1)
                        lastLine.RemoveAt(lastLine.Count - 1);
                    if (lastLine.Count > 1 && string.IsNullOrWhiteSpace(lastLine.Last().text))
                        lastLine.RemoveAt(lastLine.Count - 1);
                    c = lastLine.Last();
                }

                lastLine.RemoveAt(lastLine.Count - 1);
                c = new TextChunk(lastWord, c.data);
                lastLine.Add(c);
            }
            components[components.Count - 1] = lastLine;
            return components;
        }
    }
}
