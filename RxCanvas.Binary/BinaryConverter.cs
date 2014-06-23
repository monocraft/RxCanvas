using RxCanvas.Interfaces;
using RxCanvas.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxCanvas.Binary
{
    internal static class BinaryReaderExtensions
    {
        public static NativeType ReadNativeType(this BinaryReader reader)
        {
            return (NativeType)reader.ReadByte();
        }

        public static IPoint ReadPoint(this BinaryReader reader)
        {
            return new XPoint(
                reader.ReadDouble(), 
                reader.ReadDouble());
        }

        public static IColor ReadColor(this BinaryReader reader)
        {
            return new XColor(
                reader.ReadByte(), 
                reader.ReadByte(), 
                reader.ReadByte(), 
                reader.ReadByte());
        }
    }

    internal static class BinaryWriterExtensions
    {
        public static void Write(this BinaryWriter writer, NativeType type)
        {
            writer.Write((byte)type);
        }

        public static void Write(this BinaryWriter writer, IPoint point)
        {
            writer.Write(point.X);
            writer.Write(point.Y);
        }

        public static void Write(this BinaryWriter writer, IColor color)
        {
            writer.Write(color.A);
            writer.Write(color.R);
            writer.Write(color.G);
            writer.Write(color.B);
        }
    }

    internal enum NativeType : byte
    {
        Line,
        Bezier,
        QuadraticBezier,
        Arc,
        Rectangle,
        Ellipse,
        Text
    }

    public static class BinaryConverter
    {
        public static ICanvas Open(string path)
        {
            using (var file = File.Open(path, FileMode.Open))
            {
                return Read(file);
            }
        }

        public static void Save(string path, ICanvas canvas)
        {
            using (var file = File.Create(path))
            {
                Write(file, canvas);
            }
        }

        public static ICanvas Read(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                var canvas = new XCanvas();
                var children = canvas.Children;
                canvas.Width = reader.ReadDouble();
                canvas.Height = reader.ReadDouble();
                canvas.Background = reader.ReadColor();
                canvas.EnableSnap = reader.ReadBoolean();
                canvas.SnapX = reader.ReadDouble();
                canvas.SnapY = reader.ReadDouble();

                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    var type = reader.ReadNativeType();
                    switch(type)
                    {
                        case NativeType.Line:
                            {
                                var line = new XLine();
                                line.Point1 = reader.ReadPoint();
                                line.Point2 = reader.ReadPoint();
                                line.Stroke = reader.ReadColor();
                                line.StrokeThickness = reader.ReadDouble();
                                children.Add(line);
                            }
                            break;
                        case NativeType.Bezier:
                            {
                                var bezier = new XBezier();
                                bezier.Start = reader.ReadPoint();
                                bezier.Point1 = reader.ReadPoint();
                                bezier.Point2 = reader.ReadPoint();
                                bezier.Point3 = reader.ReadPoint();
                                bezier.Stroke = reader.ReadColor();
                                bezier.StrokeThickness = reader.ReadDouble();
                                bezier.Fill = reader.ReadColor();
                                bezier.IsFilled = reader.ReadBoolean();
                                bezier.IsClosed = reader.ReadBoolean();
                                children.Add(bezier);
                            }
                            break;
                        case NativeType.QuadraticBezier:
                            {
                                var quadraticBezier = new XQuadraticBezier();
                                quadraticBezier.Start = reader.ReadPoint();
                                quadraticBezier.Point1 = reader.ReadPoint();
                                quadraticBezier.Point2 = reader.ReadPoint();
                                quadraticBezier.Stroke = reader.ReadColor();
                                quadraticBezier.StrokeThickness = reader.ReadDouble();
                                quadraticBezier.Fill = reader.ReadColor();
                                quadraticBezier.IsFilled = reader.ReadBoolean();
                                quadraticBezier.IsClosed = reader.ReadBoolean();
                                children.Add(quadraticBezier);
                            }
                            break;
                        case NativeType.Arc:
                            {
                                //var arc = new XArc();
                                //arc.Point1 = reader.ReadPoint();
                                //arc.Point2 = reader.ReadPoint();
                                //arc.StartAngle = reader.ReadDouble();
                                //arc.SweepAngle = reader.ReadDouble();
                                //arc.Stroke = reader.ReadColor();
                                //arc.StrokeThickness = reader.ReadDouble();
                                //arc.Fill = reader.ReadColor();
                                //arc.IsFilled = reader.ReadBoolean();
                                //arc.IsClosed = reader.ReadBoolean();
                                //children.Add(arc);
                            }
                            break;
                        case NativeType.Rectangle:
                            {
                                var rectangle = new XRectangle();
                                rectangle.Point1 = reader.ReadPoint();
                                rectangle.Point2 = reader.ReadPoint();
                                rectangle.Stroke = reader.ReadColor();
                                rectangle.StrokeThickness = reader.ReadDouble();
                                rectangle.Fill = reader.ReadColor();
                                children.Add(rectangle);
                            }
                            break;
                        case NativeType.Ellipse:
                            {
                                var ellipse = new XEllipse();
                                ellipse.Point1 = reader.ReadPoint();
                                ellipse.Point2 = reader.ReadPoint();
                                ellipse.Stroke = reader.ReadColor();
                                ellipse.StrokeThickness = reader.ReadDouble();
                                ellipse.Fill = reader.ReadColor();
                                children.Add(ellipse);
                            }
                            break;
                        case NativeType.Text:
                            {
                                //var text = new XText();
                                //text.Point1 = reader.ReadPoint();
                                //text.Point2 = reader.ReadPoint();
                                //text.HorizontalAlignment = reader.ReadInt32();
                                //text.VerticalAlignment = reader.ReadInt32();
                                //text.Size = reader.ReadDouble();
                                //text.Text = reader.ReadString();
                                //text.Foreground = reader.ReadColor();
                                //text.Backgroud = reader.ReadColor();
                                //children.Add(text);
                            }
                            break;
                    }
                }

                return canvas;
            }
        }

        public static void Write(Stream stream, ICanvas canvas)
        {
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(canvas.Width);
                writer.Write(canvas.Height);
                writer.Write(canvas.Background);
                writer.Write(canvas.EnableSnap);
                writer.Write(canvas.SnapX);
                writer.Write(canvas.SnapY);

                var children = canvas.Children;
                int count = children.Count;
                for (int i = 0; i < count; i++)
                {
                    var child = children[i];

                    if (child is ILine)
                    {
                        var line = child as ILine;
                        writer.Write(NativeType.Line);
                        writer.Write(line.Point1);
                        writer.Write(line.Point2);
                        writer.Write(line.Stroke);
                        writer.Write(line.StrokeThickness);
                    }
                    else if (child is IBezier)
                    {
                        var bezier = child as IBezier;
                        writer.Write(NativeType.Bezier);
                        writer.Write(bezier.Start);
                        writer.Write(bezier.Point1);
                        writer.Write(bezier.Point2);
                        writer.Write(bezier.Point3);
                        writer.Write(bezier.Stroke);
                        writer.Write(bezier.StrokeThickness);
                        writer.Write(bezier.Fill);
                        writer.Write(bezier.IsFilled);
                        writer.Write(bezier.IsClosed);
                    }
                    else if (child is IQuadraticBezier)
                    {
                        var quadraticBezier = child as IQuadraticBezier;
                        writer.Write(NativeType.QuadraticBezier);
                        writer.Write(quadraticBezier.Start);
                        writer.Write(quadraticBezier.Point1);
                        writer.Write(quadraticBezier.Point2);
                        writer.Write(quadraticBezier.Stroke);
                        writer.Write(quadraticBezier.StrokeThickness);
                        writer.Write(quadraticBezier.Fill);
                        writer.Write(quadraticBezier.IsFilled);
                        writer.Write(quadraticBezier.IsClosed);
                    }
                    else if (child is IArc)
                    {
                        //var arc = child as IArc;
                        //writer.Write(NativeType.Arc);
                        //writer.Write(arc.Point1);
                        //writer.Write(arc.Point2);
                        //writer.Write(arc.StartAngle);
                        //writer.Write(arc.SweepAngle);
                        //writer.Write(arc.Stroke);
                        //writer.Write(arc.StrokeThickness);
                        //writer.Write(arc.Fill);
                        //writer.Write(arc.IsFilled);
                        //writer.Write(arc.IsClosed);
                    }
                    else if (child is IRectangle)
                    {
                        var rectangle = child as IRectangle;
                        writer.Write(NativeType.Rectangle);
                        writer.Write(rectangle.Point1);
                        writer.Write(rectangle.Point2);
                        writer.Write(rectangle.Stroke);
                        writer.Write(rectangle.StrokeThickness);
                        writer.Write(rectangle.Fill);
                    }
                    else if (child is IEllipse)
                    {
                        var ellipse = child as IEllipse;
                        writer.Write(NativeType.Ellipse);
                        writer.Write(ellipse.Point1);
                        writer.Write(ellipse.Point2);
                        writer.Write(ellipse.Stroke);
                        writer.Write(ellipse.StrokeThickness);
                        writer.Write(ellipse.Fill);
                    }
                    else if (child is IText)
                    {
                        //var text = child as IText;
                        //writer.Write(NativeType.Text);
                        //writer.Write(text.Point1);
                        //writer.Write(text.Point2);
                        //writer.Write(text.HorizontalAlignment);
                        //writer.Write(text.VerticalAlignment);
                        //writer.Write(text.Size);
                        //writer.Write(text.Text);
                        //writer.Write(text.Foreground);
                        //writer.Write(text.Backgroud);
                    }
                }
            }
        }
    }
}
