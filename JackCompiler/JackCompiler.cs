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
                    writer.WriteElementString(tknz.token().type, tknz.token().content);
                    tknz.advance();
                }
                writer.WriteEndElement();
                writer.Flush();
            }

        }
    }
    public class JackTokenizer
    {
        string JackContent { get; set; }
        string ActualToken { get; set; }
        int LineCompiling = 0;

        Stack<string> JackTokensInLine = new Stack<string>();
        string[] JackLines { get; set; }
        public JackTokenizer(string fname) {
            try
            {
                using (var sr = new StreamReader(fname))
                {
                    JackContent = sr.ReadToEnd();
                    JackLines = JackContent.Split(
                        new[] { "\r\n", "\r", "\n" },
                        StringSplitOptions.None
                    );
                    for (int i = 0; i < JackLines.Length; i++)
                    {
                        JackLines[i] = Regex.Replace(JackLines[i], @"(//.*)|(/\*(.|\n)*?\*/)", " ").Trim();

                    }                    
                }
            }
            catch (IOException e)
            {
                JackContent = "";
            }
        }
        public void advance() {

            if (!(JackTokensInLine.Count > 0))
            {
                //verifica Linhas em branco
                if (JackLines[LineCompiling] == "")
                {
                    LineCompiling++;
                    advance();
                }
                else
                {
                    foreach (string Token in JackLines[LineCompiling].Split(" "))
                    {
                        JackTokensInLine.Push(Token);
                    }
                    LineCompiling++;
                }
            }
            else
            {
                ActualToken = JackTokensInLine.Pop();
            }
        }
        public bool hasMoreTokens() {
            if (  LineCompiling < JackLines.Length)
                return true;
            else
                return false;        
        }
        public Token token() {
            return new Token(ActualToken);
        }

        public class Token {
            public string type;

            public string content;
            public Token(string token) {
                content = token;
                type = "unkown";
                if (token != null)
                {
                    findKeyword(token);
                    fundSymbol(token);
                    findIdentifier(token);
                    findIntVal(token);
                    findStringVal(token);
                }
            }
            public void findKeyword(string token) {
                string keywords = "class|constructor|function|"+
                                  "method|field|static|var|int|"+
                                  "char|boolean|void|true|false|"+
                                  "null|this|let|do|if|else|"+
                                  "while|return";
                if (Regex.IsMatch(token, keywords)) type = "Keyword";                
            }
            public void fundSymbol(string token) {
                string symbols = @"^({|}|(|)|\[|\]|.|,|;|\+|-|\*|\/|&||<|>|=|~)$";
                if (Regex.IsMatch(token, symbols)) type = "Symbbol";
            }
            public void findIdentifier(string token) {  }
            public void findIntVal(string token) {  }
            public void findStringVal(string token) {
                if (Regex.IsMatch(token, "(\".*\")")) type = "stringConst";


            }
        }

    }

}
