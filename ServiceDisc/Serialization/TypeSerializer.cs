using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace ServiceDisc.Serialization
{
    class TypeSerializer
    {
        private static readonly Dictionary<Type,TypeSerialization> _conversionDictionary = new Dictionary<Type, TypeSerialization>();

        static TypeSerializer()
        {
            Add(new TypeSerialization<string>(i => i, i => i));
            Add(new TypeSerialization<bool>(i => i.ToString(CultureInfo.InvariantCulture), i => Convert.ToBoolean(i, CultureInfo.InvariantCulture)));
            Add(new TypeSerialization<int>(i => i.ToString(CultureInfo.InvariantCulture), i => Convert.ToInt32(i, CultureInfo.InvariantCulture)));
            Add(new TypeSerialization<long>(i => i.ToString(CultureInfo.InvariantCulture), i => Convert.ToInt64(i, CultureInfo.InvariantCulture)));
            Add(new TypeSerialization<float>(i => i.ToString("R", CultureInfo.InvariantCulture), i => Convert.ToSingle(i, CultureInfo.InvariantCulture)));
            Add(new TypeSerialization<double>(i => i.ToString("R", CultureInfo.InvariantCulture), i => Convert.ToDouble(i, CultureInfo.InvariantCulture)));
            Add(new TypeSerialization<decimal>(i => i.ToString(CultureInfo.InvariantCulture), i => Convert.ToDecimal(i, CultureInfo.InvariantCulture)));
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

        public string Serialize<T>(T input)
        {
            if (input == null) return null;
            if (typeof(T) == typeof(object) && input.GetType() != typeof(object)) return SerializeWithTypeParameter(input);

            if (_conversionDictionary.TryGetValue(typeof(T), out var serialization))
            {
                if (serialization is TypeSerialization<T> serializer)
                {
                    return serializer.Serialize(input);
                }

                throw new InvalidOperationException($"Serializer is incorrect, expected: {typeof(T).FullName}, actual: {serialization.Type.FullName}");
            }

            return JsonConvert.SerializeObject(input);
        }

        private string SerializeWithTypeParameter(object invocationArgument)
        {
            var genericMethod = typeof(TypeSerializer).GetMethod("Serialize")?.MakeGenericMethod(invocationArgument.GetType());
            if (genericMethod == null) throw new InvalidOperationException("Serialize method required.");

            return (string)genericMethod.Invoke(this, new[] { invocationArgument });
        }
    }
}