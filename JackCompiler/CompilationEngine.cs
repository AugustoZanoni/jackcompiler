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
                throw ex;
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
                            case "field":
                                CompileClassVarDec(tokenizer);
                                break;
                            case "constructor":
                            case "function":
                            case "method":
                                CompileSubroutine(tokenizer);
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
            if(tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && Regex.IsMatch(tokenizer.token().content.ToLower(), "int|char|boolean|className")) //Verificar Possibilidade de ClassName (Não COntempla) type
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

        public void CompileSubroutine(JackTokenizer tokenizer)
        {
            XElement Subroutine = new XElement("subroutineDec");
            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && Regex.IsMatch(tokenizer.token().content.ToLower(), "(constructor|function|method)"))
                Subroutine.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && Regex.IsMatch(tokenizer.token().content.ToLower(), "int|char|boolean|void|className")) //Verificar Possibilidade de ClassName (Não COntempla) type
                Subroutine.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
                Subroutine.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "(")
                Subroutine.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();

            
            if (tokenizer.token().content != ")")
            {
                XElement ParameterList;
                CompileParameterList(tokenizer, out ParameterList);
                Subroutine.Add(ParameterList);
            }


            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ")")
                Subroutine.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "{");
            else throw new CompilerException();

            XElement SubroutineBody = new XElement("subroutineBody");
            SubroutineBody.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            tokenizer.advance();

            //VarDec
            while (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && tokenizer.token().content == "var")
            {
                XElement VarDec;
                CompileVarDec(tokenizer, out VarDec);
                SubroutineBody.Add(VarDec);
            }

            //Subroutine
            if (tokenizer.token().content != "}")
            {
                XElement Statement;
                compileStatements(tokenizer, out Statement);
                SubroutineBody.Add(Statement);
            }

            SubroutineBody.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            Subroutine.Add(SubroutineBody);

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "}") 
                SubroutineBody.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            Subroutine.WriteTo(writer);
        }

        public void CompileParameterList(JackTokenizer tokenizer, out XElement ParameterList)
        {
            ParameterList = new XElement("parameterList");
            while (tokenizer.token().content != ")")
            {
                ParameterList.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                tokenizer.advance();
            }
        }

        public void CompileVarDec(JackTokenizer tokenizer, out XElement VarDec)
        {
            VarDec = new XElement("varDec");

            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && tokenizer.token().content == "var")
                VarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
            if (Regex.IsMatch(tokenizer.token().content.ToLower(), "int|char|boolean") || tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
                VarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
                VarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
            while(tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ",")
            {
                VarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                tokenizer.advance();
                if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
                    VarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                else
                    throw new CompilerException();
                tokenizer.advance();
            }

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ";")
                VarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
        }

        public void compileStatements(JackTokenizer tokenizer, out XElement Statements)
        {
            Statements = new XElement("statements");
            while(tokenizer.hasMoreTokens() && tokenizer.token().content != "}")
            {
                switch (tokenizer.token().content.ToLower())
                {
                    case "let":
                        break;
                    case "if":
                        break;
                    case "while":
                        break;
                    case "do":
                        XElement Do;
                        compileDo(tokenizer,out Do);
                        Statements.Add(Do);
                        break;
                    case "return":
                        XElement returnStatement;
                        compileReturn(tokenizer, out returnStatement);
                        Statements.Add(returnStatement);
                        break;
                    default:
                        break;
                }
                tokenizer.advance();
            }
        }

        public void CompileExpression(JackTokenizer tokenizer, out XElement Expression)
        {
            Expression = new XElement("expression");
            XElement term = new XElement("term");
            if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER || 
                tokenizer.token().type == JackTokenizer.Token.Type.INTEGER_CONSTANT ||
                tokenizer.token().type == JackTokenizer.Token.Type.STRING_CONSTANT ||
                tokenizer.token().content == "this" 
                )
                term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            Expression.Add(term);
            //tokenizer.advance();          
        }

        public void CompileExpressionList(JackTokenizer tokenizer, out XElement expressionList)
        {
            expressionList = new XElement("expressionList");
            while (tokenizer.token().type != JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content != ")")
            {
                XElement expression;
                CompileExpression(tokenizer, out expression);
                tokenizer.advance();
                if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content != ",")
                {
                    expressionList.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                    tokenizer.advance();
                    if (tokenizer.token().type != JackTokenizer.Token.Type.IDENTIFIER)
                        throw new CompilerException();
                }
                expressionList.Add(expression);
            }
        }

        public void compileDo(JackTokenizer tokenizer, out XElement Do)
        {
            Do = new XElement("doStatement");
            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && tokenizer.token().content == "do")
                Do.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
                Do.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ".")
            {
                Do.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                tokenizer.advance();
                if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
                    Do.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                else
                    throw new CompilerException();
                tokenizer.advance();
            }
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "(")
                Do.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
            XElement expressionList;
            CompileExpressionList(tokenizer, out expressionList);           
            Do.Add(expressionList);
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ")")
                Do.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ";")
                Do.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            //tokenizer.advance();
        }

        public void compileReturn(JackTokenizer tokenizer, out XElement returnStatement)
        {
            returnStatement = new XElement("returnStatement");
            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && tokenizer.token().content == "return")
                returnStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            if(tokenizer.token().type != JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ";")
            {
                XElement expression;
                //XElement expression = new XElement("expression");
                //XElement term = new XElement("term");
                //if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
                //    term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                //else
                //    throw new CompilerException();
                //expression.Add(term);
                CompileExpression(tokenizer, out expression);
                returnStatement.Add(expression);
            }
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ";")
                returnStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
        }
    }
}
