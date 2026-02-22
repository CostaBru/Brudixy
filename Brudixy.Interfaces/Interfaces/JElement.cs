using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Brudixy.Interfaces.Interfaces;

namespace Brudixy.Interfaces
{
    [JsonSerializable(typeof(JAttribute))]
    [JsonConverter(typeof(JAttributeConverter))]
    public class JAttribute
    {
        public JAttribute()
        {
        }
        
        public JAttribute(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public JAttribute(JAttribute attribute, Func<object, object> clone)
        {
            Name = attribute.Name;
           
            Value = attribute.Value is ICloneable cl ? cl.Clone() : clone(attribute.Value);
        }
        
        public string Name { get; set; }
        public object Value { get; set; }
    }
    
    [JsonSerializable(typeof(JElement))]
    [JsonConverter(typeof(JElementConverter))]
    public class JElement
    {
        private List<JElement> m_elements = new List<JElement>();
        private List<JAttribute> m_attributes = new List<JAttribute>();

        private static JsonSerializerOptions s_jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

        public JElement()
        {
        }
        
        public JElement(string name, object value = null)
        {
            Name = name;
            Value = value;
        }

        public JElement(JElement element, Func<object, object> clone)
        {
            Name = element.Name;

            Value = element.Value is ICloneable cl ? cl.Clone() : clone(element.Value);

            foreach (var attribute in element.m_attributes)
            {
                m_attributes.Add(new JAttribute(attribute, clone));
            }

            foreach (var el in element.m_elements)
            {
                m_elements.Add(new JElement(el, clone));
            }
        }

        [JsonPropertyOrder(0)]
        public string Name { get; set; }
        
        [JsonPropertyOrder(1)]
        public object Value { get; set; }
        
        [JsonPropertyOrder(2)]
        public List<JAttribute> Attributes
        {
            get => m_attributes;
            set => m_attributes = value;
        }

        [JsonPropertyOrder(3)]
        public List<JElement> Elements
        {
            get => m_elements;
            set => m_elements = value;
        }

        public void AddElement(JElement anotherElement)
        {
            m_elements.Add(anotherElement);
        }

        public void AddAttribute(JAttribute anotherAttribute)
        {
            m_attributes.Add(anotherAttribute);
        }

        public object GetAttribute(string name)
        {
            return m_attributes.FirstOrDefault(a => a.Name == name)?.Value;
        }
        
        public void SetAttribute(string name, string value)
        {
            var attribute = m_attributes.FirstOrDefault(a => a.Name == name);

            if (attribute != null)
            {
                attribute.Value = value;
            }
            else
            {
                m_attributes.Add(new JAttribute(name, value));
            }
        }
        
        public object GetElement(string name)
        {
            return m_elements.FirstOrDefault(a => a.Name == name)?.Value;
        }

        public static JElement Parse(JsonNode value)
        {
            var jElement = JsonSerializer.Deserialize<JElement>(value);

            RestoreValue(jElement);

            return jElement;
        }

        public static JElement Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }
            var node = JsonNode.Parse(json);
            return Parse(node);
        }

        private static void RestoreValue(JElement jElement)
        {
            var queue = new Queue<JElement>();

            queue.Enqueue(jElement);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();

                if (node.Value is JsonElement jn)
                {
                    if (jn.ValueKind == JsonValueKind.Object)
                    {
                        JElement je;

                        node.Value = je = JsonSerializer.Deserialize<JElement>(jn);

                        foreach (var j in je.Elements)
                        {
                            queue.Enqueue(j);
                        }
                    }
                    else
                    {
                        node.Value = jn.ToString();
                    }
                }
                
                foreach (var j in node.Elements)
                {
                    queue.Enqueue(j);
                }
                
                foreach (var a in node.Attributes)
                {
                    if (a.Value is JsonElement jna)
                    {
                        if (jna.ValueKind == JsonValueKind.Object)
                        {
                            JElement je;

                            a.Value = je = JsonSerializer.Deserialize<JElement>(jna);

                            foreach (var j in je.Elements)
                            {
                                queue.Enqueue(j);
                            }
                        }
                        else
                        {
                            a.Value = jna.ToString();
                        }
                    }
                }
            }
        }

        public static JsonNode ToJson(JElement el, JsonSerializerOptions options = null)
        {
            var optionsToWrite = options ?? s_jsonSerializerOptions;
            
            return JsonSerializer.SerializeToNode(el, optionsToWrite);
        }

        public override string ToString()
        {
            return ToJson(this).ToString();
        }
        
        public string ToString(JsonSerializerOptions options)
        {
            return ToJson(this, options).ToString();
        }
    }
}