using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using RxCanvas.Interfaces;

namespace RxCanvas.Creators
{
    public class CoreCanvasPdfCreator : ICreator<ICanvas>
    {
        public string Name { get; set; }
        public string Extension { get; set; }

        private Func<double, double> X;
        private Func<double, double> Y;

        public CoreCanvasPdfCreator()
        {
            Name = "Pdf";
            Extension = "pdf";
        }

        public void Save(string path, ICanvas canvas)
        {
            using (var document = new PdfDocument())
            {
                AddPage(document, canvas);
                document.Save(path);
            }
        }

        public void Save(string path, IEnumerable<ICanvas> canvases)
        {
            using (var document = new PdfDocument())
            {
                foreach (var canvas in canvases)
                {
                    AddPage(document, canvas);
                }
                document.Save(path);
            }
        }

        private void AddPage(PdfDocument document, ICanvas canvas)
        {
            PdfPage page = document.AddPage();
            page.Size = PageSize.A4;
            page.Orientation = PageOrientation.Landscape;
            using (XGraphics gfx = XGraphics.FromPdfPage(page))
            {
                double scaleX = page.Width.Value / canvas.Width;
                double scaleY = page.Height.Value / canvas.Height;
                double scale = Math.Min(scaleX, scaleY);
                X = (x) => x * scale;
                Y = (y) => y * scale;
                DrawCanvas(gfx, canvas);
            }
        }

        private XColor ToXColor(IColor color)
        {
            return XColor.FromArgb(color.A, color.R, color.G, color.B);
        }

        private void DrawLine(XGraphics gfx, ILine line)
        {
            var pen = new XPen(ToXColor(line.Stroke), X(line.StrokeThickness));

            gfx.DrawLine(
                pen, 
                X(line.Point1.X), 
                Y(line.Point1.Y), 
                X(line.Point2.X), 
                Y(line.Point2.Y));
        }

        private void DrawBezier(XGraphics gfx, IBezier bezier)
        {
            var pen = new XPen(
                ToXColor(bezier.Stroke), 
                X(bezier.StrokeThickness));

            gfx.DrawBezier(pen,
                X(bezier.Start.X), Y(bezier.Start.Y),
                X(bezier.Point1.X), Y(bezier.Point1.Y),
                X(bezier.Point2.X), Y(bezier.Point2.Y),
                X(bezier.Point3.X), Y(bezier.Point3.Y));
        }

        private void DrawQuadraticBezier(XGraphics gfx, IQuadraticBezier quadraticBezier)
        {
            double x1 = quadraticBezier.Start.X;
            double y1 = quadraticBezier.Start.Y;
            double x2 = quadraticBezier.Start.X + (2.0 * (quadraticBezier.Point1.X - quadraticBezier.Start.X)) / 3.0;
            double y2 = quadraticBezier.Start.Y + (2.0 * (quadraticBezier.Point1.Y - quadraticBezier.Start.Y)) / 3.0;
            double x3 = x2 + (quadraticBezier.Point2.X - quadraticBezier.Start.X) / 3.0;
            double y3 = y2 + (quadraticBezier.Point2.Y - quadraticBezier.Start.Y) / 3.0;
            double x4 = quadraticBezier.Point2.X;
            double y4 = quadraticBezier.Point2.Y;

            var pen = new XPen(
                ToXColor(quadraticBezier.Stroke), 
                X(quadraticBezier.StrokeThickness));

            gfx.DrawBezier(pen,
                X(x1), Y(y1),
                X(x2), Y(y2),
                X(x3), Y(y3),
                X(x4), Y(y4));
        }

        private void DrawArc(XGraphics gfx, IArc arc)
        {
            var pen = new XPen(
                ToXColor(arc.Stroke), 
                X(arc.StrokeThickness));

            gfx.DrawArc(
                pen, 
                X(arc.X), 
                Y(arc.Y), 
                X(arc.Width), 
                Y(arc.Height), 
                arc.StartAngle, 
                arc.SweepAngle);
        }

        private void DrawRectangle(XGraphics gfx, IRectangle rectangle)
        {
            if (rectangle.Fill.A > 0x00)
            {
                var pen = new XPen(
                    ToXColor(rectangle.Stroke), 
                    X(rectangle.StrokeThickness));

                var brush = new XSolidBrush(ToXColor(rectangle.Fill));

                double x = Math.Min(rectangle.Point1.X, rectangle.Point2.X);
                double y = Math.Min(rectangle.Point1.Y, rectangle.Point2.Y);
                double width = Math.Abs(rectangle.Point2.X - rectangle.Point1.X);
                double height = Math.Abs(rectangle.Point2.Y - rectangle.Point1.Y);

                gfx.DrawRectangle(
                    pen, 
                    brush, 
                    X(x), 
                    Y(y), 
                    X(width), 
                    Y(height));
            }
            else
            {
                var pen = new XPen(
                    ToXColor(rectangle.Stroke), 
                    X(rectangle.StrokeThickness));

                double x = Math.Min(rectangle.Point1.X, rectangle.Point2.X);
                double y = Math.Min(rectangle.Point1.Y, rectangle.Point2.Y);
                double width = Math.Abs(rectangle.Point2.X - rectangle.Point1.X);
                double height = Math.Abs(rectangle.Point2.Y - rectangle.Point1.Y);

                gfx.DrawRectangle(
                    pen, 
                    X(x), 
                    Y(y), 
                    X(width), 
                    Y(height));
            }
        }

        private void DrawEllipse(XGraphics gfx, IEllipse ellipse)
        {
            if (ellipse.Fill.A > 0x00)
            {
                var pen = new XPen(
                    ToXColor(ellipse.Stroke), 
                    X(ellipse.StrokeThickness));

                var brush = new XSolidBrush(ToXColor(ellipse.Fill));
                
                gfx.DrawEllipse(
                    pen, 
                    brush, 
                    X(ellipse.X), 
                    Y(ellipse.Y), 
                    X(ellipse.Width), 
                    Y(ellipse.Height));
            }
            else
            {
                var pen = new XPen(
                    ToXColor(ellipse.Stroke), 
                    X(ellipse.StrokeThickness));
                
                gfx.DrawEllipse(pen,
                    X(ellipse.X), 
                    Y(ellipse.Y), 
                    X(ellipse.Width), 
                    Y(ellipse.Height));
            }
        }

        private void DrawText(XGraphics gfx, IText text)
        {
            XPdfFontOptions options = new XPdfFontOptions(
                PdfFontEncoding.Unicode, 
                PdfFontEmbedding.Always);

            XFont font = new XFont(
                "Calibri", 
                Y(text.Size), 
                XFontStyle.Regular, 
                options);

            XStringFormat format = new XStringFormat();

            XRect rect = new XRect(
                X(text.X), 
                Y(text.Y), 
                X(text.Width), 
                Y(text.Height));

            switch (text.HorizontalAlignment)
            {
                case 0: format.Alignment = XStringAlignment.Near; break;
                case 1: format.Alignment = XStringAlignment.Center; break;
                case 2: format.Alignment = XStringAlignment.Far; break;
            }

            switch (text.VerticalAlignment)
            {
                case 0: format.LineAlignment = XLineAlignment.Near; break;
                case 1: format.LineAlignment = XLineAlignment.Center; break;
                case 2: format.LineAlignment = XLineAlignment.Far; break;
            }

            if (text.Backgroud.A != 0x00)
            {
                var brushBackground = new XSolidBrush(ToXColor(text.Backgroud));
                gfx.DrawRectangle(brushBackground, rect);
            }

            var brushForeground = new XSolidBrush(ToXColor(text.Foreground));
            gfx.DrawString(
                text.Text, 
                font, 
                brushForeground, 
                rect, 
                format);
        }

        private void DrawCanvas(XGraphics gfx, ICanvas canvas)
        {
            foreach (var child in canvas.Children)
            {
                if (child is ILine)
                {
                    DrawLine(gfx, child as ILine);
                }
                else if (child is IBezier)
                {
                    DrawBezier(gfx, child as IBezier);
                }
                else if (child is IQuadraticBezier)
                {
                    DrawQuadraticBezier(gfx, child as IQuadraticBezier);
                }
                else if (child is IArc)
                {
                    DrawArc(gfx, child as IArc);
                }
                else if (child is IRectangle)
                {
                    DrawRectangle(gfx, child as IRectangle);
                }
                else if (child is IEllipse)
                {
                    DrawEllipse(gfx, child as IEllipse);
                }
                else if (child is IText)
                {
                    DrawText(gfx, child as IText);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }
    }

    public class CoreCanvasDxfCreator : ICreator<ICanvas>
    {
        public string Name { get; set; }
        public string Extension { get; set; }

        public CoreCanvasDxfCreator()
        {
            Name = "Dxf";
            Extension = "dxf";
        }

        public void Save(string path, ICanvas canvas)
        {
            throw new NotImplementedException();
        }

        public void Save(string path, IEnumerable<ICanvas> canvases)
        {
            throw new NotImplementedException();
        }
    }
}
