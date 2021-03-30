using System;
using System.Collections.Generic;
using System.Text;

namespace JackCompiler
{
    class CompilerException : Exception
    {
       public CompilerException(string expected, int linecompiling)
       {
            string Message = "Expected " + expected + " on Line " + linecompiling;
            Console.WriteLine(Message);
            throw new ArgumentException(Message);
       }
    }
}
