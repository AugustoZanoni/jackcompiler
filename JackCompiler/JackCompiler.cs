using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace JackCompiler
{

    public class JackCompiler
    {
        static void Main(string[] args)
        {
            new CompilationEngine("nandtotetris/11/Average/Main.jack"); //  Pasta 11
            //new CompilationEngine("nandtotetris/11/Seven/Main.jack"); // Pasta 11
            //RunJackTokenizer();
        }

        public static void RunJackTokenizer()
        {
            JackTokenizer tknz = new JackTokenizer("main.jack");

            var settings = new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "    "
            };
            using (XmlWriter writer = XmlWriter.Create(Console.Out, settings))
            {
                tknz.advance();
                writer.WriteStartElement("tokens");
                while (tknz.hasMoreTokens())
                {
                    writer.WriteElementString(tknz.token().type.ToString(), tknz.token().content);
                    tknz.advance();
                }
                writer.WriteEndElement();
                writer.Flush();
            }
        }
    }

}
