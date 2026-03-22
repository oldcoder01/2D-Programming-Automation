using System;
using System.Collections.Generic;

public sealed class ScriptParser
{
    private List<ScriptToken> _tokens;
    private int _current;

    public ScriptBlockStatement Parse(List<ScriptToken> tokens)
    {
        _tokens = tokens;
        _current = 0;

        ScriptBlockStatement root = new ScriptBlockStatement();
        root.LineNumber = 1;

        while (!IsAtEnd())
        {
            if (Match(ScriptTokenType.NewLine))
            {
                continue;
            }

            if (Check(ScriptTokenType.Dedent))
            {
                Advance();
                continue;
            }

            ScriptStatement statement = ParseStatement();
            root.Statements.Add(statement);
        }

        return root;
    }

    private ScriptStatement ParseStatement()
    {
        if (Match(ScriptTokenType.KeywordIf))
        {
            return ParseIfStatement(Previous().LineNumber);
        }

        if (Match(ScriptTokenType.KeywordWhile))
        {
            return ParseWhileStatement(Previous().LineNumber);
        }

        if (Match(ScriptTokenType.KeywordDef))
        {
            return ParseFunctionDefinition(Previous().LineNumber);
        }

        return ParseCallStatement();
    }

    private ScriptStatement ParseCallStatement()
    {
        ScriptToken name = Consume(ScriptTokenType.Identifier, ScriptMessageFormatter.MissingFunctionOrCommandName());
        Consume(ScriptTokenType.LeftParen, ScriptMessageFormatter.ExpectedLeftParenAfterIdentifier());
        Consume(ScriptTokenType.RightParen, ScriptMessageFormatter.ExpectedRightParenAfterLeftParen());

        ConsumeLineEnd(ScriptMessageFormatter.ExpectedNewLineAfterCall());

        ScriptCallStatement statement = new ScriptCallStatement();
        statement.LineNumber = name.LineNumber;
        statement.Name = name.Lexeme;
        return statement;
    }

    private ScriptIfStatement ParseIfStatement(int lineNumber)
    {
        ScriptIfStatement statement = new ScriptIfStatement();
        statement.LineNumber = lineNumber;
        statement.Condition = ParseExpression();

        Consume(ScriptTokenType.Colon, ScriptMessageFormatter.ExpectedColonAfterIfCondition());
        Consume(ScriptTokenType.NewLine, ScriptMessageFormatter.ExpectedNewLineAfterIf());
        statement.ThenBlock.LineNumber = lineNumber;
        ParseIndentedBlockInto(statement.ThenBlock);

        while (Match(ScriptTokenType.KeywordElif))
        {
            ScriptElifBranch branch = new ScriptElifBranch();
            branch.LineNumber = Previous().LineNumber;
            branch.Condition = ParseExpression();

            Consume(ScriptTokenType.Colon, ScriptMessageFormatter.ExpectedColonAfterElifCondition());
            Consume(ScriptTokenType.NewLine, ScriptMessageFormatter.ExpectedNewLineAfterElif());
            branch.Block.LineNumber = branch.LineNumber;
            ParseIndentedBlockInto(branch.Block);

            statement.ElifBranches.Add(branch);
        }

        if (Match(ScriptTokenType.KeywordElse))
        {
            Consume(ScriptTokenType.Colon, ScriptMessageFormatter.ExpectedColonAfterElse());
            Consume(ScriptTokenType.NewLine, ScriptMessageFormatter.ExpectedNewLineAfterElse());

            statement.ElseBlock = new ScriptBlockStatement();
            statement.ElseBlock.LineNumber = Previous().LineNumber;
            ParseIndentedBlockInto(statement.ElseBlock);
        }

        return statement;
    }

    private ScriptWhileStatement ParseWhileStatement(int lineNumber)
    {
        ScriptWhileStatement statement = new ScriptWhileStatement();
        statement.LineNumber = lineNumber;
        statement.Condition = ParseExpression();

        Consume(ScriptTokenType.Colon, ScriptMessageFormatter.ExpectedColonAfterWhileCondition());
        Consume(ScriptTokenType.NewLine, ScriptMessageFormatter.ExpectedNewLineAfterWhile());

        statement.Block.LineNumber = lineNumber;
        ParseIndentedBlockInto(statement.Block);
        return statement;
    }

    private ScriptFunctionDefinitionStatement ParseFunctionDefinition(int lineNumber)
    {
        ScriptToken name = Consume(ScriptTokenType.Identifier, ScriptMessageFormatter.MissingFunctionNameAfterDef());
        Consume(ScriptTokenType.LeftParen, ScriptMessageFormatter.ExpectedLeftParenAfterFunctionName());
        Consume(ScriptTokenType.RightParen, ScriptMessageFormatter.ExpectedRightParenInFunctionDefinition());
        Consume(ScriptTokenType.Colon, ScriptMessageFormatter.ExpectedColonAfterFunctionDefinition());
        Consume(ScriptTokenType.NewLine, ScriptMessageFormatter.ExpectedNewLineAfterFunctionDefinition());

        ScriptFunctionDefinitionStatement statement = new ScriptFunctionDefinitionStatement();
        statement.LineNumber = lineNumber;
        statement.Name = name.Lexeme;
        statement.Block.LineNumber = lineNumber;

        ParseIndentedBlockInto(statement.Block);
        return statement;
    }

    private void ParseIndentedBlockInto(ScriptBlockStatement block)
    {
        Consume(ScriptTokenType.Indent, ScriptMessageFormatter.ExpectedIndentedBlock());

        while (!Check(ScriptTokenType.Dedent) && !IsAtEnd())
        {
            if (Match(ScriptTokenType.NewLine))
            {
                continue;
            }

            block.Statements.Add(ParseStatement());
        }

        Consume(ScriptTokenType.Dedent, ScriptMessageFormatter.ExpectedEndOfIndentedBlock());
    }

    private ScriptExpression ParseExpression()
    {
        return ParseOr();
    }

    private ScriptExpression ParseOr()
    {
        ScriptExpression expression = ParseAnd();

        while (Match(ScriptTokenType.KeywordOr))
        {
            ScriptToken operatorToken = Previous();
            ScriptExpression right = ParseAnd();

            ScriptBinaryExpression binary = new ScriptBinaryExpression();
            binary.LineNumber = operatorToken.LineNumber;
            binary.Operator = "or";
            binary.Left = expression;
            binary.Right = right;
            expression = binary;
        }

        return expression;
    }

    private ScriptExpression ParseAnd()
    {
        ScriptExpression expression = ParseUnary();

        while (Match(ScriptTokenType.KeywordAnd))
        {
            ScriptToken operatorToken = Previous();
            ScriptExpression right = ParseUnary();

            ScriptBinaryExpression binary = new ScriptBinaryExpression();
            binary.LineNumber = operatorToken.LineNumber;
            binary.Operator = "and";
            binary.Left = expression;
            binary.Right = right;
            expression = binary;
        }

        return expression;
    }

    private ScriptExpression ParseUnary()
    {
        if (Match(ScriptTokenType.KeywordNot))
        {
            ScriptToken operatorToken = Previous();
            ScriptExpression operand = ParseUnary();

            ScriptUnaryExpression unary = new ScriptUnaryExpression();
            unary.LineNumber = operatorToken.LineNumber;
            unary.Operator = "not";
            unary.Operand = operand;
            return unary;
        }

        return ParsePrimary();
    }

    private ScriptExpression ParsePrimary()
    {
        if (Match(ScriptTokenType.KeywordTrue))
        {
            ScriptBoolLiteralExpression expression = new ScriptBoolLiteralExpression();
            expression.LineNumber = Previous().LineNumber;
            expression.Value = true;
            return expression;
        }

        if (Match(ScriptTokenType.KeywordFalse))
        {
            ScriptBoolLiteralExpression expression = new ScriptBoolLiteralExpression();
            expression.LineNumber = Previous().LineNumber;
            expression.Value = false;
            return expression;
        }

        if (Match(ScriptTokenType.Identifier))
        {
            ScriptToken name = Previous();
            Consume(ScriptTokenType.LeftParen, ScriptMessageFormatter.ExpectedLeftParenAfterIdentifier());
            Consume(ScriptTokenType.RightParen, ScriptMessageFormatter.ExpectedRightParenAfterLeftParen());

            ScriptCallExpression expression = new ScriptCallExpression();
            expression.LineNumber = name.LineNumber;
            expression.Name = name.Lexeme;
            return expression;
        }

        if (Match(ScriptTokenType.LeftParen))
        {
            ScriptExpression expression = ParseExpression();
            Consume(ScriptTokenType.RightParen, ScriptMessageFormatter.ExpectedRightParenAfterExpression());
            return expression;
        }

        throw Error(Peek(), ScriptMessageFormatter.ExpectedExpression());
    }

    private void ConsumeLineEnd(string message)
    {
        Consume(ScriptTokenType.NewLine, message);
    }

    private ScriptToken Consume(ScriptTokenType type, string message)
    {
        if (Check(type))
        {
            return Advance();
        }

        throw Error(Peek(), message);
    }

    private bool Match(ScriptTokenType type)
    {
        if (Check(type))
        {
            Advance();
            return true;
        }

        return false;
    }

    private bool Check(ScriptTokenType type)
    {
        if (IsAtEnd())
        {
            return type == ScriptTokenType.EndOfFile;
        }

        return Peek().Type == type;
    }

    private ScriptToken Advance()
    {
        if (!IsAtEnd())
        {
            _current++;
        }

        return Previous();
    }

    private bool IsAtEnd()
    {
        return Peek().Type == ScriptTokenType.EndOfFile;
    }

    private ScriptToken Peek()
    {
        return _tokens[_current];
    }

    private ScriptToken Previous()
    {
        return _tokens[_current - 1];
    }

    private Exception Error(ScriptToken token, string message)
    {
        return new Exception(ScriptMessageFormatter.LineMessage(token.LineNumber, message));
    }
}