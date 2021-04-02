﻿using System;
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
                    throw new CompilerException("class", tokenizer.LineCompiling);
                tokenizer.advance();
                if(tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL)
                    writer.WriteElementString(tokenizer.token().type.ToString(), tokenizer.token().content);
                else
                    throw new CompilerException("(", tokenizer.LineCompiling);

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
                throw new CompilerException("class", tokenizer.LineCompiling);
            }
        }

        public void CompileClassVarDec(JackTokenizer tokenizer)
        {
            XElement classVarDec = new XElement("classVarDec");
            if(tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && Regex.IsMatch( tokenizer.token().content.ToLower(), "(static|field)"))
                classVarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("static|field", tokenizer.LineCompiling);
            tokenizer.advance();
            if(tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && Regex.IsMatch(tokenizer.token().content.ToLower(), "int|char|boolean|className")) //Verificar Possibilidade de ClassName (Não COntempla) type
                classVarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("int|char|boolean|className", tokenizer.LineCompiling);
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER) 
                classVarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("Identifier", tokenizer.LineCompiling);
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL) 
                classVarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException(";", tokenizer.LineCompiling);
            classVarDec.WriteTo(writer);           
        }

        public void CompileSubroutine(JackTokenizer tokenizer)
        {
            XElement Subroutine = new XElement("subroutineDec");
            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && Regex.IsMatch(tokenizer.token().content.ToLower(), "(constructor|function|method)"))
                Subroutine.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("constructor|function|method", tokenizer.LineCompiling);
            tokenizer.advance();
            
            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && Regex.IsMatch(tokenizer.token().content.ToLower(), "int|char|boolean|void|className")) //Verificar Possibilidade de ClassName (Não COntempla) type
                Subroutine.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("Keyword int|char|boolean|void|className", tokenizer.LineCompiling);
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
                Subroutine.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("identifier", tokenizer.LineCompiling);
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "(")
                Subroutine.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("(", tokenizer.LineCompiling);
            tokenizer.advance();

            if (tokenizer.token().content != ")")
            {
                XElement ParameterList;
                CompileParameterList(tokenizer, out ParameterList);
                Subroutine.Add(ParameterList);
            }
            else{
                Subroutine.Add(new XElement("parameterList")); // CORRIGIR --> O intuito é fazer aparecer os nomes alinhados tanto o de cima quanto o de baixo
            }

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ")")
                Subroutine.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException(")", tokenizer.LineCompiling);
            tokenizer.advance();

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "{") ;
            else throw new CompilerException("{", tokenizer.LineCompiling);

            XElement SubroutineBody = new XElement("subroutineBody");
            SubroutineBody.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            tokenizer.advance();

            Subroutine.Add(SubroutineBody);

            // Aparentemente o comportamento é estável
            
            while (tokenizer.token().content != "}"){
                if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD){
                    if(tokenizer.token().content.ToLower() == "var"){
                        XElement VarDec;
                        CompileVarDec(tokenizer, out VarDec);
                        SubroutineBody.Add(VarDec);
                        tokenizer.advance();
                    } else {
                        XElement Statement;
                        compileStatements(tokenizer, out Statement);
                        SubroutineBody.Add(Statement);
                    }
                } else {
                    Console.WriteLine(tokenizer.token().content);
                    tokenizer.advance();
                }
                // TESTADO
            }
            // Aparentemente o comportamento é estável

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "}") 
                SubroutineBody.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("}", tokenizer.LineCompiling);
            Subroutine.WriteTo(writer);
        }

        public void CompileParameterList(JackTokenizer tokenizer, out XElement ParameterList)
        {
            ParameterList = new XElement("parameterList");
            while (tokenizer.token().content != ")")
            {
            //Console.WriteLine(tokenizer.token().content); // Testando
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
                throw new CompilerException("var", tokenizer.LineCompiling);
            tokenizer.advance();
            if (Regex.IsMatch(tokenizer.token().content.ToLower(), "int|char|boolean") || tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
                VarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("int|char|boolean", tokenizer.LineCompiling);
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
                VarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("identifier", tokenizer.LineCompiling);
            tokenizer.advance();
            while(tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ",")
            {
                VarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                tokenizer.advance();
                if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
                    VarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                else
                    throw new CompilerException("identifier", tokenizer.LineCompiling);
                tokenizer.advance();
            }

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ";")
                VarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException(";", tokenizer.LineCompiling);
            //tokenizer.advance();
        }

        public void compileStatements(JackTokenizer tokenizer, out XElement Statements)
        {
            Statements = new XElement("statements");

            while(tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD){
                switch (tokenizer.token().content.ToLower())
                {
                    case "let":
                        //Console.WriteLine("Passou no let"); // TESTANDO
                        XElement letStatement;
                        CompileLet(tokenizer, out letStatement);
                        Statements.Add(letStatement);
                        tokenizer.advance();
                        break;
                    case "if":
                        XElement ifSatement;
                        CompileIf(tokenizer, out ifSatement);
                        Statements.Add(ifSatement);
                        break;
                    /*case "while":
                        XElement whileStatement;
                        CompileWhile(tokenizer, out whileStatement);
                        Statements.Add(whileStatement);
                        break;*/
                    case "do":
                        XElement Do;
                        compileDo(tokenizer, out Do);
                        Statements.Add(Do);
                        tokenizer.advance();
                        break;
                    case "return":
                        XElement returnStatement;
                        compileReturn(tokenizer, out returnStatement);
                        Statements.Add(returnStatement);
                        tokenizer.advance();
                        break;
                    default:
                        break;
                }
            }

            
            //tokenizer.advance();
            /*while (tokenizer.hasMoreTokens() && tokenizer.token().content != "}")
            {
                
            }*/
        }

        public void compileDo(JackTokenizer tokenizer, out XElement Do)
        {
            Do = new XElement("doStatement");
            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && tokenizer.token().content == "do")
                Do.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("do", tokenizer.LineCompiling);
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
                Do.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("identifier", tokenizer.LineCompiling);
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ".")
            {
                Do.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                tokenizer.advance();
                if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
                    Do.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                else
                    throw new CompilerException("identifier", tokenizer.LineCompiling);
                tokenizer.advance();
            }
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "(")
                Do.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("(", tokenizer.LineCompiling);
            tokenizer.advance();
            
            if (tokenizer.token().content != ")")
            {
                XElement expressionList;
                CompileParameterList(tokenizer, out expressionList);
                Do.Add(expressionList);
            }
            else{
                Do.Add(new XElement("expressionList")); // CORRIGIR --> O intuito é fazer aparecer os nomes alinhados tanto o de cima quanto o de baixo
            }
            //
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ")")
                Do.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException(")", tokenizer.LineCompiling);
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ";")
                Do.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException(";", tokenizer.LineCompiling);
            //tokenizer.advance();
        }

        public void CompileLet(JackTokenizer tokenizer, out XElement letStatement)
        {
            letStatement = new XElement("letStatements");

            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && tokenizer.token().content == "let")
                letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("let", tokenizer.LineCompiling);
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
                letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("identifier", tokenizer.LineCompiling);
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "[")
            {
                letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                tokenizer.advance();

                XElement Expression;
                CompileExpression(tokenizer, out Expression);
                letStatement.Add(Expression);

                if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "]")
                    letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                else
                    throw new CompilerException("]", tokenizer.LineCompiling);
                tokenizer.advance();
            }

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "=")
                letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("=", tokenizer.LineCompiling);
            tokenizer.advance();

            XElement letExpression;
            CompileExpression(tokenizer, out letExpression);
            letStatement.Add(letExpression);

            //CORRIGIR --> ESSA PARTE NÃO DEVERIA EXISTIR

            while(tokenizer.token().content != ";"){
                Console.WriteLine(tokenizer.token().content); // Testando
                tokenizer.advance();
            }

            //CORRIGIR --> ESSA PARTE NÃO DEVERIA EXISTIR

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ";")
                letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException(";", tokenizer.LineCompiling);
            

        }

        public void compileReturn(JackTokenizer tokenizer, out XElement returnStatement)
        {
            returnStatement = new XElement("returnStatement");
            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && tokenizer.token().content == "return")
                returnStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("return", tokenizer.LineCompiling);
            /*if (tokenizer.token().type != JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ";")
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
            }*/
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ";")
                returnStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException(";", tokenizer.LineCompiling);
        }

        public void CompileIf(JackTokenizer tokenizer, out XElement ifStatement)
        {
            ifStatement = new XElement("ifStatements");
            
            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && tokenizer.token().content == "if")
                ifStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("if", tokenizer.LineCompiling);
            tokenizer.advance();

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "(")
                ifStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("(", tokenizer.LineCompiling);
            tokenizer.advance();
            
            if (tokenizer.token().content != ")")
            {
                XElement Expression;
                CompileExpression(tokenizer, out Expression);
                ifStatement.Add(Expression);
            }

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ")")
                ifStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException(")", tokenizer.LineCompiling);
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "{")
                ifStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("{", tokenizer.LineCompiling);
            tokenizer.advance();

            XElement Statements;
            compileStatements(tokenizer, out Statements);
            ifStatement.Add(Statements);

            //Aparentemente o comportamento é estável
            
            while (tokenizer.token().content != "}"){
                if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD){
                    if(tokenizer.token().content.ToLower() == "var"){
                        XElement VarDec;
                        CompileVarDec(tokenizer, out VarDec);
                        ifStatement.Add(VarDec);
                        tokenizer.advance();
                    } else {
                        XElement Statement;
                        compileStatements(tokenizer, out Statement);
                        ifStatement.Add(Statement);

                    }
                }
            }

            //Aparentemente o comportamento é estável

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "}")
                ifStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("}", tokenizer.LineCompiling);
            tokenizer.advance();

            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && tokenizer.token().content == "else")
            {
                if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && tokenizer.token().content == "else")
                    ifStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                else
                    throw new CompilerException("else", tokenizer.LineCompiling);
                tokenizer.advance();

                if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "{")
                    ifStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                else
                    throw new CompilerException("{", tokenizer.LineCompiling);
                tokenizer.advance();

                XElement elseStatements;
                compileStatements(tokenizer, out elseStatements);
                ifStatement.Add(elseStatements);

                //Aparentemente o comportamento é estável
                
                while (tokenizer.token().content != "}"){
                    if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD){
                        if(tokenizer.token().content.ToLower() == "var"){
                            XElement VarDec;
                            CompileVarDec(tokenizer, out VarDec);
                            ifStatement.Add(VarDec);
                            tokenizer.advance();
                        } else {
                            XElement Statement;
                            compileStatements(tokenizer, out Statement);
                            ifStatement.Add(Statement);

                        }
                    }
                }

                //Aparentemente o comportamento é estável

                if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "}")
                    ifStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                else
                    throw new CompilerException("}", tokenizer.LineCompiling);
                tokenizer.advance();
            }
                
        }
        
        public void CompileExpression(JackTokenizer tokenizer, out XElement Expression)
        {
            Expression = new XElement("expression");
            XElement term = new XElement("term");
            CompileTerm(tokenizer, out term);
            Expression.Add(term);
            //tokenizer.advance();          
        }

        //Must be Implemented
        public void CompileTerm(JackTokenizer tokenizer, out XElement Term)
        {
            Term = new XElement("term");
            if (Regex.IsMatch(tokenizer.token().content.ToLower(), "(-|~)")
                ) Term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL ||
                tokenizer.token().content.ToLower() == "("
                )
            {
                XElement expression;
                CompileExpression(tokenizer, out expression);
                Term.Add(expression);
                tokenizer.advance();
                if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL ||
                    tokenizer.token().content.ToLower() == ")") 
                    Term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                else
                    throw new CompilerException(")",tokenizer.LineCompiling);
            }


            if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER ||
                tokenizer.token().type == JackTokenizer.Token.Type.INTEGER_CONSTANT ||
                tokenizer.token().type == JackTokenizer.Token.Type.STRING_CONSTANT ||
                Regex.IsMatch(tokenizer.token().content.ToLower(), "(true|false|null|this)")
                )
            {
                Term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                tokenizer.advance();

                if(tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "."){
                    if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ".")
                    {
                        Term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                        tokenizer.advance();
                        if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
                            Term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                        else
                            throw new CompilerException("identifier", tokenizer.LineCompiling);
                        tokenizer.advance();
                    }
                    if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "(")
                        Term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                    else
                        throw new CompilerException("(", tokenizer.LineCompiling);
                    tokenizer.advance();
                    
                    if (tokenizer.token().content != ")")
                    {
                        XElement expressionList;
                        CompileParameterList(tokenizer, out expressionList);
                        Term.Add(expressionList);
                    }
                    else{
                        Term.Add(new XElement("expressionList")); // CORRIGIR --> O intuito é fazer aparecer os nomes alinhados tanto o de cima quanto o de baixo
                    }
                    //
                    if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ")")
                        Term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                    else
                        throw new CompilerException(")", tokenizer.LineCompiling);
                    tokenizer.advance();
                    if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ";")
                        Term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                    else
                        throw new CompilerException(";", tokenizer.LineCompiling);
                }
                
                if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "[")
                {
                    Term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                    tokenizer.advance();

                    XElement Expression;
                    CompileExpression(tokenizer, out Expression);
                    Term.Add(Expression);

                    if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "]")
                        Term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                    else
                        throw new CompilerException("]", tokenizer.LineCompiling);
                    tokenizer.advance();
                }

            }
            else
                throw new CompilerException("true|false|null|this|string|int|identifier", tokenizer.LineCompiling);
            
            
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
                        throw new CompilerException(",", tokenizer.LineCompiling);
                }
                expressionList.Add(expression);
            }
        }

        public void CompileWhile(JackTokenizer tokenizer, out XElement whileStatement)
        {
            whileStatement = new XElement("whileStatement");
            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && tokenizer.token().content == "while")
                whileStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("while", tokenizer.LineCompiling);
            tokenizer.advance();

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "(")
                whileStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("(", tokenizer.LineCompiling);
            tokenizer.advance();

            XElement Expression;
            CompileExpression(tokenizer, out Expression);
            whileStatement.Add(Expression);

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ")")
                whileStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException(")", tokenizer.LineCompiling);
            tokenizer.advance();

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "{")
                whileStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("{", tokenizer.LineCompiling);
            tokenizer.advance();

            XElement Statements;
            compileStatements(tokenizer, out Statements);
            whileStatement.Add(Statements);

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "}")
                whileStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("}", tokenizer.LineCompiling);
            tokenizer.advance();

        }
    }
}
