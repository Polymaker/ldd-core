using System;
using System.Runtime.InteropServices;

namespace LDD.Common.IO
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class EncodingAttribute : Attribute
    {
        public CharSet CharSet { get; set; }

        public EncodingAttribute(CharSet charSet)
        {
            CharSet = charSet;
        }
    }
}
