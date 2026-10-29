using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Gdi = System.Drawing;
using GdiImaging = System.Drawing.Imaging;
using static System.FormattableString;

namespace PlaceKit.IconRenderer
{
    // The ribbon icons as vector shapes, drawn for a screen scale in the colours of an Office theme. Program renders
    // them into the image strips the add-in ships. Like Office's own icons they use a 32 x 32 grid with 1-unit lines on half units.
    public sealed class RibbonIcons
    {
        const double GridSize = 32;

        // The space a shape in front clears around itself in the shapes behind it.
        const double Gap = 1;

        static readonly Icon CreateIcon = new Icon(
            new[] { Braces() },
            new[] { Shape.Solid(Path("M24,20 H26 V24 H30 V26 H26 V30 H24 V26 H20 V24 H24 Z"), Ink.Accent) });

        static readonly Icon FillIcon = new Icon(
            new[] { Braces() },
            new[]
            {
                Shape.Solid(Path("M0,0 L4.5,-2.5 V2.5 Z", -45, 12.5, 29.5), Ink.Accent),
                Shape.Solid(Path("M5.5,-2.5 H15 A2.5,2.5 0 0 1 15,2.5 H5.5 Z", -45, 12.5, 29.5), Ink.Accent),
            });

        static readonly Icon FinalizeIcon = new Icon(
            new[] { Braces() },
            new[] { Shape.Outline(Path("M20.5,25 L24,28.5 L30,22"), Ink.Accent, 2) });

        static readonly Icon GuideIcon = new Icon(
            new[] { Shape.Outline(Path("M16,9.5 C13,7.5 8.5,7 3.5,7.5 V25.5 C8.5,25 13,25.5 16,27.5 C19,25.5 23.5,25 28.5,25.5 V7.5 C23.5,7 19,7.5 16,9.5 Z M16,9.5 V27.5"), Ink.Line) },
            new[] { Shape.Solid(Path("M20,4 H24 V13 L22,11.5 L20,13 Z"), Ink.Accent) });

        static readonly Icon SettingsIcon = new Icon(
            new[]
            {
                Shape.Outline(Path(Gear(16, 16, 8, 12.5, 9.5, 4, 5.2)), Ink.Line),
                Shape.Outline(Path("M12,16 A4,4 0 1 1 20,16 A4,4 0 1 1 12,16 Z"), Ink.Line),
            });

        static readonly Icon AboutIcon = new Icon(
            new[]
            {
                Shape.Solid(Path("M10,4 H22 A6,6 0 0 1 28,10 V22 A6,6 0 0 1 22,28 H10 A6,6 0 0 1 4,22 V10 A6,6 0 0 1 10,4 Z"), Ink.Tile),
                Shape.Outline(Path(Brace(9, 12, 14, 9, 23)), Ink.OnTile, 2),
                Shape.Outline(Path(Brace(23, 20, 18, 9, 23)), Ink.OnTile, 2),
            });

        static readonly Icon HireIcon = new Icon(
            new[]
            {
                Shape.Solid(Path("M26,3 Q26.4,5.6 29,6 Q26.4,6.4 26,9 Q25.6,6.4 23,6 Q25.6,5.6 26,3 Z"), Ink.Line),
                Shape.Solid(Path("M6,21.5 Q6.3,23.7 8.5,24 Q6.3,24.3 6,26.5 Q5.7,24.3 3.5,24 Q5.7,23.7 6,21.5 Z"), Ink.Line),
            },
            new[] { Shape.Rounded(Path("M19,2.5 L8,18 H15.5 L13,29.5 L24,14 H16.5 Z"), Ink.Promo) });

        readonly double _scale;
        readonly Palette _palette;

        // scale: the screen scale, 1.5 at 150 %.
        public RibbonIcons(double scale, bool darkTheme)
        {
            _scale = scale;
            _palette = darkTheme ? Palette.Dark : Palette.Light;
        }

        public Gdi.Bitmap Render(RibbonIcon icon) => Render(IconOf(icon));

        Gdi.Bitmap Render(Icon icon)
        {
            int size = (int)Math.Round(GridSize * _scale);
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext context = visual.RenderOpen())
            {
                context.PushTransform(new ScaleTransform(size / GridSize, size / GridSize));
                icon.Draw(context, _palette);
            }

            RenderTargetBitmap target = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            target.Render(visual);
            return ToBitmap(target);
        }

        static Icon IconOf(RibbonIcon icon)
        {
            switch (icon)
            {
                case RibbonIcon.Create:
                    return CreateIcon;
                case RibbonIcon.Fill:
                    return FillIcon;
                case RibbonIcon.Finalize:
                    return FinalizeIcon;
                case RibbonIcon.Guide:
                    return GuideIcon;
                case RibbonIcon.Settings:
                    return SettingsIcon;
                case RibbonIcon.About:
                    return AboutIcon;
                case RibbonIcon.Hire:
                    return HireIcon;
                default:
                    throw new ArgumentOutOfRangeException(nameof(icon));
            }
        }

        static Gdi.Bitmap ToBitmap(BitmapSource source)
        {
            FormatConvertedBitmap pixels = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            Gdi.Bitmap bitmap = new Gdi.Bitmap(pixels.PixelWidth, pixels.PixelHeight, GdiImaging.PixelFormat.Format32bppArgb);
            GdiImaging.BitmapData data = bitmap.LockBits(new Gdi.Rectangle(0, 0, bitmap.Width, bitmap.Height), GdiImaging.ImageLockMode.WriteOnly, bitmap.PixelFormat);
            try
            {
                pixels.CopyPixels(Int32Rect.Empty, data.Scan0, data.Stride * data.Height, data.Stride);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }

            return bitmap;
        }

        // The { } of Create, Fill and Finalize, in the top left corner so the action fits below on the right.
        static Shape Braces() => Shape.Outline(Path(Brace(2.5, 5.5, 8.5, 2.5, 22.5) + " " + Brace(22.5, 19.5, 16.5, 2.5, 22.5)), Ink.Line);

        // Path markup in grid units. Parts drawn around their own origin are rotated by angle degrees, then moved to x, y.
        static Geometry Path(string data, double angle = 0, double x = 0, double y = 0)
        {
            Geometry geometry = Geometry.Parse(data).Clone();
            Matrix matrix = Matrix.Identity;
            matrix.Rotate(angle);
            matrix.Translate(x, y);
            geometry.Transform = new MatrixTransform(matrix);
            geometry.Freeze();
            return geometry;
        }

        // A curly brace: tip is the x of the point in the middle, stem the x of the straight parts, end the x of the
        // two ends. For "{" the tip is on the left of the ends, for "}" on the right.
        static string Brace(double tip, double stem, double end, double top, double bottom)
        {
            // Bezier handles for a quarter circle.
            const double k = 0.552;
            double middle = (top + bottom) / 2;
            double outer = end - stem;
            double inner = stem - tip;
            double ro = Math.Abs(outer);
            double ri = Math.Abs(inner);
            return Invariant($"M{end},{top} C{end - outer * k},{top} {stem},{top + ro * (1 - k)} {stem},{top + ro} ") +
                Invariant($"L{stem},{middle - ri} C{stem},{middle - ri * (1 - k)} {tip + inner * k},{middle} {tip},{middle} ") +
                Invariant($"C{tip + inner * k},{middle} {stem},{middle + ri * (1 - k)} {stem},{middle + ri} ") +
                Invariant($"L{stem},{bottom - ro} C{stem},{bottom - ro * (1 - k)} {end - outer * k},{bottom} {end},{bottom}");
        }

        // A gear outline: teeth that are toothWidth wide at the outer radius and rootWidth at the root radius.
        static string Gear(double x, double y, int teeth, double outer, double root, double toothWidth, double rootWidth)
        {
            double outerHalf = toothWidth / 2 / outer;
            double rootHalf = rootWidth / 2 / root;
            string Point(double radius, double angle) => Invariant($"{x + radius * Math.Sin(angle)},{y - radius * Math.Cos(angle)}");

            string path = "M" + Point(root, -rootHalf);
            for (int i = 0; i < teeth; i++)
            {
                double angle = 2 * Math.PI * i / teeth;
                double next = 2 * Math.PI * (i + 1) / teeth;
                path += Invariant($" L{Point(outer, angle - outerHalf)} A{outer},{outer} 0 0 1 {Point(outer, angle + outerHalf)}") +
                    Invariant($" L{Point(root, angle + rootHalf)} A{root},{root} 0 0 1 {Point(root, next - rootHalf)}");
            }

            return path + " Z";
        }

        enum Ink
        {
            Line,
            Accent,
            Promo,
            Tile,
            OnTile,
        }

        sealed class Palette
        {
            public static readonly Palette Light = new Palette("#424242", "#2B579A", "#B86E00");
            public static readonly Palette Dark = new Palette("#D4D4D4", "#3B9ADC", "#F2B53A");

            readonly Brush[] _brushes;

            Palette(string line, string accent, string promo)
            {
                _brushes = new[] { line, accent, promo, "#2B579A", "#FFFFFF" }.Select(CreateBrush).ToArray();
            }

            public Brush this[Ink ink] => _brushes[(int)ink];

            static Brush CreateBrush(string color)
            {
                SolidColorBrush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
                brush.Freeze();
                return brush;
            }
        }

        sealed class Shape
        {
            readonly Geometry _geometry;
            readonly Ink _ink;
            readonly bool _filled;
            readonly double _thickness;

            Shape(Geometry geometry, Ink ink, bool filled, double thickness)
            {
                _geometry = geometry;
                _ink = ink;
                _filled = filled;
                _thickness = thickness;
            }

            public static Shape Outline(Geometry geometry, Ink ink, double thickness = 1) => new Shape(geometry, ink, false, thickness);

            public static Shape Solid(Geometry geometry, Ink ink) => new Shape(geometry, ink, true, 0);

            // A solid shape with its corners rounded by an outline of the same colour.
            public static Shape Rounded(Geometry geometry, Ink ink) => new Shape(geometry, ink, true, 1);

            public void Draw(DrawingContext context, Palette palette)
            {
                Brush brush = palette[_ink];
                context.DrawGeometry(_filled ? brush : null, _thickness > 0 ? CreatePen(brush, _thickness) : null, _geometry);
            }

            // The area the shape covers, inside included, grown by the gap.
            public Geometry Area(double gap) =>
                Geometry.Combine(_geometry.GetWidenedPathGeometry(CreatePen(Brushes.Black, _thickness + 2 * gap)), _geometry, GeometryCombineMode.Union, null);

            static Pen CreatePen(Brush brush, double thickness) =>
                new Pen(brush, thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
        }

        // Shapes in front cut a gap into the shapes behind, so overlapping parts stay readable.
        sealed class Icon
        {
            readonly Shape[] _back;
            readonly Shape[] _front;
            readonly Geometry _clip;

            public Icon(Shape[] back, Shape[] front = null)
            {
                _back = back;
                _front = front ?? new Shape[0];

                Geometry cut = _front.Aggregate(Geometry.Empty, (area, shape) => Geometry.Combine(area, shape.Area(Gap), GeometryCombineMode.Union, null));
                _clip = Geometry.Combine(new RectangleGeometry(new Rect(0, 0, GridSize, GridSize)), cut, GeometryCombineMode.Exclude, null);
                _clip.Freeze();
            }

            public void Draw(DrawingContext context, Palette palette)
            {
                context.PushClip(_clip);
                foreach (Shape shape in _back)
                {
                    shape.Draw(context, palette);
                }

                context.Pop();
                foreach (Shape shape in _front)
                {
                    shape.Draw(context, palette);
                }
            }
        }
    }
}
