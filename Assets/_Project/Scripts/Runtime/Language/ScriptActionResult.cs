public sealed class ScriptActionResult
{
    public bool Success;
    public string Message;

    public static ScriptActionResult Succeeded(string message = "")
    {
        ScriptActionResult result = new ScriptActionResult();
        result.Success = true;
        result.Message = message;
        return result;
    }

    public static ScriptActionResult Failed(string message)
    {
        ScriptActionResult result = new ScriptActionResult();
        result.Success = false;
        result.Message = message;
        return result;
    }
}