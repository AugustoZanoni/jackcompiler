using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace JackCompiler
{
    class VMWriter
    {
        public StreamWriter codeWriter;
        public VMWriter(string outputfile)
        {
            outputfile = outputfile.Replace(".jack", ".vm");
            codeWriter = File.CreateText(outputfile);
        }

        public void WritePush(Symbol symbol)
        {

        }
        public void WritePop(Symbol symbol)
        {

        }
        public void WriteArithmetic(Symbol symbol)
        {

        }
        public void WriteCall(Symbol symbol)
        {

        }
        public void WriteFunction(Symbol symbol)
        {

        }
        public void WriteReturn()
        {
            codeWriter.WriteLine("return");
        }
        public void WriteLabel(Symbol symbol)
        {

        }
        public void WriteGoto(Symbol symbol)
        {

        }
        public void WriteIf(Symbol symbol)
        {

        }
        public void close()
        {
            codeWriter.Flush();
            codeWriter.Close();
        }


    }
}
