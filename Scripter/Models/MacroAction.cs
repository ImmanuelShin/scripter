namespace Scripter.Models;

public class MacroAction
{
    public ActionType Type { get; set; }
    public int Param1 { get; set; }
    public int Param2 { get; set; }
    public string? Extra { get; set; }
    public ActionCondition? Condition { get; set; }
    public int TimeoutMs { get; set; }

    public MacroAction Clone() => new()
    {
        Type = Type,
        Param1 = Param1,
        Param2 = Param2,
        Extra = Extra,
        Condition = Condition?.Clone(),
        TimeoutMs = TimeoutMs
    };

    public string GetSummary() => Type switch
    {
        ActionType.MouseClick => $"Click {Extra ?? "Left"} at ({Param1}, {Param2})",
        ActionType.MoveMouse => $"Move to ({Param1}, {Param2})",
        ActionType.KeyPress => $"Press {Extra ?? "?"}",
        ActionType.Wait => $"Wait {Param1} ms",
        ActionType.If => $"If {Condition?.GetSummary() ?? "always"}",
        ActionType.Else => "Else",
        ActionType.EndIf => "EndIf",
        ActionType.Loop => LoopSummary(),
        ActionType.EndLoop => "EndLoop",
        ActionType.Break => "Break",
        _ => Type.ToString()
    };

    private string LoopSummary()
    {
        var cond = Condition ?? new ActionCondition { Kind = ConditionKind.Always };
        var timeout = TimeoutMs > 0 ? $", timeout {TimeoutMs} ms" : "";
        return cond.Kind == ConditionKind.Always
            ? $"Loop until break{timeout}"
            : $"Loop while {cond.GetSummary()}{timeout}";
    }
}
