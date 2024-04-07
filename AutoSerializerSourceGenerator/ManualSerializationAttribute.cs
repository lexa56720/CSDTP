using System;
using System.Collections.Generic;
using System.Text;

namespace AutoSerializerSourceGenerator
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ManualSerializationAttribute : Attribute
    {
    }
}
