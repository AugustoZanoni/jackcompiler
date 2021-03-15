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

        public void CompileParameterListNovo(JackTokenizer tokenizer, out XElement ParameterList)
        {
            ParameterList = new XElement("parameterList");
            while (tokenizer.token().content != "]")
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

        public void CompileLet(JackTokenizer tokenizer, out XElement letStatement)
        {
            letStatement = new XElement("letStatements");

            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && tokenizer.token().content == "let")
                letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
                letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
            if (tokenizer.token().content == "[")
            {
                if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "[")
                    letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                else
                    throw new CompilerException();
                tokenizer.advance();

                if (tokenizer.token().content != "]")
                {
                    XElement ParameterList;
                    CompileParameterListNovo(tokenizer, out ParameterList);
                    letStatement.Add(ParameterList);
                }

                if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "]")
                    letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                else
                    throw new CompilerException();
                tokenizer.advance();

            } 
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "=")
                letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
            switch (tokenizer.token().type)
            {
                case JackTokenizer.Token.Type.IDENTIFIER:
                    if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
                        letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                    else
                        throw new CompilerException();
                    tokenizer.advance();
                    if(tokenizer.token().content == ".")
                    {
                        if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ".")
                            letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                        else
                            throw new CompilerException();
                        tokenizer.advance();
                        if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
                            letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                        else
                            throw new CompilerException();
                        tokenizer.advance();
                        if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "(")
                            letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                        else
                            throw new CompilerException();
                        tokenizer.advance();
                        if (tokenizer.token().content != ")")
                        {
                            XElement ParameterList;
                            CompileParameterList(tokenizer, out ParameterList);
                            letStatement.Add(ParameterList);
                        }
                        if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ")")
                            letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                        else
                            throw new CompilerException();
                        tokenizer.advance();
                    }
                    if (tokenizer.token().content == "[")
                    {
                        if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "[")
                            letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                        else
                            throw new CompilerException();
                        tokenizer.advance();

                        if (tokenizer.token().content != "]")
                        {
                            XElement ParameterList;
                            CompileParameterListNovo(tokenizer, out ParameterList);
                            letStatement.Add(ParameterList);
                        }

                        if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "]")
                            letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                        else
                            throw new CompilerException();
                        tokenizer.advance();

                    }
                    if(tokenizer.token().content == "+" || tokenizer.token().content == "-" || tokenizer.token().content == "*" || tokenizer.token().content == "/" || tokenizer.token().content == "|")
                    {
                        // Aqui precisa de correção pra tratar erros com as expressões algebricas
                        while(tokenizer.token().content != ";")
                        {
                            letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                            tokenizer.advance();
                        }
                    }
                    break;
                case JackTokenizer.Token.Type.INTEGER_CONSTANT:
                    if (tokenizer.token().type == JackTokenizer.Token.Type.INTEGER_CONSTANT)
                        letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                    else
                        throw new CompilerException();
                    tokenizer.advance();
                    break;
                case JackTokenizer.Token.Type.STRING_CONSTANT:
                    if (tokenizer.token().type == JackTokenizer.Token.Type.STRING_CONSTANT)
                        letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                    else
                        throw new CompilerException();
                    tokenizer.advance();
                    break;
                case JackTokenizer.Token.Type.KEYWORD:
                    // NESSE CASO, EXISTE PROBLEMAS EM RELAÇÃO A ACEITAÇÃO DE OUTRAS PALAVRAS CHAVES
                    if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD)
                        letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                    else
                        throw new CompilerException();
                    tokenizer.advance();
                    break;
                default:
                    break;

            }
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ";")
                letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
        }

        public void CompileIf(JackTokenizer tokenizer, out XElement ifStatement)
        {
            ifStatement = new XElement("ifStatements");
            
            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && tokenizer.token().content == "if")
                ifStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "(")
                ifStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();

            
            if (tokenizer.token().content != ")")
            {
                XElement ParameterList;
                CompileParameterList(tokenizer, out ParameterList);
                ifStatement.Add(ParameterList);
            }


            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ")")
                ifStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
            
            XElement ifBody = new XElement("ifBody");

            statementBody(tokenizer, ifBody); 

            ifBody.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            ifStatement.Add(ifBody);

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "}") 
                ifStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();

            if(tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && tokenizer.token().content == "else")
            {
                XElement Statement;
                CompileElse(tokenizer, out Statement);
                ifStatement.Add(Statement);
            }
        }

        public void CompileElse(JackTokenizer tokenizer, out XElement elseStatement)
        {
            elseStatement = new XElement("elseStatements");

            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && tokenizer.token().content == "else")
                elseStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();

            XElement elseBody = new XElement("elseBody");
            statementBody(tokenizer, elseBody);            

            elseBody.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            elseStatement.Add(elseBody);

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "}") 
                elseStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
        }

        public void CompileWhile(JackTokenizer tokenizer, out XElement whileStatement)
        {
            whileStatement = new XElement("whileStatement");
            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && tokenizer.token().content == "while")
                whileStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "(")
                whileStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();

            
            if (tokenizer.token().content != ")")
            {
                XElement ParameterList;
                CompileParameterList(tokenizer, out ParameterList);
                whileStatement.Add(ParameterList);
            }


            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ")")
                whileStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();
            
            XElement whileBody = new XElement("whileBody");

            statementBody(tokenizer, whileBody); 

            whileBody.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            whileStatement.Add(whileBody);

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "}") 
                whileStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException();
            tokenizer.advance();

        }

        public void statementBody(JackTokenizer tokenizer, XElement Body)
        {
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "{")
                Body.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else throw new CompilerException();
            
            tokenizer.advance();

            //Let
            while (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && tokenizer.token().content == "let")
            {
                XElement letStatement;
                CompileLet(tokenizer, out letStatement);
                Body.Add(letStatement);
            }

            //VarDec
            while (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && tokenizer.token().content == "var")
            {
                XElement VarDec;
                CompileVarDec(tokenizer, out VarDec);
                Body.Add(VarDec);
            }

            //Subroutine
            if (tokenizer.token().content != "}")
            {
                XElement Statement;
                compileStatements(tokenizer, out Statement);
                Body.Add(Statement);
            }

        }

        public void compileStatements(JackTokenizer tokenizer, out XElement Statements)
        {
            Statements = new XElement("statements");
            while(tokenizer.hasMoreTokens() && tokenizer.token().content != "}")
            {
                switch (tokenizer.token().content.ToLower())
                {
                    case "let":
                        //CompileLet(tokenizer, out Statements);
                        break;
                    case "if":
                        //CompileIf(tokenizer, out Statements);
                        break;
                    case "while":
                        CompileWhile(tokenizer, out Statements);
                        Console.WriteLine(tokenizer.token().content.ToLower() + " !!!!!!!!!!!!!!!!!!!!!!!!");
                        break;
                    case "do":
                        break;
                    case "return":
                        break;
                    default:
                        break;
                }
                tokenizer.advance();
            }
        }
    }
}
