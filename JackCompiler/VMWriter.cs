using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace JackCompiler
{
    class VMWriter
    {
        public enum Segments
        {
            CONSTANT = 0,
            ARGUMENT = 1,
            LOCAL = 2,
            STATIC = 3,
            THIS = 4,
            THAT = 5,
            POINTER = 6,
            TEMP = 7,
        };

        public enum Commands
        {
            ADD = 0,
            SUB = 1,
            NEG = 2,
            EQ = 3,
            GT = 4,
            LT = 5,
            AND = 6,
            OR = 7,
            NOT = 8,
        };

        public StreamWriter codeWriter;
        public VMWriter(string outputfile)
        {
            outputfile = outputfile.Replace(".jack", ".vm");
            codeWriter = File.CreateText(outputfile);
        }

        public void WritePush(Segments segment, Symbol symbol)
        {            
            this.codeWriter.WriteLine("push "+segment.ToString().ToLower()+ " "+symbol.index); 
        }
        public void WritePop(Segments segment, Symbol symbol)
        {
            this.codeWriter.WriteLine("pop " + segment.ToString().ToLower() + " " + symbol.index);
        }
        public void WriteArithmetic(Commands command)
        {
            this.codeWriter.WriteLine(command.ToString().ToLower()) ;
        }
        public void WriteCall(string name, int nArgs)
        {
            this.codeWriter.WriteLine($"Hello {name} {nArgs}");
        }
        public void WriteFunction(Symbol symbol)
        {

        }
        public void WriteReturn()
        {
            this.codeWriter.WriteLine("return");
        }
        public void WriteLabel(string label)
        {
            this.codeWriter.WriteLine("label " + label);
        }
        public void WriteGoto(string label)
        {
            this.codeWriter.WriteLine("goto " + label);
        }
        public void WriteIf(string label)
        {
            this.codeWriter.WriteLine("if-goto " + label);
        }
        public void close()
        {
            codeWriter.Flush();
            codeWriter.Close();
        }


    }
}
