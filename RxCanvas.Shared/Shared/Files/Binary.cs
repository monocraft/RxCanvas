using RxCanvas.Interfaces;
using RxCanvas.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxCanvas.Serializers
{
    internal enum NativeType : byte
    {
        // Solution
        Solution = 0x01,
        Project = 0x02,
        Canvas = 0x03,
        // Block
        Block = 0x11,
        End = 0x12,
        // Primitive
        Pin = 0x21,
        Line = 0x22,
        Bezier = 0x23,
        QuadraticBezier = 0x24,
        Arc = 0x25,
        Rectangle = 0x26,
        Ellipse = 0x27,
        Text = 0x28,
    }

    internal struct BPoint
    {
        public int Id;
        public int[] Connected;
        public IPoint Point;
    }

    internal class IdPreprocessor
    {
        private int _nextId;
        private List<IPoint> _points;

        private void Process(IPoint point)
        {
            // add only unique points
            if (point.Id == 0)
            {
                point.Id = NextId();
                _points.Add(point);
            }
        }

        private void Process(IPin pin)
        {
            pin.Id = NextId();
            Process(pin.Point);
        }

        private void Process(ILine line)
        {
            line.Id = NextId();
            Process(line.Point1);
            Process(line.Point2);
        }

        private void Process(IBezier bezier)
        {
            bezier.Id = NextId();
            Process(bezier.Start);
            Process(bezier.Point1);
            Process(bezier.Point2);
            Process(bezier.Point3);
        }

        private void Process(IQuadraticBezier quadraticBezier)
        {
            quadraticBezier.Id = NextId();
            Process(quadraticBezier.Start);
            Process(quadraticBezier.Point1);
            Process(quadraticBezier.Point2);
        }

        private void Process(IArc arc)
        {
            arc.Id = NextId();
            Process(arc.Point1);
            Process(arc.Point2);
        }

        private void Process(IRectangle rectangle)
        {
            rectangle.Id = NextId();
            Process(rectangle.Point1);
            Process(rectangle.Point2);
        }

        private void Process(IEllipse ellipse)
        {
            ellipse.Id = NextId();
            Process(ellipse.Point1);
            Process(ellipse.Point2);
        }

        private void Process(IText text)
        {
            text.Id = NextId();
            Process(text.Point1);
            Process(text.Point2);
        }

        private void Process(IList<INative> children)
        {
            int count = children.Count;
            for (int i = 0; i < count; i++)
            {
                Process(children[i]);
            }
        }

        private void Process(INative child)
        {
            if (child is IPin)
            {
                Process(child as IPin);
            }
            else if (child is ILine)
            {
                Process(child as ILine);
            }
            else if (child is IBezier)
            {
                Process(child as IBezier);
            }
            else if (child is IQuadraticBezier)
            {
                Process(child as IQuadraticBezier);
            }
            else if (child is IArc)
            {
                Process(child as IArc);
            }
            else if (child is IRectangle)
            {
                Process(child as IRectangle);
            }
            else if (child is IEllipse)
            {
                Process(child as IEllipse);
            }
            else if (child is IText)
            {
                Process(child as IText);
            }
            else if (child is IBlock)
            {
                Process(child as IBlock);
            }
        }

        private void Process(IBlock block)
        {
            block.Id = NextId();
            Process(block.Children);
        }

        private int NextId()
        {
            return _nextId++;
        }

        public BPoint[] Process(ICanvas canvas)
        {
            _nextId = 1;
            _points = new List<IPoint>();

            canvas.Id = NextId();
            Process(canvas.Children);

            var bpoints = new BPoint[_points.Count];

            for (int i = 0; i < _points.Count; i++)
            {
                var point = _points[i];
                var bpoint = new BPoint();

                bpoint.Id = point.Id;
                bpoint.Connected = new int[point.Connected.Count];

                for (int j = 0; j < point.Connected.Count; j++)
                {
                    bpoint.Connected[j] = point.Connected[j].Id;
                }

                bpoint.Point = point;
                bpoints[i] = bpoint;
            }

            return bpoints;
        }
    }

    internal class CanvasReader
    {
        private BinaryReader _reader;

        private NativeType ReadNativeType()
        {
            return (NativeType)_reader.ReadByte();
        }

        private INative ReadNative()
        {
            var type = ReadNativeType();
            switch (type)
            {
                case NativeType.Pin:
                    return ReadPin();
                case NativeType.Line:
                    return ReadLine();
                case NativeType.Bezier:
                    return ReadBezier();
                case NativeType.QuadraticBezier:
                    return ReadQuadraticBezier();
                case NativeType.Arc:
                    return ReadArc();
                case NativeType.Rectangle:
                    return ReadRectangle();
                case NativeType.Ellipse:
                    return ReadEllipse();
                case NativeType.Text:
                    return ReadText();
                case NativeType.Block:
                    return ReadBlock();
                default:
                    throw new InvalidDataException();
            }
        }

        private IPoint ReadPoint()
        {
            return new XPoint(
                _reader.ReadDouble(),
                _reader.ReadDouble());
        }

        private IColor ReadColor()
        {
            return new XColor(
                _reader.ReadByte(),
                _reader.ReadByte(),
                _reader.ReadByte(),
                _reader.ReadByte());
        }

        private IPin ReadPin()
        {
            return new XPin()
            {
                Id = _reader.ReadInt32(),
                Point = ReadPoint(),
                Shape = ReadNative(),
            };
        }

        private ILine ReadLine()
        {
            return new XLine()
            {
                Id = _reader.ReadInt32(),
                Point1 = ReadPoint(),
                Point2 = ReadPoint(),
                Stroke = ReadColor(),
                StrokeThickness = _reader.ReadDouble()  
            };
        }

        private IBezier ReadBezier()
        {
            return new XBezier()
            {
                Id = _reader.ReadInt32(),
                Start = ReadPoint(),
                Point1 = ReadPoint(),
                Point2 = ReadPoint(),
                Point3 = ReadPoint(),
                Stroke = ReadColor(),
                StrokeThickness = _reader.ReadDouble(),
                Fill = ReadColor(),
                IsFilled = _reader.ReadBoolean(),
                IsClosed = _reader.ReadBoolean()
            };
        }

        private IQuadraticBezier ReadQuadraticBezier()
        {
            return new XQuadraticBezier()
            {
                Id = _reader.ReadInt32(),
                Start = ReadPoint(),
                Point1 = ReadPoint(),
                Point2 = ReadPoint(),
                Stroke = ReadColor(),
                StrokeThickness = _reader.ReadDouble(),
                Fill = ReadColor(),
                IsFilled = _reader.ReadBoolean(),
                IsClosed = _reader.ReadBoolean()
            };
        }

        private IArc ReadArc()
        {
            return new XArc()
            {
                Id = _reader.ReadInt32(),
                Point1 = ReadPoint(),
                Point2 = ReadPoint(),
                StartAngle = _reader.ReadDouble(),
                SweepAngle = _reader.ReadDouble(),
                Stroke = ReadColor(),
                StrokeThickness = _reader.ReadDouble(),
                Fill = ReadColor(),
                IsFilled = _reader.ReadBoolean(),
                IsClosed = _reader.ReadBoolean()
            };
        }

        private IRectangle ReadRectangle()
        {
            return new XRectangle()
            {
                Id = _reader.ReadInt32(),
                Point1 = ReadPoint(),
                Point2 = ReadPoint(),
                Stroke = ReadColor(),
                StrokeThickness = _reader.ReadDouble(),
                Fill = ReadColor()
            };
        }

        private IEllipse ReadEllipse()
        {
            return new XEllipse()
            {
                Id = _reader.ReadInt32(),
                Point1 = ReadPoint(),
                Point2 = ReadPoint(),
                Stroke = ReadColor(),
                StrokeThickness = _reader.ReadDouble(),
                Fill = ReadColor()
            };
        }

        private IText ReadText()
        {
            return new XText()
            {
                Id = _reader.ReadInt32(),
                Point1 = ReadPoint(),
                Point2 = ReadPoint(),
                HorizontalAlignment = _reader.ReadInt32(),
                VerticalAlignment = _reader.ReadInt32(),
                Size = _reader.ReadDouble(),
                Text = _reader.ReadString(),
                Foreground = ReadColor(),
                Backgroud = ReadColor()
            };
        }

        private IBlock ReadBlock()
        {
            var block = new XBlock()
            {
                Id = _reader.ReadInt32()
            };
            var children = block.Children;

            while (_reader.BaseStream.Position != _reader.BaseStream.Length)
            {
                var type = ReadNativeType();
                switch (type)
                {
                    case NativeType.Pin:
                        children.Add(ReadPin());
                        break;
                    case NativeType.Line:
                        children.Add(ReadLine());
                        break;
                    case NativeType.Bezier:
                        children.Add(ReadBezier());
                        break;
                    case NativeType.QuadraticBezier:
                        children.Add(ReadQuadraticBezier());
                        break;
                    case NativeType.Arc:
                        children.Add(ReadArc());
                        break;
                    case NativeType.Rectangle:
                        children.Add(ReadRectangle());
                        break;
                    case NativeType.Ellipse:
                        children.Add(ReadEllipse());
                        break;
                    case NativeType.Text:
                        children.Add(ReadText());
                        break;
                    case NativeType.Block:
                        children.Add(ReadBlock());
                        break;
                    case NativeType.End:
                        return block;
                    default:
                        throw new InvalidDataException();
                }
            }

            throw new InvalidDataException();
        }

        public ICanvas Read(BinaryReader reader)
        {
            _reader = reader;

            var canvas = new XCanvas()
            {
                Id = _reader.ReadInt32(),
                Width = _reader.ReadDouble(),
                Height = _reader.ReadDouble(),
                Background = ReadColor(),
                EnableSnap = _reader.ReadBoolean(),
                SnapX = _reader.ReadDouble(),
                SnapY = _reader.ReadDouble()
            };
            var children = canvas.Children;

            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                var type = ReadNativeType();
                switch (type)
                {
                    case NativeType.Pin:
                        children.Add(ReadPin());
                        break;
                    case NativeType.Line:
                        children.Add(ReadLine());
                        break;
                    case NativeType.Bezier:
                        children.Add(ReadBezier());
                        break;
                    case NativeType.QuadraticBezier:
                        children.Add(ReadQuadraticBezier());
                        break;
                    case NativeType.Arc:
                        children.Add(ReadArc());
                        break;
                    case NativeType.Rectangle:
                        children.Add(ReadRectangle());
                        break;
                    case NativeType.Ellipse:
                        children.Add(ReadEllipse());
                        break;
                    case NativeType.Text:
                        children.Add(ReadText());
                        break;
                    case NativeType.Block:
                        children.Add(ReadBlock());
                        break;
                    case NativeType.End:
                        _reader = null;
                        return canvas;
                    default:
                        _reader = null;
                        throw new InvalidDataException();
                }
            }

            _reader = null;
            throw new InvalidDataException();
        }
    }

    internal class CanvasWriter
    {
        private BinaryWriter _writer;

        private void Write(NativeType type)
        {
            _writer.Write((byte)type);
        }

        private void Write(ref BPoint bpoint)
        {
            _writer.Write(bpoint.Id);
            _writer.Write(bpoint.Connected.Length);
            for (int i = 0; i < bpoint.Connected.Length; i++)
            {
                _writer.Write(bpoint.Connected[i]);
            }
            _writer.Write(bpoint.Point.X);
            _writer.Write(bpoint.Point.Y);
        }

        private void Write(IPoint point)
        {
            _writer.Write(point.Id);
        }

        private void Write(IColor color)
        {
            _writer.Write(color.A);
            _writer.Write(color.R);
            _writer.Write(color.G);
            _writer.Write(color.B);
        }

        private void Write(IPin pin)
        {
            Write(NativeType.Pin);
            _writer.Write(pin.Id);
            Write(pin.Point);
            Write(pin.Shape);
        }

        private void Write(ILine line)
        {
            Write(NativeType.Line);
            _writer.Write(line.Id);
            Write(line.Point1);
            Write(line.Point2);
            Write(line.Stroke);
            _writer.Write(line.StrokeThickness);
        }

        private void Write(IBezier bezier)
        {
            Write(NativeType.Bezier);
            _writer.Write(bezier.Id);
            Write(bezier.Start);
            Write(bezier.Point1);
            Write(bezier.Point2);
            Write(bezier.Point3);
            Write(bezier.Stroke);
            _writer.Write(bezier.StrokeThickness);
            Write(bezier.Fill);
            _writer.Write(bezier.IsFilled);
            _writer.Write(bezier.IsClosed);
        }

        private void Write(IQuadraticBezier quadraticBezier)
        {
            Write(NativeType.QuadraticBezier);
            _writer.Write(quadraticBezier.Id);
            Write(quadraticBezier.Start);
            Write(quadraticBezier.Point1);
            Write(quadraticBezier.Point2);
            Write(quadraticBezier.Stroke);
            _writer.Write(quadraticBezier.StrokeThickness);
            Write(quadraticBezier.Fill);
            _writer.Write(quadraticBezier.IsFilled);
            _writer.Write(quadraticBezier.IsClosed);
        }

        private void Write(IArc arc)
        {
            Write(NativeType.Arc);
            _writer.Write(arc.Id);
            Write(arc.Point1);
            Write(arc.Point2);
            _writer.Write(arc.StartAngle);
            _writer.Write(arc.SweepAngle);
            Write(arc.Stroke);
            _writer.Write(arc.StrokeThickness);
            Write(arc.Fill);
            _writer.Write(arc.IsFilled);
            _writer.Write(arc.IsClosed);
        }

        private void Write(IRectangle rectangle)
        {
            Write(NativeType.Rectangle);
            _writer.Write(rectangle.Id);
            Write(rectangle.Point1);
            Write(rectangle.Point2);
            Write(rectangle.Stroke);
            _writer.Write(rectangle.StrokeThickness);
            Write(rectangle.Fill);
        }

        private void Write(IEllipse ellipse)
        {
            Write(NativeType.Ellipse);
            _writer.Write(ellipse.Id);
            Write(ellipse.Point1);
            Write(ellipse.Point2);
            Write(ellipse.Stroke);
            _writer.Write(ellipse.StrokeThickness);
            Write(ellipse.Fill);
        }

        private void Write(IText text)
        {
            Write(NativeType.Text);
            _writer.Write(text.Id);
            Write(text.Point1);
            Write(text.Point2);
            _writer.Write(text.HorizontalAlignment);
            _writer.Write(text.VerticalAlignment);
            _writer.Write(text.Size);
            _writer.Write(text.Text);
            Write(text.Foreground);
            Write(text.Backgroud);
        }

        private void Write(IList<INative> children)
        {
            int count = children.Count;
            for (int i = 0; i < count; i++)
            {
                Write(children[i]);
            }
        }

        private void Write(INative child)
        {
            if (child is IPin)
            {
                Write(child as IPin);
            }
            else if (child is ILine)
            {
                Write(child as ILine);
            }
            else if (child is IBezier)
            {
                Write(child as IBezier);
            }
            else if (child is IQuadraticBezier)
            {
                Write(child as IQuadraticBezier);
            }
            else if (child is IArc)
            {
                Write(child as IArc);
            }
            else if (child is IRectangle)
            {
                Write(child as IRectangle);
            }
            else if (child is IEllipse)
            {
                Write(child as IEllipse);
            }
            else if (child is IText)
            {
                Write(child as IText);
            }
            else if (child is IBlock)
            {
                Write(child as IBlock);
            }
        }

        private void Write(IBlock block)
        {
            Write(NativeType.Block);
            _writer.Write(block.Id);
            Write(block.Children);
            Write(NativeType.End);
        }

        private void Write(ICanvas canvas)
        {
            Write(NativeType.Canvas);
            _writer.Write(canvas.Id);
            _writer.Write(canvas.Width);
            _writer.Write(canvas.Height);
            Write(canvas.Background);
            _writer.Write(canvas.EnableSnap);
            _writer.Write(canvas.SnapX);
            _writer.Write(canvas.SnapY);
            Write(canvas.Children);
            Write(NativeType.End);
        }

        public void Write(BinaryWriter writer, ref BPoint[] bpoints, ICanvas canvas)
        {
            _writer = writer;

            _writer.Write(bpoints.Length);
            for (int i = 0; i < bpoints.Length; i++)
            {
                Write(ref bpoints[i]);
            }

            Write(canvas);

            _writer = null;
        }
    }

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
            var canvasReader = new CanvasReader();

            using (var reader = new BinaryReader(stream))
            {
                return canvasReader.Read(reader);
            }
        }

        public void Write(Stream stream, ICanvas canvas)
        {
            var idPreprocessor = new IdPreprocessor();
            var bpoints = idPreprocessor.Process(canvas);
            var canvasWriter = new CanvasWriter();

            using (var writer = new BinaryWriter(stream))
            {
                canvasWriter.Write(writer, ref bpoints, canvas);
            }
        }
    }
}
