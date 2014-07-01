using RxCanvas.Interfaces;
using RxCanvas.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxCanvas.Serializers
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

        public static ILine ReadLine(this BinaryReader reader)
        {
            return new XLine()
            {
                Point1 = reader.ReadPoint(),
                Point2 = reader.ReadPoint(),
                Stroke = reader.ReadColor(),
                StrokeThickness = reader.ReadDouble()  
            };
        }

        public static IBezier ReadBezier(this BinaryReader reader)
        {
            return new XBezier()
            {
                Start = reader.ReadPoint(),
                Point1 = reader.ReadPoint(),
                Point2 = reader.ReadPoint(),
                Point3 = reader.ReadPoint(),
                Stroke = reader.ReadColor(),
                StrokeThickness = reader.ReadDouble(),
                Fill = reader.ReadColor(),
                IsFilled = reader.ReadBoolean(),
                IsClosed = reader.ReadBoolean()
            };
        }

        public static IQuadraticBezier ReadQuadraticBezier(this BinaryReader reader)
        {
            return new XQuadraticBezier()
            {
                Start = reader.ReadPoint(),
                Point1 = reader.ReadPoint(),
                Point2 = reader.ReadPoint(),
                Stroke = reader.ReadColor(),
                StrokeThickness = reader.ReadDouble(),
                Fill = reader.ReadColor(),
                IsFilled = reader.ReadBoolean(),
                IsClosed = reader.ReadBoolean()
            };
        }

        public static IArc ReadArc(this BinaryReader reader)
        {
            return new XArc()
            {
                Point1 = reader.ReadPoint(),
                Point2 = reader.ReadPoint(),
                StartAngle = reader.ReadDouble(),
                SweepAngle = reader.ReadDouble(),
                Stroke = reader.ReadColor(),
                StrokeThickness = reader.ReadDouble(),
                Fill = reader.ReadColor(),
                IsFilled = reader.ReadBoolean(),
                IsClosed = reader.ReadBoolean()
            };
        }

        public static IRectangle ReadRectangle(this BinaryReader reader)
        {
            return new XRectangle()
            {
                Point1 = reader.ReadPoint(),
                Point2 = reader.ReadPoint(),
                Stroke = reader.ReadColor(),
                StrokeThickness = reader.ReadDouble(),
                Fill = reader.ReadColor()
            };
        }

        public static IEllipse ReadEllipse(this BinaryReader reader)
        {
            return new XEllipse()
            {
                Point1 = reader.ReadPoint(),
                Point2 = reader.ReadPoint(),
                Stroke = reader.ReadColor(),
                StrokeThickness = reader.ReadDouble(),
                Fill = reader.ReadColor()
            };
        }

        public static IText ReadText(this BinaryReader reader)
        {
            return new XText()
            {
                Point1 = reader.ReadPoint(),
                Point2 = reader.ReadPoint(),
                HorizontalAlignment = reader.ReadInt32(),
                VerticalAlignment = reader.ReadInt32(),
                Size = reader.ReadDouble(),
                Text = reader.ReadString(),
                Foreground = reader.ReadColor(),
                Backgroud = reader.ReadColor()
            };
        }

        public static IBlock ReadBlock(this BinaryReader reader)
        {
            var block = new XBlock();
            var children = block.Children;

            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                var type = reader.ReadNativeType();
                switch (type)
                {
                    case NativeType.Line:
                        children.Add(reader.ReadLine());
                        break;
                    case NativeType.Bezier:
                        children.Add(reader.ReadBezier());
                        break;
                    case NativeType.QuadraticBezier:
                        children.Add(reader.ReadQuadraticBezier());
                        break;
                    case NativeType.Arc:
                        children.Add(reader.ReadArc());
                        break;
                    case NativeType.Rectangle:
                        children.Add(reader.ReadRectangle());
                        break;
                    case NativeType.Ellipse:
                        children.Add(reader.ReadEllipse());
                        break;
                    case NativeType.Text:
                        children.Add(reader.ReadText());
                        break;
                    case NativeType.Block:
                        children.Add(reader.ReadBlock());
                        break;
                    case NativeType.End:
                        return block;
                    default:
                        throw new InvalidDataException();
                }
            }

            throw new InvalidDataException();
        }

        public static ICanvas ReadCanvas(this BinaryReader reader)
        {
            var canvas = new XCanvas()
            {
                Width = reader.ReadDouble(),
                Height = reader.ReadDouble(),
                Background = reader.ReadColor(),
                EnableSnap = reader.ReadBoolean(),
                SnapX = reader.ReadDouble(),
                SnapY = reader.ReadDouble()
            };
            var children = canvas.Children;

            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                var type = reader.ReadNativeType();
                switch (type)
                {
                    case NativeType.Line:
                        children.Add(reader.ReadLine());
                        break;
                    case NativeType.Bezier:
                        children.Add(reader.ReadBezier());
                        break;
                    case NativeType.QuadraticBezier:
                        children.Add(reader.ReadQuadraticBezier());
                        break;
                    case NativeType.Arc:
                        children.Add(reader.ReadArc());
                        break;
                    case NativeType.Rectangle:
                        children.Add(reader.ReadRectangle());
                        break;
                    case NativeType.Ellipse:
                        children.Add(reader.ReadEllipse());
                        break;
                    case NativeType.Text:
                        children.Add(reader.ReadText());
                        break;
                    case NativeType.Block:
                        children.Add(reader.ReadBlock());
                        break;
                    case NativeType.End:
                        return canvas;
                    default:
                        throw new InvalidDataException();
                }
            }

            throw new InvalidDataException();
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

        public static void Write(this BinaryWriter writer, ILine line)
        {
            writer.Write(NativeType.Line);
            writer.Write(line.Point1);
            writer.Write(line.Point2);
            writer.Write(line.Stroke);
            writer.Write(line.StrokeThickness);
        }

        public static void Write(this BinaryWriter writer, IBezier bezier)
        {
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

        public static void Write(this BinaryWriter writer, IQuadraticBezier quadraticBezier)
        {
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

        public static void Write(this BinaryWriter writer, IArc arc)
        {
            writer.Write(NativeType.Arc);
            writer.Write(arc.Point1);
            writer.Write(arc.Point2);
            writer.Write(arc.StartAngle);
            writer.Write(arc.SweepAngle);
            writer.Write(arc.Stroke);
            writer.Write(arc.StrokeThickness);
            writer.Write(arc.Fill);
            writer.Write(arc.IsFilled);
            writer.Write(arc.IsClosed);
        }

        public static void Write(this BinaryWriter writer, IRectangle rectangle)
        {
            writer.Write(NativeType.Rectangle);
            writer.Write(rectangle.Point1);
            writer.Write(rectangle.Point2);
            writer.Write(rectangle.Stroke);
            writer.Write(rectangle.StrokeThickness);
            writer.Write(rectangle.Fill);
        }

        public static void Write(this BinaryWriter writer, IEllipse ellipse)
        {
            writer.Write(NativeType.Ellipse);
            writer.Write(ellipse.Point1);
            writer.Write(ellipse.Point2);
            writer.Write(ellipse.Stroke);
            writer.Write(ellipse.StrokeThickness);
            writer.Write(ellipse.Fill);
        }

        public static void Write(this BinaryWriter writer, IText text)
        {
            writer.Write(NativeType.Text);
            writer.Write(text.Point1);
            writer.Write(text.Point2);
            writer.Write(text.HorizontalAlignment);
            writer.Write(text.VerticalAlignment);
            writer.Write(text.Size);
            writer.Write(text.Text);
            writer.Write(text.Foreground);
            writer.Write(text.Backgroud);
        }

        public static void Write(this BinaryWriter writer, IList<INative> children)
        {
            int count = children.Count;
            for (int i = 0; i < count; i++)
            {
                var child = children[i];

                if (child is ILine)
                {
                    writer.Write(child as ILine);
                }
                else if (child is IBezier)
                {
                    writer.Write(child as IBezier);
                }
                else if (child is IQuadraticBezier)
                {
                    writer.Write(child as IQuadraticBezier);
                }
                else if (child is IArc)
                {
                    writer.Write(child as IArc);
                }
                else if (child is IRectangle)
                {
                    writer.Write(child as IRectangle);
                }
                else if (child is IEllipse)
                {
                    writer.Write(child as IEllipse);
                }
                else if (child is IText)
                {
                    writer.Write(child as IText);
                }
                else if (child is IBlock)
                {
                    writer.Write(child as IBlock);
                }
            }
        }

        public static void Write(this BinaryWriter writer, IBlock block)
        {
            writer.Write(NativeType.Block);
            writer.Write(block.Children);
            writer.Write(NativeType.End);
        }

        public static void Write(this BinaryWriter writer, ICanvas canvas)
        {
            writer.Write(NativeType.Canvas);
            writer.Write(canvas.Width);
            writer.Write(canvas.Height);
            writer.Write(canvas.Background);
            writer.Write(canvas.EnableSnap);
            writer.Write(canvas.SnapX);
            writer.Write(canvas.SnapY);
            writer.Write(canvas.Children);
            writer.Write(NativeType.End);
        }
    }

    internal enum NativeType : byte
    {
        // Solution
        Solution        = 0x01,
        Project         = 0x02,
        Canvas          = 0x03,
        // Block
        Block           = 0x11,
        End             = 0x12,
        // Primitive
        Line            = 0x21,
        Bezier          = 0x22,
        QuadraticBezier = 0x23,
        Arc             = 0x24,
        Rectangle       = 0x25,
        Ellipse         = 0x26,
        Text            = 0x27,
    }

    [Export(typeof(IFile))]
    public class BinaryFile : IFile
    {
        public string Name { get; set; }
        public string Extension { get; set; }

        public BinaryFile()
        {
            Name = "Binary";
            Extension = "bin";
        }

        public ICanvas Open(string path)
        {
            using (var file = File.Open(path, FileMode.Open))
            {
                return Read(file);
            }
        }

        public void Save(string path, ICanvas canvas)
        {
            using (var file = File.Create(path))
            {
                Write(file, canvas);
            }
        }

        public ICanvas Read(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                var type = reader.ReadNativeType();
                if (type == NativeType.Canvas)
                {
                    return reader.ReadCanvas();
                }
                throw new InvalidDataException();
            }
        }

        public void Write(Stream stream, ICanvas canvas)
        {
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(canvas);
            }
        }
    }
}
