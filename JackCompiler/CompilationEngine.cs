using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace JackCompiler
{
    class CompilationEngine
    {
        static protected XmlWriterSettings settings = new XmlWriterSettings()
        {
            Indent = true,
            IndentChars = "    "
        };
        public XmlWriter writer = XmlWriter.Create(Console.Out, settings);
        public CompilationEngine(string path)
        {
            try
            {
                JackTokenizer Tokenizer = new JackTokenizer(path);
                Tokenizer.advance();
                CompileClass(Tokenizer);
            }
            catch(Exception ex)
            {

            }
        }

        public void CompileClass(JackTokenizer tokenizer)
        {
            if (tokenizer.token().content.ToLower() == "class")
            {
                writer.WriteStartElement("class");
                writer.WriteElementString(tokenizer.token().type.ToString(), tokenizer.token().content);
                tokenizer.advance();
                if(tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
                    writer.WriteElementString(tokenizer.token().type.ToString(), tokenizer.token().content);
                else
                    throw new CompilerException();
                tokenizer.advance();
                if(tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL)
                    writer.WriteElementString(tokenizer.token().type.ToString(), tokenizer.token().content);
                else
                    throw new CompilerException();

                while (tokenizer.hasMoreTokens())
                {
                    tokenizer.advance();
                    if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD)
                    {
                        switch (tokenizer.token().content.ToLower())
                        {
                            case "static":
                                CompileClassVarDec(tokenizer);
                                break;
                            case "field":
                                CompileClassVarDec(tokenizer);
                                break;
                            default:
                                break;
                        }
                    }
                    //else 
                    //    throw new CompilerException();
                }

                //Corrigir
                writer.WriteElementString("SYMBOL", "}");

                writer.WriteEndElement();
                writer.Flush();
            } else {
                throw new CompilerException();
            }
        }

        public void CompileClassVarDec(JackTokenizer tokenizer)
        {
            XElement classVarDec = new XElement("classVarDec");
            if(tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && Regex.IsMatch( tokenizer.token().content.ToLower(), "(static|field)"))
                classVarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
            if(tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD) //Verificar Possibilidade de ClassName (Não COntempla)
                classVarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER) 
                classVarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL) 
                classVarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            classVarDec.WriteTo(writer);

            //XElement child2 = new XElement("AnotherChild",
            //    new XElement("GrandChild", "different content")
            //);
            //child2.WriteTo(writer);
        }
    }
}
