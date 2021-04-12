using System;
using System.Collections.Generic;
using System.Text;

namespace JackCompiler
{
    class Symbol
    {
        private string _name = "";
        public string name { get { return _name; } set { _name = value; } }
        private string _type = "";
        public string type { get { return _type; } set { _type = value; } }
        private string _kind = "";
        public string kind { get { return _kind; } set { _kind = value; } }
        private int _index = 0;
        public int index { get { return _index; } set { _index = value; } }          

        public Symbol() { }
    }
    class SymbolTable
    {
       

        public List<Symbol> classScope = new List<Symbol>();
        public List<Symbol> localScope = new List<Symbol>();
        public int count = 0;
        public SymbolTable() { }

        public void Add(Symbol symbol)
        {
            symbol.index = count;
            if (symbol.kind == "static" || symbol.kind == "field")
                classScope.Add(symbol);
            else
                localScope.Add(symbol);

            count++;
        }

        // CORRIGIR - MODIFICADO POR RICARDO
        
        public int LocalScopeLenght(){
            return count;
        }

        // CORRIGIR - MODIFICADO POR RICARDO

        public void FinishLocalScope()
        {
            localScope.Clear();
        }
        public void FinishClassScope()
        {
            classScope.Clear();
        }

        public Symbol findSymbol(string name)
        {
            var element = localScope.Find(e => e.name == name);
            foreach (var nomelus in localScope){
                Console.WriteLine(nomelus.name);
            }
            if (element == null) element = classScope.Find(e => e.name == name); 


            if (element == null) throw new Exception(name + " variable not found");
            

            return element;

        }
    }
}
