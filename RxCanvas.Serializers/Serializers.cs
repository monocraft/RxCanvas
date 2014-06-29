using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization;
using System.Globalization;
using RxCanvas.Interfaces;
using RxCanvas.Model;

namespace RxCanvas.Serializers
{
    public class XSerializationBinder : SerializationBinder
    {
        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.Name.Substring(1, serializedType.Name.Length - 1);
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
            string resolvedTypeName = string.Format("RxCanvas.Model.{0}, RxCanvas.Shared", 'X' + typeName);
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
            writer.WriteValue(((IColor)value).ToHtml());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(IColor))
            {
                return ((string)reader.Value).FromHtml();
            }
            throw new ArgumentException("objectType");
        }
    }

    public class XPointConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IPoint) || objectType == typeof(XPoint);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((IPoint)value).ToText());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(IPoint))
            {
                return ((string)reader.Value).FromText();
            }
            throw new ArgumentException("objectType");
        }
    }

    public class XJsonSerializer : ISerializer<ICanvas>
    {
        public string Name { get; set; }
        public string Extension { get; set; }

        private JsonSerializerSettings Settings = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            Binder = new XSerializationBinder(),
            NullValueHandling = NullValueHandling.Ignore,
            Converters =  { new XColorConverter(), new XPointConverter() }
        };

        public XJsonSerializer()
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
