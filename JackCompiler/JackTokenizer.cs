using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace JackCompiler
{
    public class JackTokenizer
    {
        string JackContent { get; set; }
        string ActualToken { get; set; }
        public int LineCompiling = 0;

        Queue<string> JackTokensInLine = new Queue<string>();
        string[] JackLines { get; set; }
        public JackTokenizer(string fname)
        {
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
                throw e;
                JackContent = "";
            }
        }
        public void advance()
        {

            if (!(JackTokensInLine.Count > 0))
            {
                //verifica Linhas em branco
                if (JackLines[LineCompiling] == "")
                {
                    if ((LineCompiling + 1) < (JackLines.Length))
                    {
                        LineCompiling++;
                        advance();
                    }else
                        LineCompiling++;
                }
                else
                {
                    foreach (Match Token in Regex.Matches(JackLines[LineCompiling], @"("".*"")|[a-zA-Z_]+[a-zA-Z0-9_]*|[0-9]+|[+|*|/|\-|{|}|(|)|\[|\]|\.|,|;|<|>|=|~]"))
                    {
                        JackTokensInLine.Enqueue(Token.Value);
                    }
                    LineCompiling++;
                    ActualToken = JackTokensInLine.Dequeue();
                }
            }
            else
                ActualToken = JackTokensInLine.Dequeue();
        }
        public bool hasMoreTokens()
        {
            if (LineCompiling < JackLines.Length)
                return true;
            else
                return false;
        }
        public Token token()
        {
            return new Token(ActualToken);
        }

        public class Token
        {
            public enum Type
            {
                UNIDENTIFIED_TOKEN,
                KEYWORD,
                IDENTIFIER,
                INTEGER_CONSTANT,
                STRING_CONSTANT,
                SYMBOL,
            }

            public Type type;

            public string content;
            public Token(string token)
            {
                content = token;
                //type = "unkown";
                if (token != null)
                {
                    if (type == 0) findKeyword(token);
                    if (type == 0) findStringVal(token);
                    if (type == 0) findIdentifier(token);
                    if (type == 0) findIntVal(token);
                    if (type == 0) fundSymbol(token);
                    if (type == 0) type = Type.UNIDENTIFIED_TOKEN;
                }
            }
            public void findKeyword(string token)
            {
                string keywords = "class|constructor|function|" +
                                  "method|field|static|var|int|" +
                                  "char|boolean|void|true|false|" +
                                  "null|this|let|do|if|else|" +
                                  "while|return";
                if (Regex.IsMatch(token, keywords)) type = Type.KEYWORD;
            }
            public void fundSymbol(string token)
            {
                string symbols = @"^({|}|(|)|\[|\]|.|,|;|\+|-|\*|\/|&||<|>|=|~)$";
                if (Regex.IsMatch(token, symbols)) type = Type.SYMBOL;
            }
            public void findIdentifier(string token)
            {
                string identifier = "[a-zA-Z_]+[a-zA-Z0-9_]*";
                if (Regex.IsMatch(token, identifier)) type = Type.IDENTIFIER;
            }
            public void findIntVal(string token)
            {
                string integer = "[0-9]+";
                if (Regex.IsMatch(token, integer)) type = Type.INTEGER_CONSTANT;
            }
            public void findStringVal(string token)
            {
                if (Regex.IsMatch(token, "(\".*\")")) type = Type.STRING_CONSTANT;
            }
        }

    }

}
