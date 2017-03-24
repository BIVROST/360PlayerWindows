using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace _3DconnexionDriver
{
    public class _3DxException : Exception
    {
        public _3DxException(string message)
            : base(message)
        {
        }

        public _3DxException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
