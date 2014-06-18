using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RxCanvas.Core;
using RxCanvas.Model;
using Newtonsoft.Json;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization;
using System.Globalization;

namespace RxCanvas.Serializers
{
    public class XModelSerializationBinder : SerializationBinder
    {
        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.Name.Substring(1, serializedType.Name.Length - 1);
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
            string resolvedTypeName = string.Format("RxCanvas.Model.{0}, RxCanvas.Model", 'X' + typeName);
            return Type.GetType(resolvedTypeName, true);
        }
    }

    public class XColorConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IColor) || objectType == typeof(XColor);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var color = (IColor)value;
            string hex = string.Concat('#', color.A.ToString("X2"), color.R.ToString("X2"), color.G.ToString("X2"), color.B.ToString("X2"));
            writer.WriteValue(hex);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(IColor))
            {
                string value = (string)reader.Value;
                return new XColor(byte.Parse(value.Substring(1, 2), NumberStyles.HexNumber),
                    byte.Parse(value.Substring(3, 2), NumberStyles.HexNumber),
                    byte.Parse(value.Substring(5, 2), NumberStyles.HexNumber),
                    byte.Parse(value.Substring(7, 2), NumberStyles.HexNumber));
            }
            throw new ArgumentException("objectType");
        }
    }

    public class XPointConverter : JsonConverter
    {
        private static char[] Separators = new char[] { ';' };

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IPoint) || objectType == typeof(XPoint);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var point = (IPoint)value;
            string hex = string.Concat(point.X.ToString(), Separators[0], point.Y.ToString());
            writer.WriteValue(hex);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(IPoint))
            {
                string value = (string)reader.Value;
                string[] values = value.Split(Separators);
                return new XPoint(double.Parse(values[0]), double.Parse(values[1]));
            }
            throw new ArgumentException("objectType");
        }
    }

    public class JsonXModelSerializer : ISerializer<ICanvas>
    {
        public string Name { get; set; }
        public string Extension { get; set; }

        private JsonSerializerSettings Settings = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            Binder = new XModelSerializationBinder(),
            NullValueHandling = NullValueHandling.Ignore,
            Converters =  { new XColorConverter(), new XPointConverter() }
        };

        public JsonXModelSerializer()
        {
            Name = "Json";
            Extension = "json";
        }

        public string Serialize(ICanvas canvas)
        {
            return JsonConvert.SerializeObject(canvas, Formatting.Indented, Settings);
        }

        public ICanvas Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<XCanvas>(json, Settings);
        }
    }
}
