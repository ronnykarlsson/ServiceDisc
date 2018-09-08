using System;

namespace ServiceDisc.Serialization
{
    public class TypeSerialization
    {
        public Type Type { get; }
        public Func<string, object> Deserialize { get; }

        public TypeSerialization(Type type, Func<string, object> deserialize)
        {
            Type = type;
            Deserialize = deserialize;
        }
    }

    public class TypeSerialization<T> : TypeSerialization
    {
        public Func<T, string> Serialize { get; }

        public TypeSerialization(Func<T, string> serialize, Func<string, object> deserialize) : base(typeof(T), deserialize)
        {
            Serialize = serialize;
        }
    }
}