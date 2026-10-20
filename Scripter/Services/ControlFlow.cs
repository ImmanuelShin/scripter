using Scripter.Models;

namespace Scripter.Services;

public static class ControlFlow
{
    public static string? Validate(IReadOnlyList<MacroAction> actions)
    {
        var stack = new Stack<(ActionType Open, bool HasElse)>();
        for (var i = 0; i < actions.Count; i++)
        {
            switch (actions[i].Type)
            {
                case ActionType.If:
                    stack.Push((ActionType.If, false));
                    break;
                case ActionType.Loop:
                    stack.Push((ActionType.Loop, false));
                    break;
                case ActionType.Else:
                    if (stack.Count == 0 || stack.Peek().Open != ActionType.If)
                    {
                        return $"Else at step {i + 1} has no matching If.";
                    }
                    if (stack.Peek().HasElse)
                    {
                        return $"Step {i + 1}: this If already has an Else.";
                    }
                    var ifFrame = stack.Pop();
                    stack.Push((ifFrame.Open, true));
                    break;
                case ActionType.EndIf:
                    if (stack.Count == 0 || stack.Peek().Open != ActionType.If)
                    {
                        return $"EndIf at step {i + 1} has no matching If.";
                    }
                    stack.Pop();
                    break;
                case ActionType.EndLoop:
                    if (stack.Count == 0 || stack.Peek().Open != ActionType.Loop)
                    {
                        return $"EndLoop at step {i + 1} has no matching Loop.";
                    }
                    stack.Pop();
                    break;
                case ActionType.Break:
                    if (!stack.Any(f => f.Open == ActionType.Loop))
                    {
                        return $"Break at step {i + 1} is not inside a Loop.";
                    }
                    break;
            }
        }

        if (stack.Count > 0)
        {
            var open = stack.Peek().Open;
            return open == ActionType.If ? "Unclosed If (missing EndIf)." : "Unclosed Loop (missing EndLoop).";
        }

        return null;
    }

    public static int FindMatchingElse(IReadOnlyList<MacroAction> actions, int ifIndex)
    {
        var depth = 0;
        for (var i = ifIndex + 1; i < actions.Count; i++)
        {
            switch (actions[i].Type)
            {
                case ActionType.If:
                    depth++;
                    break;
                case ActionType.EndIf:
                    if (depth == 0)
                    {
                        return -1;
                    }
                    depth--;
                    break;
                case ActionType.Else when depth == 0:
                    return i;
            }
        }

        return -1;
    }

    public static int FindMatchingEndIf(IReadOnlyList<MacroAction> actions, int fromIndex)
    {
        var depth = 0;
        for (var i = fromIndex + 1; i < actions.Count; i++)
        {
            switch (actions[i].Type)
            {
                case ActionType.If:
                    depth++;
                    break;
                case ActionType.EndIf:
                    if (depth == 0)
                    {
                        return i;
                    }
                    depth--;
                    break;
            }
        }

        return -1;
    }

    public static int FindMatchingEndLoop(IReadOnlyList<MacroAction> actions, int loopIndex)
    {
        var depth = 0;
        for (var i = loopIndex + 1; i < actions.Count; i++)
        {
            switch (actions[i].Type)
            {
                case ActionType.Loop:
                    depth++;
                    break;
                case ActionType.EndLoop:
                    if (depth == 0)
                    {
                        return i;
                    }
                    depth--;
                    break;
            }
        }

        return -1;
    }

    public static int[] GetIndentLevels(IReadOnlyList<MacroAction> actions)
    {
        var indents = new int[actions.Count];
        var depth = 0;
        for (var i = 0; i < actions.Count; i++)
        {
            var type = actions[i].Type;
            if (type is ActionType.EndIf or ActionType.EndLoop or ActionType.Else)
            {
                depth = Math.Max(0, depth - 1);
            }

            indents[i] = depth;

            if (type is ActionType.If or ActionType.Loop or ActionType.Else)
            {
                depth++;
            }
        }

        return indents;
    }
}
