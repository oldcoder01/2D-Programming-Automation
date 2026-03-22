using System;
using System.Collections;
using UnityEngine;

public sealed class ScriptInterpreter
{
    public IEnumerator ExecuteRootCoroutine(ScriptBlockStatement root, ScriptRuntimeContext context, float stepDelay)
    {
        CollectFunctionDefinitions(root, context);
        yield return ExecuteBlockCoroutine(root, context, stepDelay);
    }

    private void CollectFunctionDefinitions(ScriptBlockStatement root, ScriptRuntimeContext context)
    {
        context.UserFunctions.Clear();

        for (int i = 0; i < root.Statements.Count; i++)
        {
            ScriptFunctionDefinitionStatement functionDefinition = root.Statements[i] as ScriptFunctionDefinitionStatement;
            if (functionDefinition == null)
            {
                continue;
            }

            if (context.UserFunctions.ContainsKey(functionDefinition.Name))
            {
                throw new Exception(
                    ScriptMessageFormatter.LineMessage(
                        functionDefinition.LineNumber,
                        ScriptMessageFormatter.DuplicateFunctionName(functionDefinition.Name)
                    )
                );
            }

            context.UserFunctions.Add(functionDefinition.Name, functionDefinition);
        }
    }

    private IEnumerator ExecuteBlockCoroutine(ScriptBlockStatement block, ScriptRuntimeContext context, float stepDelay)
    {
        for (int i = 0; i < block.Statements.Count; i++)
        {
            if (!context.IsRunning)
            {
                yield break;
            }

            ScriptFunctionDefinitionStatement functionDefinition = block.Statements[i] as ScriptFunctionDefinitionStatement;
            if (functionDefinition != null)
            {
                continue;
            }

            yield return ExecuteStatementCoroutine(block.Statements[i], context, stepDelay);

            if (!context.IsRunning)
            {
                yield break;
            }
        }
    }

    private IEnumerator ExecuteStatementCoroutine(ScriptStatement statement, ScriptRuntimeContext context, float stepDelay)
    {
        context.StepCounter++;

        if (context.StepCounter > context.MaxSteps)
        {
            context.WriteError(ScriptMessageFormatter.MaximumStepCountReached());
            context.IsRunning = false;
            yield break;
        }

        ScriptCallStatement callStatement = statement as ScriptCallStatement;
        if (callStatement != null)
        {
            yield return ExecuteCallStatementCoroutine(callStatement, context, stepDelay);
            yield break;
        }

        ScriptIfStatement ifStatement = statement as ScriptIfStatement;
        if (ifStatement != null)
        {
            bool condition = EvaluateExpression(ifStatement.Condition, context);
            if (!context.IsRunning)
            {
                yield break;
            }

            if (condition)
            {
                yield return ExecuteBlockCoroutine(ifStatement.ThenBlock, context, stepDelay);
                yield break;
            }

            for (int i = 0; i < ifStatement.ElifBranches.Count; i++)
            {
                ScriptElifBranch branch = ifStatement.ElifBranches[i];
                bool elifCondition = EvaluateExpression(branch.Condition, context);
                if (!context.IsRunning)
                {
                    yield break;
                }

                if (elifCondition)
                {
                    yield return ExecuteBlockCoroutine(branch.Block, context, stepDelay);
                    yield break;
                }
            }

            if (ifStatement.ElseBlock != null)
            {
                yield return ExecuteBlockCoroutine(ifStatement.ElseBlock, context, stepDelay);
            }

            yield break;
        }

        ScriptWhileStatement whileStatement = statement as ScriptWhileStatement;
        if (whileStatement != null)
        {
            while (context.IsRunning && EvaluateExpression(whileStatement.Condition, context))
            {
                yield return ExecuteBlockCoroutine(whileStatement.Block, context, stepDelay);
            }

            yield break;
        }

        context.WriteError(
            ScriptMessageFormatter.LineMessage(
                statement.LineNumber,
                ScriptMessageFormatter.StatementTypeNotSupported()
            )
        );
        context.IsRunning = false;
    }

    private IEnumerator ExecuteCallStatementCoroutine(ScriptCallStatement statement, ScriptRuntimeContext context, float stepDelay)
    {
        if (context.BuiltInRegistry.TryGetDefinition(statement.Name, out ScriptBuiltInDefinition builtInDefinition))
        {
            if (!context.IsBuiltInUnlocked(builtInDefinition))
            {
                context.WriteError(
                    ScriptMessageFormatter.LineMessage(
                        statement.LineNumber,
                        ScriptMessageFormatter.BuiltInLocked(ScriptMessageFormatter.ActionCall(statement.Name))
                    )
                );
                context.IsRunning = false;
                yield break;
            }

            if (builtInDefinition.Kind != ScriptBuiltInKind.Action)
            {
                context.WriteError(
                    ScriptMessageFormatter.LineMessage(
                        statement.LineNumber,
                        ScriptMessageFormatter.CannotBeUsedAsStatement(ScriptMessageFormatter.ActionCall(statement.Name))
                    )
                );
                context.IsRunning = false;
                yield break;
            }

            ScriptCallResult result = ExecuteBuiltInAction(statement.Name, context, statement.LineNumber);

            if (!result.Success)
            {
                yield return new WaitForSeconds(stepDelay);
                yield break;
            }

            yield return new WaitForSeconds(stepDelay);
            yield break;
        }

        if (context.UserFunctions.TryGetValue(statement.Name, out ScriptFunctionDefinitionStatement functionDefinition))
        {
            context.CallDepth++;

            if (context.CallDepth > context.MaxCallDepth)
            {
                context.WriteError(
                    ScriptMessageFormatter.LineMessage(
                        statement.LineNumber,
                        ScriptMessageFormatter.MaximumFunctionCallDepthReached()
                    )
                );
                context.IsRunning = false;
                yield break;
            }

            yield return ExecuteBlockCoroutine(functionDefinition.Block, context, stepDelay);

            context.CallDepth--;
            yield break;
        }

        context.WriteError(
            ScriptMessageFormatter.LineMessage(
                statement.LineNumber,
                ScriptMessageFormatter.NotKnownCommandOrFunction(ScriptMessageFormatter.ActionCall(statement.Name))
            )
        );
        context.IsRunning = false;
    }

    private bool EvaluateExpression(ScriptExpression expression, ScriptRuntimeContext context)
    {
        ScriptBoolLiteralExpression boolLiteral = expression as ScriptBoolLiteralExpression;
        if (boolLiteral != null)
        {
            return boolLiteral.Value;
        }

        ScriptCallExpression callExpression = expression as ScriptCallExpression;
        if (callExpression != null)
        {
            return EvaluateCallExpression(callExpression, context);
        }

        ScriptUnaryExpression unary = expression as ScriptUnaryExpression;
        if (unary != null)
        {
            bool operand = EvaluateExpression(unary.Operand, context);

            if (!context.IsRunning)
            {
                return false;
            }

            if (unary.Operator == "not")
            {
                return !operand;
            }

            context.WriteError(
                ScriptMessageFormatter.LineMessage(
                    unary.LineNumber,
                    ScriptMessageFormatter.OperatorNotSupportedHere(unary.Operator)
                )
            );
            context.IsRunning = false;
            return false;
        }

        ScriptBinaryExpression binary = expression as ScriptBinaryExpression;
        if (binary != null)
        {
            if (binary.Operator == "and")
            {
                bool left = EvaluateExpression(binary.Left, context);
                if (!context.IsRunning)
                {
                    return false;
                }

                if (!left)
                {
                    return false;
                }

                return EvaluateExpression(binary.Right, context);
            }

            if (binary.Operator == "or")
            {
                bool left = EvaluateExpression(binary.Left, context);
                if (!context.IsRunning)
                {
                    return false;
                }

                if (left)
                {
                    return true;
                }

                return EvaluateExpression(binary.Right, context);
            }

            context.WriteError(
                ScriptMessageFormatter.LineMessage(
                    binary.LineNumber,
                    ScriptMessageFormatter.OperatorNotSupportedHere(binary.Operator)
                )
            );
            context.IsRunning = false;
            return false;
        }

        context.WriteError(ScriptMessageFormatter.ExpressionCouldNotBeEvaluated());
        context.IsRunning = false;
        return false;
    }

    private bool EvaluateCallExpression(ScriptCallExpression expression, ScriptRuntimeContext context)
    {
        if (!context.BuiltInRegistry.TryGetDefinition(expression.Name, out ScriptBuiltInDefinition builtInDefinition))
        {
            context.WriteError(
                ScriptMessageFormatter.LineMessage(
                    expression.LineNumber,
                    ScriptMessageFormatter.NotKnownQuery(ScriptMessageFormatter.ActionCall(expression.Name))
                )
            );
            context.IsRunning = false;
            return false;
        }

        if (!context.IsBuiltInUnlocked(builtInDefinition))
        {
            context.WriteError(
                ScriptMessageFormatter.LineMessage(
                    expression.LineNumber,
                    ScriptMessageFormatter.BuiltInLocked(ScriptMessageFormatter.ActionCall(expression.Name))
                )
            );
            context.IsRunning = false;
            return false;
        }

        if (builtInDefinition.Kind != ScriptBuiltInKind.Query)
        {
            context.WriteError(
                ScriptMessageFormatter.LineMessage(
                    expression.LineNumber,
                    ScriptMessageFormatter.CannotBeUsedInExpression(ScriptMessageFormatter.ActionCall(expression.Name))
                )
            );
            context.IsRunning = false;
            return false;
        }

        ScriptCallResult result = ExecuteBuiltInQuery(expression.Name, context, expression.LineNumber);
        if (!result.Success)
        {
            context.IsRunning = false;
            return false;
        }

        return result.BoolValue;
    }

    private ScriptCallResult ExecuteBuiltInAction(string name, ScriptRuntimeContext context, int lineNumber)
    {
        WorldController worldController = context.WorldController;

        switch (name)
        {
            case "move_up":
                return FromActionResult(worldController.TryMoveUp(), lineNumber, context);

            case "move_down":
                return FromActionResult(worldController.TryMoveDown(), lineNumber, context);

            case "move_left":
                return FromActionResult(worldController.TryMoveLeft(), lineNumber, context);

            case "move_right":
                return FromActionResult(worldController.TryMoveRight(), lineNumber, context);

            case "pick_up":
                return FromActionResult(worldController.TryPickUp(), lineNumber, context);

            case "drop_off":
                return FromActionResult(worldController.TryDropOff(), lineNumber, context);

            default:
                context.WriteError(
                    ScriptMessageFormatter.LineMessage(
                        lineNumber,
                        ScriptMessageFormatter.NotKnownAction(ScriptMessageFormatter.ActionCall(name))
                    )
                );
                return ScriptCallResult.ActionFailure("Unknown action.");
        }
    }

    private ScriptCallResult ExecuteBuiltInQuery(string name, ScriptRuntimeContext context, int lineNumber)
    {
        WorldController worldController = context.WorldController;

        switch (name)
        {
            case "package_here":
                return ScriptCallResult.QueryResult(worldController.IsPackageHere());

            case "delivery_here":
                return ScriptCallResult.QueryResult(worldController.IsDeliveryHere());

            case "carrying_package":
                return ScriptCallResult.QueryResult(worldController.IsCarryingPackage());

            case "can_move_up":
                return ScriptCallResult.QueryResult(worldController.CanMoveUp());

            case "can_move_down":
                return ScriptCallResult.QueryResult(worldController.CanMoveDown());

            case "can_move_left":
                return ScriptCallResult.QueryResult(worldController.CanMoveLeft());

            case "can_move_right":
                return ScriptCallResult.QueryResult(worldController.CanMoveRight());

            default:
                context.WriteError(
                    ScriptMessageFormatter.LineMessage(
                        lineNumber,
                        ScriptMessageFormatter.NotKnownQuery(ScriptMessageFormatter.ActionCall(name))
                    )
                );
                return ScriptCallResult.ActionFailure("Unknown query.");
        }
    }

    private ScriptCallResult FromActionResult(ScriptActionResult actionResult, int lineNumber, ScriptRuntimeContext context)
    {
        if (actionResult == null)
        {
            context.WriteError(
                ScriptMessageFormatter.LineMessage(
                    lineNumber,
                    ScriptMessageFormatter.ActionDidNotReturnResult()
                )
            );
            return ScriptCallResult.ActionFailure("Missing action result.");
        }

        if (!actionResult.Success)
        {
            string message = actionResult.Message;

            if (string.IsNullOrWhiteSpace(message))
            {
                message = ScriptMessageFormatter.LineMessage(
                    lineNumber,
                    ScriptMessageFormatter.ActionFailedFallback()
                );
            }

            context.WriteWarning(message);
            return ScriptCallResult.ActionFailure(message);
        }

        if (!string.IsNullOrWhiteSpace(actionResult.Message))
        {
            context.WriteSuccess(actionResult.Message);
        }

        return ScriptCallResult.ActionSuccess();
    }
}