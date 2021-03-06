﻿using System;
using System.Collections.Generic;
using System.IO;
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

        public XmlWriter xmlWriter = XmlWriter.Create(Console.Out, settings);
        SymbolTable symbolTable = new SymbolTable();
        VMWriter VMWriter;
        public int ControlFlowID;
        public CompilationEngine(string path)
        {
            try
            {
                VMWriter = new VMWriter(path);
                JackTokenizer Tokenizer = new JackTokenizer(path);
                Tokenizer.advance();
                CompileClass(Tokenizer);
                VMWriter.close();
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public int labelGenerator(string label)
        {
            return ControlFlowID++;
        }

        public void CompileClass(JackTokenizer tokenizer)
        {
            if (tokenizer.token().content.ToLower() == "class")
            {
                xmlWriter.WriteStartElement("class");
                xmlWriter.WriteElementString(tokenizer.token().type.ToString(), tokenizer.token().content);
                tokenizer.advance();
                if(tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
                    xmlWriter.WriteElementString(tokenizer.token().type.ToString(), tokenizer.token().content);
                else
                    throw new CompilerException("class", tokenizer.LineCompiling);
                tokenizer.advance();
                if(tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL)
                    xmlWriter.WriteElementString(tokenizer.token().type.ToString(), tokenizer.token().content);
                else
                    throw new CompilerException("{", tokenizer.LineCompiling);

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
                xmlWriter.WriteElementString("SYMBOL", "}");

                xmlWriter.WriteEndElement();
                xmlWriter.Flush();
            } else {
                throw new CompilerException("class", tokenizer.LineCompiling);
            }
        }

        public void CompileClassVarDec(JackTokenizer tokenizer)
        {
            Symbol ClassVarDecSymbol = new Symbol();

            XElement classVarDec = new XElement("classVarDec");
            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && Regex.IsMatch(tokenizer.token().content.ToLower(), "(static|field)"))
            {
                classVarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                ClassVarDecSymbol.kind = tokenizer.token().content;
            }
            else
                throw new CompilerException("static|field", tokenizer.LineCompiling);
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && Regex.IsMatch(tokenizer.token().content.ToLower(), "int|char|boolean|className"))
            { //Verificar Possibilidade de ClassName (Não COntempla) type
                classVarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                ClassVarDecSymbol.type = tokenizer.token().content;
            }
            else
                throw new CompilerException("int|char|boolean|className", tokenizer.LineCompiling);
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
            {
                classVarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                ClassVarDecSymbol.name = tokenizer.token().content;
            }
            else
                throw new CompilerException("Identifier", tokenizer.LineCompiling);
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL) 
                classVarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException(";", tokenizer.LineCompiling);
            classVarDec.WriteTo(xmlWriter);

            
            symbolTable.Add(ClassVarDecSymbol);
        }

        public void CompileSubroutine(JackTokenizer tokenizer)
        {
            XElement Subroutine = new XElement("subroutineDec");
            string FUNCTION; //  = "function Main." 

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

            //CORRIGIR - MODIFICADO POR RICARDO

            FUNCTION = tokenizer.token().content;

            //CORRIGIR - MODIFICADO POR RICARDO

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
                        FUNCTION += " " + symbolTable.LocalScopeLenght();
                        VMWriter.WriteFunction(FUNCTION);
                        XElement Statement;
                        compileStatements(tokenizer, out Statement);
                        SubroutineBody.Add(Statement);
                    }
                } else {
                    tokenizer.advance();
                }
                // TESTADO
            }
            // Aparentemente o comportamento é estável

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "}") 
                SubroutineBody.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("}", tokenizer.LineCompiling);
            Subroutine.WriteTo(xmlWriter);
        }

        public void CompileParameterList(JackTokenizer tokenizer, out XElement ParameterList, string tempName = null, int tempIndex = -1)
        {
            ParameterList = new XElement("parameterList");
            int countParameter = 0;
            bool isParameter = false;

            while (tokenizer.token().content != ")")
            {
                if(tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "("){
                    ParameterList.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                    tokenizer.advance();

                    XElement expression;
                    CompileExpression(tokenizer, out expression);
                    ParameterList.Add(expression);
                    tokenizer.advance();
                }

                //CORRIGIR - MODIFICADO POR RICARDO

                if(tokenizer.token().content.Contains('"')){
                    countParameter += 1;
                    byte[] asciiBytes = Encoding.ASCII.GetBytes(tokenizer.token().content);
                    VMWriter.WritePushString(VMWriter.Segments.CONSTANT, (tokenizer.token().content.Length - 2).ToString());
                    VMWriter.WriteCall("String.new", 1);
                    foreach(byte symbol in asciiBytes){
                        if(symbol != '"'){
                            VMWriter.WritePushString(VMWriter.Segments.CONSTANT, symbol.ToString());
                            VMWriter.WriteCall("String.appendChar", 2);
                        }
                    }
                    //VMWriter.WriteCall(tempName, 1);
                    //VMWriter.WritePop(VMWriter.Segments.LOCAL, new Symbol() { index = 1 });
                }
                else if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER){
                    countParameter += 1;

                    var temp = symbolTable.findSymbol(tokenizer.token().content);
                    if(temp.kind == "constant")
                        VMWriter.WritePushString(VMWriter.Segments.CONSTANT, temp.index.ToString());
                    else if(temp.kind == "argument")
                        VMWriter.WritePushString(VMWriter.Segments.ARGUMENT, temp.index.ToString());
                    else if(temp.kind == "local")
                        VMWriter.WritePushString(VMWriter.Segments.LOCAL, temp.index.ToString());
                    else if(temp.kind == "static")
                        VMWriter.WritePushString(VMWriter.Segments.STATIC, temp.index.ToString());
                    else if(temp.kind == "this")
                        VMWriter.WritePushString(VMWriter.Segments.THIS, temp.index.ToString());
                    else if(temp.kind == "that")
                        VMWriter.WritePushString(VMWriter.Segments.THAT, temp.index.ToString());
                    else if(temp.kind == "pointer")
                        VMWriter.WritePushString(VMWriter.Segments.POINTER, temp.index.ToString());
                    else if(temp.kind == "temp")
                        VMWriter.WritePushString(VMWriter.Segments.TEMP, temp.index.ToString());
                    
                } else if(tokenizer.token().type == JackTokenizer.Token.Type.INTEGER_CONSTANT){
                    countParameter += 1;
                    VMWriter.WritePushString(VMWriter.Segments.CONSTANT, tokenizer.token().content);
                }

                //CORRIGIR - MODIFICADO POR RICARDO

                Console.WriteLine("0 --> " + tokenizer.token().content);
                if(tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ",") isParameter = true;

                ParameterList.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                tokenizer.advance();

                Console.WriteLine("0 --> " + tokenizer.token().content);

                //CORRIGIR - MODIFICADO POR RICARDO

                if(tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && "+-*/&|<>=".Contains(tokenizer.token().content)){
                    countParameter -= 1;
                    string currentSymbol = tokenizer.token().content;

                    if(isParameter){
                        countParameter += 1;
                        currentSymbol = "~";
                    }
                    
                    while(tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content != ")"){

                        if(tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "("){
                            ParameterList.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                            tokenizer.advance();

                            XElement expression;
                            CompileExpression(tokenizer, out expression);
                            ParameterList.Add(expression);
                            tokenizer.advance();

                            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL &&
                                tokenizer.token().content.ToLower() == ")")
                                ParameterList.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                            else
                                throw new CompilerException(")", tokenizer.LineCompiling);
                        } else {
                            countParameter += 1;
                            
                            ParameterList.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                            tokenizer.advance();
                            //
                            XElement expression;
                            CompileExpression(tokenizer, out expression);
                            //tokenizer.advance();
                            //GeneratorExpression(tokenizer, ParameterList);
                        }
                    }
                    if (currentSymbol == "+") VMWriter.WriteArithmetic(VMWriter.Commands.ADD);
                    if (currentSymbol == "-") VMWriter.WriteArithmetic(VMWriter.Commands.SUB);
                    if (currentSymbol == "*") VMWriter.WriteCall("Math.multiply", 2);
                    if (currentSymbol == "/") VMWriter.WriteCall("Math.divide", 2);
                    if (currentSymbol == "&") VMWriter.WriteArithmetic(VMWriter.Commands.AND);
                    if (currentSymbol == "|") VMWriter.WriteArithmetic(VMWriter.Commands.OR);
                    if (currentSymbol == "<") VMWriter.WriteArithmetic(VMWriter.Commands.LT);
                    if (currentSymbol == ">") VMWriter.WriteArithmetic(VMWriter.Commands.GT);
                    if (currentSymbol == "=") VMWriter.WriteArithmetic(VMWriter.Commands.EQ);
                    if (currentSymbol == "~") VMWriter.WriteArithmetic(VMWriter.Commands.NEG);
                }
            }
            VMWriter.WriteCall(tempName, countParameter);

            //CORRIGIR - MODIFICADO POR RICARDO
        }       

        public void CompileVarDec(JackTokenizer tokenizer, out XElement VarDec)
        {
            VarDec = new XElement("varDec");
            Symbol symbol = new Symbol();

            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && tokenizer.token().content == "var")
            {
                VarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                symbol.kind = "local";
            }
            else
                throw new CompilerException("var", tokenizer.LineCompiling);
            tokenizer.advance();
            if (Regex.IsMatch(tokenizer.token().content.ToLower(), "int|char|boolean") || tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
            {
                VarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                symbol.type = tokenizer.token().content;
            }
            else
                throw new CompilerException("int|char|boolean", tokenizer.LineCompiling);
            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
            {
                VarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                symbol.name = tokenizer.token().content;
            }
            else
                throw new CompilerException("identifier", tokenizer.LineCompiling);
            symbolTable.Add(symbol);
            tokenizer.advance();
            while(tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ",")
            {
                Symbol whileSymbol = new Symbol() { kind = symbol.kind, type = symbol.type };
                VarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                tokenizer.advance();
                if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER)
                {
                    VarDec.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                    whileSymbol.name = tokenizer.token().content;
                }
                else
                    throw new CompilerException("identifier", tokenizer.LineCompiling);
                tokenizer.advance();
                symbolTable.Add(whileSymbol);
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
                    case "while":
                        XElement whileStatement;
                        CompileWhile(tokenizer, out whileStatement);
                        Statements.Add(whileStatement);
                        break;
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

            //CORRIGIR - MODIFICADO POR RICARDO

            string tempName;
            tempName = tokenizer.token().content;

            //CORRIGIR - MODIFICADO POR RICARDO

            tokenizer.advance();

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ".")
            {
                Do.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));

                //CORRIGIR - MODIFICADO POR RICARDO

                tempName += tokenizer.token().content;

                //CORRIGIR - MODIFICADO POR RICARDO

                tokenizer.advance();

                // CORRIGIR: PROBLEMA ENCONTRADO

                if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER || tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD) // Pq printString e printInt está sendo identificado como KEYWORD?
                    Do.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                else
                    throw new CompilerException("identifier", tokenizer.LineCompiling);

                //CORRIGIR - MODIFICADO POR RICARDO

                tempName += tokenizer.token().content;

                //CORRIGIR - MODIFICADO POR RICARDO

                // CORRIGIR: PROBLEMA ENCONTRADO
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

                //CORRIGIR - MODIFICADO POR RICARDO

                CompileParameterList(tokenizer, out expressionList, tempName);

                //CORRIGIR - MODIFICADO POR RICARDO
                
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

            //CORRIGIR - MODIFICADO POR RICARDO

            VMWriter.WritePop(VMWriter.Segments.TEMP, new Symbol() { index = 0 }); // Precisa ser corrigido

            //CORRIGIR - MODIFICADO POR RICARDO

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
            {
                letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            }
            else
                throw new CompilerException("identifier", tokenizer.LineCompiling);

            //CORRIGIR - MODIFICADO POR RICARDO

            Symbol currentSymbol = symbolTable.findSymbol(tokenizer.token().content.ToString());

            //CORRIGIR - MODIFICADO POR RICARDO

            tokenizer.advance();
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "[")
            {
                //CORRIGIR - MODIFICADO POR RICARDO

                if(currentSymbol.kind == "constant")
                    VMWriter.WritePop(VMWriter.Segments.CONSTANT, new Symbol() { index = currentSymbol.index });
                else if(currentSymbol.kind == "argument")
                    VMWriter.WritePop(VMWriter.Segments.ARGUMENT, new Symbol() { index = currentSymbol.index });
                else if(currentSymbol.kind == "local")
                    VMWriter.WritePush(VMWriter.Segments.LOCAL, new Symbol() { index = currentSymbol.index });
                else if(currentSymbol.kind == "static")
                    VMWriter.WritePop(VMWriter.Segments.STATIC, new Symbol() { index = currentSymbol.index });
                else if(currentSymbol.kind == "this")
                    VMWriter.WritePop(VMWriter.Segments.THIS, new Symbol() { index = currentSymbol.index });
                else if(currentSymbol.kind == "that")
                    VMWriter.WritePop(VMWriter.Segments.THAT, new Symbol() { index = currentSymbol.index });
                else if(currentSymbol.kind == "pointer")
                    VMWriter.WritePop(VMWriter.Segments.POINTER, new Symbol() { index = currentSymbol.index });
                else if(currentSymbol.kind == "temp")
                    VMWriter.WritePop(VMWriter.Segments.TEMP, new Symbol() { index = currentSymbol.index });

                //CORRIGIR - MODIFICADO POR RICARDO

                letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                tokenizer.advance();

                XElement Expression;
                CompileExpression(tokenizer, out Expression);
                letStatement.Add(Expression);

                //CORRIGIR - MODIFICADO POR RICARDO

                VMWriter.WriteArithmetic(VMWriter.Commands.ADD); // Em vetor acontece a adição entre váriavel e indice

                //CORRIGIR - MODIFICADO POR RICARDO

                if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "]")
                    letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                else
                    throw new CompilerException("]", tokenizer.LineCompiling);
                tokenizer.advance();

                //currentSymbol.kind = "temp";


                if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "=")
                    letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                else
                    throw new CompilerException("=", tokenizer.LineCompiling);
                tokenizer.advance();

                XElement letExpression;
                CompileExpression(tokenizer, out letExpression);
                letStatement.Add(letExpression);

                VMWriter.WritePop(VMWriter.Segments.TEMP, new Symbol() { index = 0 });
                VMWriter.WritePop(VMWriter.Segments.POINTER, new Symbol() { index = 1 });
                VMWriter.WritePush(VMWriter.Segments.TEMP, new Symbol() { index = 0 });
                VMWriter.WritePop(VMWriter.Segments.THAT, new Symbol() { index = 0 });
            }
            else
            {
                if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "=")
                    letStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                else
                    throw new CompilerException("=", tokenizer.LineCompiling);
                tokenizer.advance();

                XElement letExpression;
                CompileExpression(tokenizer, out letExpression);
                letStatement.Add(letExpression);

                if (currentSymbol.kind == "constant")
                    VMWriter.WritePop(VMWriter.Segments.CONSTANT, new Symbol() { index = currentSymbol.index });
                else if (currentSymbol.kind == "argument")
                    VMWriter.WritePop(VMWriter.Segments.ARGUMENT, new Symbol() { index = currentSymbol.index });
                else if (currentSymbol.kind == "local")
                    VMWriter.WritePop(VMWriter.Segments.LOCAL, new Symbol() { index = currentSymbol.index });
                else if (currentSymbol.kind == "static")
                    VMWriter.WritePop(VMWriter.Segments.STATIC, new Symbol() { index = currentSymbol.index });
                else if (currentSymbol.kind == "this")
                    VMWriter.WritePop(VMWriter.Segments.THIS, new Symbol() { index = currentSymbol.index });
                else if (currentSymbol.kind == "that")
                    VMWriter.WritePop(VMWriter.Segments.THAT, new Symbol() { index = currentSymbol.index });
                else if (currentSymbol.kind == "pointer")
                    VMWriter.WritePop(VMWriter.Segments.POINTER, new Symbol() { index = currentSymbol.index });
                else if (currentSymbol.kind == "temp")
                    VMWriter.WritePop(VMWriter.Segments.TEMP, new Symbol() { index = currentSymbol.index });
            }


            //CORRIGIR - MODIFICADO POR RICARDO

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
            if (tokenizer.token().type != JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ";")
            {
                XElement expression;
                CompileExpression(tokenizer, out expression);
                returnStatement.Add(expression);
            }
            else                 
                VMWriter.WritePush(VMWriter.Segments.CONSTANT, new Symbol() { index = 0 });            

            tokenizer.advance();
            VMWriter.WriteReturn();

            if(tokenizer.token().content != ";"){
                XElement Expression;
                CompileExpression(tokenizer, out Expression);
                returnStatement.Add(Expression);
            }

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ";")
                returnStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException(";", tokenizer.LineCompiling);

            
        }

        public void CompileIf(JackTokenizer tokenizer, out XElement ifStatement)
        {
            ifStatement = new XElement("ifStatements");
            int id = labelGenerator("if");
            string IF_FALSE = "IF_ELSE"+id, IF_END = "IF_END"+id;
            
            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && tokenizer.token().content == "if")
                ifStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("if", tokenizer.LineCompiling);
            VMWriter.WriteIf(IF_FALSE);
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
                ifStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                tokenizer.advance();
                VMWriter.WriteGoto(IF_END);
                VMWriter.WriteLabel(IF_FALSE);

                if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "{")
                    ifStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                else
                    throw new CompilerException("{", tokenizer.LineCompiling);
                tokenizer.advance();

                XElement elseStatements;
                compileStatements(tokenizer, out elseStatements);
                ifStatement.Add(elseStatements);

                //Aparentemente o comportamento é estável

                while (tokenizer.token().content != "}")
                {
                    if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD)
                    {
                        if (tokenizer.token().content.ToLower() == "var")
                        {
                            XElement VarDec;
                            CompileVarDec(tokenizer, out VarDec);
                            ifStatement.Add(VarDec);
                            tokenizer.advance();
                        }
                        else
                        {
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

                VMWriter.WriteLabel(IF_END);
            }
            else
                VMWriter.WriteLabel(IF_FALSE);
                
        }
        
        public void CompileExpression(JackTokenizer tokenizer, out XElement Expression)
        {
            Expression = new XElement("expression");
            XElement term = new XElement("term");
            CompileTerm(tokenizer, out term);
            Expression.Add(term);

            while (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL &&
                    "+-*/&|<>=".Contains(tokenizer.token().content))
                {
                Expression.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));

                //CORRIGIR - MODIFICADO POR RICARDO

                string currentSymbol = tokenizer.token().content.ToString();

                //CORRIGIR - MODIFICADO POR RICARDO

                tokenizer.advance();
                CompileTerm(tokenizer, out term);
                Expression.Add(term);

                //CORRIGIR - MODIFICADO POR RICARDO

                if (currentSymbol == "+") VMWriter.WriteArithmetic(VMWriter.Commands.ADD);
                if (currentSymbol == "-") VMWriter.WriteArithmetic(VMWriter.Commands.SUB);
                if (currentSymbol == "*") VMWriter.WriteCall("Math.multiply", 2);
                if (currentSymbol == "/") VMWriter.WriteCall("Math.divide", 2);
                if (currentSymbol == "&") VMWriter.WriteArithmetic(VMWriter.Commands.AND);
                if (currentSymbol == "|") VMWriter.WriteArithmetic(VMWriter.Commands.OR);
                if (currentSymbol == "<") VMWriter.WriteArithmetic(VMWriter.Commands.LT);
                if (currentSymbol == ">") VMWriter.WriteArithmetic(VMWriter.Commands.GT);
                if (currentSymbol == "=") VMWriter.WriteArithmetic(VMWriter.Commands.EQ);

                //CORRIGIR - MODIFICADO POR RICARDO

            }
            //tokenizer.advance();          
        }

        public void CompileTerm(JackTokenizer tokenizer, out XElement Term)
        {
            Term = new XElement("term");

            if (Regex.IsMatch(tokenizer.token().content.ToLower(), "(-|~)"))
            {
                Term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                if (tokenizer.token().content == "-")
                    VMWriter.WriteArithmetic(VMWriter.Commands.NEG);
                else if (tokenizer.token().content == "~")
                    VMWriter.WriteArithmetic(VMWriter.Commands.NOT);

                tokenizer.advance();
                XElement unaryOpTerm = new XElement("term");
                CompileTerm(tokenizer, out unaryOpTerm);
                Term.Add(unaryOpTerm);
            }
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL || tokenizer.token().content.ToLower() == "(")
            {
                Term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                tokenizer.advance();

                XElement expression;
                CompileExpression(tokenizer, out expression);
                Term.Add(expression);

                tokenizer.advance();
                if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL ||
                    tokenizer.token().content.ToLower() == ")")
                    Term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                else
                    throw new CompilerException(")", tokenizer.LineCompiling);
            }                   
            else if(
                tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER||
                tokenizer.token().type == JackTokenizer.Token.Type.INTEGER_CONSTANT ||
                tokenizer.token().type == JackTokenizer.Token.Type.STRING_CONSTANT ||
                Regex.IsMatch(tokenizer.token().content.ToLower(), "(true|false|null|this)")
                )
            {
                Term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));

                //CORRIGIR - MODIFICADO POR RICARDO

                string tempName;
                tempName = tokenizer.token().content;

                if(tokenizer.token().type == JackTokenizer.Token.Type.INTEGER_CONSTANT){
                    VMWriter.WritePushString(VMWriter.Segments.CONSTANT, tokenizer.token().content);
                }
                if(tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER && symbolTable.findSymbolTeste(tempName)){
                    var temp = symbolTable.findSymbol(tokenizer.token().content);
                    if(temp.kind == "constant")
                        VMWriter.WritePushString(VMWriter.Segments.CONSTANT, temp.index.ToString());
                    else if(temp.kind == "argument")
                        VMWriter.WritePushString(VMWriter.Segments.ARGUMENT, temp.index.ToString());
                    else if(temp.kind == "local")
                        VMWriter.WritePushString(VMWriter.Segments.LOCAL, temp.index.ToString());
                    else if(temp.kind == "static")
                        VMWriter.WritePushString(VMWriter.Segments.STATIC, temp.index.ToString());
                    else if(temp.kind == "this")
                        VMWriter.WritePushString(VMWriter.Segments.THIS, temp.index.ToString());
                    else if(temp.kind == "that")
                        VMWriter.WritePushString(VMWriter.Segments.THAT, temp.index.ToString());
                    else if(temp.kind == "pointer")
                        VMWriter.WritePushString(VMWriter.Segments.POINTER, temp.index.ToString());
                    else if(temp.kind == "temp")
                        VMWriter.WritePushString(VMWriter.Segments.TEMP, temp.index.ToString());
                }

                //CORRIGIR - MODIFICADO POR RICARDO

                tokenizer.advance();

                if(tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "."){
                    Term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));

                    //CORRIGIR - MODIFICADO POR RICARDO

                    tempName += tokenizer.token().content;

                    //CORRIGIR - MODIFICADO POR RICARDO

                    tokenizer.advance();
                    if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER || tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD) // CORRIGIR -- IDENTIFICANDO COMO KEYWORD
                        Term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                    else
                        throw new CompilerException("identifier", tokenizer.LineCompiling);

                    //CORRIGIR - MODIFICADO POR RICARDO

                    tempName += tokenizer.token().content;

                    //CORRIGIR - MODIFICADO POR RICARDO

                    tokenizer.advance();
                    if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "(")
                        Term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                    else
                        throw new CompilerException("(", tokenizer.LineCompiling);
                    tokenizer.advance();
                    
                    if (tokenizer.token().content != ")")
                    {
                        XElement expressionList;

                        //CORRIGIR - MODIFICADO POR RICARDO
                        
                        CompileParameterList(tokenizer, out expressionList, tempName);

                        //CORRIGIR - MODIFICADO POR RICARDO
                        
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

                    /*if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == ";")
                        Term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                    else
                        throw new CompilerException(";", tokenizer.LineCompiling);*/
                    
                }
                
                else if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "[")
                {
                    Term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                    tokenizer.advance();

                    XElement Expression;
                    CompileExpression(tokenizer, out Expression);
                    Term.Add(Expression);

                    //CORRIGIR - MODIFICADO POR RICARDO

                    VMWriter.WriteArithmetic(VMWriter.Commands.ADD); // Em vetor acontece a adição entre váriavel e indice
                    VMWriter.WritePop(VMWriter.Segments.POINTER, new Symbol(){index=1});
                    VMWriter.WritePush(VMWriter.Segments.THAT, new Symbol(){index=0});
                    //CORRIGIR - MODIFICADO POR RICARDO

                    if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "]")
                        Term.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                    else
                        throw new CompilerException("]", tokenizer.LineCompiling);
                    tokenizer.advance();
                }
            }
            else{
                Console.WriteLine(tokenizer.token().content + " " +  tokenizer.token().type);
                throw new CompilerException("true|false|null|this|string|int|identifier", tokenizer.LineCompiling);
            }
            
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
            int id = labelGenerator("while");
            string WHILE_EXP = "WHILE_EXP" + id, WHILE_END = "WHILE_END" + id;

            if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD && tokenizer.token().content == "while")
                whileStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("while", tokenizer.LineCompiling);
            VMWriter.WriteLabel(WHILE_EXP);
            tokenizer.advance();

            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "(")
                whileStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("(", tokenizer.LineCompiling);
            tokenizer.advance();

            XElement Expression;
            CompileExpression(tokenizer, out Expression);
            whileStatement.Add(Expression);

            VMWriter.WriteArithmetic(VMWriter.Commands.NOT);
            VMWriter.WriteIf(WHILE_END);
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

            //Aparentemente o comportamento é estável
            
            while (tokenizer.token().content != "}"){
                if (tokenizer.token().type == JackTokenizer.Token.Type.KEYWORD){
                    if(tokenizer.token().content.ToLower() == "var"){
                        XElement VarDec;
                        CompileVarDec(tokenizer, out VarDec);
                        whileStatement.Add(VarDec);
                        tokenizer.advance();
                    } else {
                        XElement Statement;
                        compileStatements(tokenizer, out Statement);
                        whileStatement.Add(Statement);

                    }
                }
            }

            //Aparentemente o comportamento é estável

            VMWriter.WriteGoto(WHILE_EXP); // CORRIGIR: A POSIÇÃO É PRA FICAR AQUI MESMO?
            if (tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content == "}")
                whileStatement.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            else
                throw new CompilerException("}", tokenizer.LineCompiling);
            tokenizer.advance();
            VMWriter.WriteLabel(WHILE_END);

        }

        //CORRIGIR - MODIFICADO POR RICARDO
        public void GeneratorExpression(JackTokenizer tokenizer, XElement ParameterList){
            
            if (tokenizer.token().type == JackTokenizer.Token.Type.IDENTIFIER){

                var temp = symbolTable.findSymbol(tokenizer.token().content);
                if(temp.kind == "constant")
                    VMWriter.WritePushString(VMWriter.Segments.CONSTANT, temp.index.ToString());
                else if(temp.kind == "argument")
                    VMWriter.WritePushString(VMWriter.Segments.ARGUMENT, temp.index.ToString());
                else if(temp.kind == "local")
                    VMWriter.WritePushString(VMWriter.Segments.LOCAL, temp.index.ToString());
                else if(temp.kind == "static")
                    VMWriter.WritePushString(VMWriter.Segments.STATIC, temp.index.ToString());
                else if(temp.kind == "this")
                    VMWriter.WritePushString(VMWriter.Segments.THIS, temp.index.ToString());
                else if(temp.kind == "that")
                    VMWriter.WritePushString(VMWriter.Segments.THAT, temp.index.ToString());
                else if(temp.kind == "pointer")
                    VMWriter.WritePushString(VMWriter.Segments.POINTER, temp.index.ToString());
                else if(temp.kind == "temp")
                    VMWriter.WritePushString(VMWriter.Segments.TEMP, temp.index.ToString());
                
            }

            ParameterList.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
            tokenizer.advance();

            if(tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL){
                string currentSymbol = tokenizer.token().content;
                while(tokenizer.token().type == JackTokenizer.Token.Type.SYMBOL && tokenizer.token().content != ")"){
                    ParameterList.Add(new XElement(tokenizer.token().type.ToString(), tokenizer.token().content));
                    tokenizer.advance();

                    GeneratorExpression(tokenizer, ParameterList);
                }
                if (currentSymbol == "+") VMWriter.WriteArithmetic(VMWriter.Commands.ADD);
                if (currentSymbol == "-") VMWriter.WriteArithmetic(VMWriter.Commands.SUB);
                if (currentSymbol == "*") VMWriter.WriteCall("Math.multiply", 2);
                if (currentSymbol == "/") VMWriter.WriteCall("Math.divide", 2);
                if (currentSymbol == "&") VMWriter.WriteArithmetic(VMWriter.Commands.AND);
                if (currentSymbol == "|") VMWriter.WriteArithmetic(VMWriter.Commands.OR);
                if (currentSymbol == "<") VMWriter.WriteArithmetic(VMWriter.Commands.LT);
                if (currentSymbol == ">") VMWriter.WriteArithmetic(VMWriter.Commands.GT);
                if (currentSymbol == "=") VMWriter.WriteArithmetic(VMWriter.Commands.EQ);
            }
        }

        //CORRIGIR - MODIFICADO POR RICARDO
    }
}
