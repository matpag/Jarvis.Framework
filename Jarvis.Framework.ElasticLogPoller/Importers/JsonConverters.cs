﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.Framework.ElasticLogPoller.Importers
{
    public class ImporterConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader,
                                        Type objectType,
                                         object existingValue,
                                         JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);
            BaseImporter retValue;
            switch (jObject["Type"].Value<String>())
            {
                case "mongo":
                    retValue = new MongoImporter();
                    break;

                default:
                    throw new Exception("Unknown type " + jObject["Type"]);
            }

            foreach (var property in jObject.Properties().Where(p => p.Name != "Type"))
            {
                retValue(property.Name, property.Value.Value<String>());
            }

            return retValue;
        }

        public override void WriteJson(JsonWriter writer,
                                       object value,
                                       JsonSerializer serializer)
        {
            writer.WriteStartObject();
            var properties = value.GetType().GetProperties();
            foreach (var property in properties)
            {
                writer.WritePropertyName(property.Name);
                writer.WriteValue(property.GetValue(value));
            }
            writer.WriteEndObject();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(BaseImporter).IsAssignableFrom(objectType);
        }
    }

}
