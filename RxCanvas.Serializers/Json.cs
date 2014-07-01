using Newtonsoft.Json;
using RxCanvas.Interfaces;
using RxCanvas.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;

namespace RxCanvas.Serializers
{
    public class JsonSerializationBinder : SerializationBinder
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

    public class ColorJsonConverter : JsonConverter
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

    public class PointJsonConverter : JsonConverter
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
    
    [Export(typeof(IFile))]
    public class JsonFile : IFile
    {
        public string Name { get; set; }
        public string Extension { get; set; }

        private JsonSerializerSettings Settings = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            Binder = new JsonSerializationBinder(),
            NullValueHandling = NullValueHandling.Ignore,
            Converters =  { new ColorJsonConverter(), new PointJsonConverter() }
        };

        public JsonFile()
        {
            Name = "Json";
            Extension = "json";
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
            using (var reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<XCanvas>(json, Settings);
            }
        }

        public void Write(Stream stream, ICanvas canvas)
        {
            using (var writer = new StreamWriter(stream))
            {
                string json = JsonConvert.SerializeObject(canvas, Formatting.Indented, Settings);
                writer.Write(json);
            }
        }
    }
}
