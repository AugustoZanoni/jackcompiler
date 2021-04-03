using System;
using System.Collections.Generic;
using System.Text;

namespace JackCompiler
{
    class SymbolTable
    {
        
        public string name { get { return name; } set { name = value; } }
        public string type { get { return type; } set { type = value; } }
        public string kind { get { return kind; } set { kind = value; } }
        public int index { get { return index; } set { index = value; } }
                
    }
}
