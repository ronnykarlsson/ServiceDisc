using System;

namespace ServiceDisc
{
    public interface IHost
    {
        string Type { get; }
        string Address { get; }
    }
}