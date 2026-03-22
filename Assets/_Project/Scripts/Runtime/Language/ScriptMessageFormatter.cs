public static class ScriptMessageFormatter
{
    public static string LineMessage(int lineNumber, string message)
    {
        return "Line " + lineNumber + ": " + message;
    }

    public static string RuntimeAlreadyRunning()
    {
        return "Runtime is already running.";
    }

    public static string RuntimeStarted()
    {
        return "Runtime started.";
    }

    public static string RuntimeFinished()
    {
        return "Runtime finished.";
    }

    public static string RuntimeStopped()
    {
        return "Runtime stopped.";
    }

    public static string MissingWorldControllerReference()
    {
        return "ScriptRuntimeController is missing WorldController reference.";
    }

    public static string MaximumStepCountReached()
    {
        return "Script stopped. Maximum step count was reached.";
    }

    public static string MaximumFunctionCallDepthReached()
    {
        return "Maximum function call depth was reached.";
    }

    public static string DuplicateFunctionName(string functionName)
    {
        return "Duplicate function name '" + functionName + "'.";
    }

    public static string StatementTypeNotSupported()
    {
        return "This statement type is not supported.";
    }

    public static string ActionDidNotReturnResult()
    {
        return "The action did not return a result.";
    }

    public static string ActionFailedFallback()
    {
        return "The action failed.";
    }

    public static string ExpressionCouldNotBeEvaluated()
    {
        return "An expression could not be evaluated.";
    }

    public static string OperatorNotSupportedHere(string operatorText)
    {
        return "The operator '" + operatorText + "' is not supported here.";
    }

    public static string BuiltInLocked(string callText)
    {
        return callText + " is locked.";
    }

    public static string CannotBeUsedAsStatement(string callText)
    {
        return callText + " cannot be used as a statement.";
    }

    public static string CannotBeUsedInExpression(string callText)
    {
        return callText + " cannot be used in an expression.";
    }

    public static string NotKnownCommandOrFunction(string callText)
    {
        return callText + " is not a known command or function.";
    }

    public static string NotKnownAction(string callText)
    {
        return callText + " is not a known action.";
    }

    public static string NotKnownQuery(string callText)
    {
        return callText + " is not a known query.";
    }

    public static string MissingFunctionOrCommandName()
    {
        return "Expected a function or command name.";
    }

    public static string MissingFunctionNameAfterDef()
    {
        return "Expected a function name after def.";
    }

    public static string ExpectedLeftParenAfterIdentifier()
    {
        return "Expected '(' after the identifier.";
    }

    public static string ExpectedLeftParenAfterFunctionName()
    {
        return "Expected '(' after the function name.";
    }

    public static string ExpectedRightParenAfterLeftParen()
    {
        return "Expected ')' after '('.";
    }

    public static string ExpectedRightParenAfterExpression()
    {
        return "Expected ')' after the expression.";
    }

    public static string ExpectedRightParenInFunctionDefinition()
    {
        return "Expected ')' after '(' in the function definition.";
    }

    public static string ExpectedColonAfterIfCondition()
    {
        return "Expected ':' after the if condition.";
    }

    public static string ExpectedColonAfterElifCondition()
    {
        return "Expected ':' after the elif condition.";
    }

    public static string ExpectedColonAfterWhileCondition()
    {
        return "Expected ':' after the while condition.";
    }

    public static string ExpectedColonAfterFunctionDefinition()
    {
        return "Expected ':' after the function definition.";
    }

    public static string ExpectedColonAfterElse()
    {
        return "Expected ':' after else.";
    }

    public static string ExpectedNewLineAfterCall()
    {
        return "Expected a new line after the call.";
    }

    public static string ExpectedNewLineAfterIf()
    {
        return "Expected a new line after the if statement.";
    }

    public static string ExpectedNewLineAfterElif()
    {
        return "Expected a new line after the elif statement.";
    }

    public static string ExpectedNewLineAfterWhile()
    {
        return "Expected a new line after the while statement.";
    }

    public static string ExpectedNewLineAfterElse()
    {
        return "Expected a new line after else.";
    }

    public static string ExpectedNewLineAfterFunctionDefinition()
    {
        return "Expected a new line after the function definition.";
    }

    public static string ExpectedIndentedBlock()
    {
        return "Expected an indented block.";
    }

    public static string ExpectedEndOfIndentedBlock()
    {
        return "Expected the end of the indented block.";
    }

    public static string ExpectedExpression()
    {
        return "Expected an expression.";
    }

    public static string ActionCall(string name)
    {
        return name + "()";
    }
}