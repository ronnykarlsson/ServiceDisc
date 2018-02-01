using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ServiceDisc.Serialization
{
    class TypeSerializer
    {
        private static readonly Dictionary<Type,TypeSerialization> _conversionDictionary = new Dictionary<Type, TypeSerialization>();

        static TypeSerializer()
        {
            Add(new TypeSerialization(typeof(string), i => i));
            Add(new TypeSerialization(typeof(bool), i => Convert.ToBoolean(i)));
            Add(new TypeSerialization(typeof(int), i => Convert.ToInt32(i)));
            Add(new TypeSerialization(typeof(long), i => Convert.ToInt64(i)));
            Add(new TypeSerialization(typeof(float), i => Convert.ToSingle(i)));
            Add(new TypeSerialization(typeof(double), i => Convert.ToDouble(i)));
            Add(new TypeSerialization(typeof(decimal), i => Convert.ToDecimal(i)));
        }

        static void Add(TypeSerialization serialization)
        {
            _conversionDictionary.Add(serialization.Type, serialization);
        }

        public object Deserialize(string input, Type targetType)
        {
            if (targetType == typeof(void)) return null;

            TypeSerialization serialization;
            if (!_conversionDictionary.TryGetValue(targetType, out serialization))
            {
                return JsonConvert.DeserializeObject(input, targetType);
            }

            return serialization.Deserialize(input);
        }

        public string Serialize(object input)
        {
            if (_conversionDictionary.ContainsKey(input.GetType()))
            {
                return input.ToString();
            }

            return JsonConvert.SerializeObject(input);
        }
    }
}