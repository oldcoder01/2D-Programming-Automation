public sealed class ScriptCallResult
{
    public bool Success;
    public bool BoolValue;
    public bool HasBoolValue;
    public string ErrorMessage;

    public static ScriptCallResult ActionSuccess()
    {
        ScriptCallResult result = new ScriptCallResult();
        result.Success = true;
        return result;
    }

    public static ScriptCallResult ActionFailure(string errorMessage)
    {
        ScriptCallResult result = new ScriptCallResult();
        result.Success = false;
        result.ErrorMessage = errorMessage;
        return result;
    }

    public static ScriptCallResult QueryResult(bool value)
    {
        ScriptCallResult result = new ScriptCallResult();
        result.Success = true;
        result.BoolValue = value;
        result.HasBoolValue = true;
        return result;
    }
}