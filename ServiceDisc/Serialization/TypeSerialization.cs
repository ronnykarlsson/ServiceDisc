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
}