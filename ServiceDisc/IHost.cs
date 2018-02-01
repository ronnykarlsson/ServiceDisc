using System;

namespace ServiceDisc
{
    public interface IHost
    {
        string Type { get; }
        Uri Uri { get; }
    }
}