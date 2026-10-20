using Scripter.Models;

namespace Scripter.Services;

public class MacroRunner
{
    public static MacroRunner Instance { get; } = new();

    private CancellationTokenSource? _cts;
    private Task? _runTask;

    private MacroRunner()
    {
    }

    public bool IsRunning => _runTask is { IsCompleted: false };

    public event EventHandler? RunningStateChanged;

    public Task RunAsync(Script script)
    {
        if (IsRunning)
        {
            throw new InvalidOperationException("A macro is already running.");
        }

        var error = ControlFlow.Validate(script.Actions);
        if (error != null)
        {
            throw new InvalidOperationException(error);
        }

        _cts = new CancellationTokenSource();
        _runTask = Task.Run(() => ExecuteScript(script, _cts.Token));
        RunningStateChanged?.Invoke(this, EventArgs.Empty);
        _ = ObserveCompletionAsync();
        return _runTask;
    }

    public void Stop()
    {
        _cts?.Cancel();
    }

    private async Task ObserveCompletionAsync()
    {
        if (_runTask == null)
        {
            return;
        }

        try
        {
            await _runTask;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            RunningStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static void ExecuteScript(Script script, CancellationToken token)
    {
        if (script.RepeatUntilStopped)
        {
            while (!token.IsCancellationRequested)
            {
                ExecuteOnce(script, token);
            }
        }
        else
        {
            var repeats = Math.Max(1, script.RepeatCount);
            for (var i = 0; i < repeats && !token.IsCancellationRequested; i++)
            {
                ExecuteOnce(script, token);
            }
        }
    }

    private static void ExecuteOnce(Script script, CancellationToken token)
    {
        var actions = script.Actions;
        var loops = new Stack<LoopFrame>();

        for (var i = 0; i < actions.Count;)
        {
            token.ThrowIfCancellationRequested();
            var action = actions[i];

            switch (action.Type)
            {
                case ActionType.If:
                {
                    if (EvaluateCondition(action.Condition ?? Always()))
                    {
                        i++;
                        break;
                    }

                    var elseIndex = ControlFlow.FindMatchingElse(actions, i);
                    var endIf = ControlFlow.FindMatchingEndIf(actions, i);
                    if (endIf < 0)
                    {
                        throw new InvalidOperationException("If is missing EndIf.");
                    }
                    i = elseIndex >= 0 ? elseIndex + 1 : endIf + 1;
                    break;
                }

                case ActionType.Else:
                {
                    var endIf = ControlFlow.FindMatchingEndIf(actions, i);
                    if (endIf < 0)
                    {
                        throw new InvalidOperationException("Else is missing EndIf.");
                    }
                    i = endIf + 1;
                    break;
                }

                case ActionType.EndIf:
                    i++;
                    break;

                case ActionType.Loop:
                {
                    if (loops.Count == 0 || loops.Peek().Start != i)
                    {
                        var endLoop = ControlFlow.FindMatchingEndLoop(actions, i);
                        if (endLoop < 0)
                        {
                            throw new InvalidOperationException("Loop is missing EndLoop.");
                        }

                        var timeout = action.TimeoutMs > 0
                            ? DateTime.UtcNow.AddMilliseconds(action.TimeoutMs)
                            : (DateTime?)null;
                        loops.Push(new LoopFrame(i, endLoop, timeout));
                    }

                    if (!EvaluateCondition(action.Condition ?? Always()))
                    {
                        var frame = loops.Pop();
                        i = frame.End + 1;
                        break;
                    }

                    i++;
                    break;
                }

                case ActionType.EndLoop:
                {
                    if (loops.Count == 0)
                    {
                        throw new InvalidOperationException("EndLoop without Loop.");
                    }

                    var frame = loops.Peek();
                    if (frame.Deadline != null && DateTime.UtcNow > frame.Deadline)
                    {
                        throw new TimeoutException("Loop timed out.");
                    }

                    i = frame.Start;
                    break;
                }

                case ActionType.Break:
                {
                    if (loops.Count == 0)
                    {
                        throw new InvalidOperationException("Break is not inside a Loop.");
                    }

                    var frame = loops.Pop();
                    i = frame.End + 1;
                    break;
                }

                default:
                    ExecuteAction(action, token);
                    i++;
                    break;
            }
        }
    }

    private static ActionCondition Always() => new() { Kind = ConditionKind.Always };

    private static bool EvaluateCondition(ActionCondition condition) =>
        condition.Kind == ConditionKind.Always || ImageMatcher.Matches(condition);

    private static void ExecuteAction(MacroAction action, CancellationToken token)
    {
        switch (action.Type)
        {
            case ActionType.MouseClick:
                InputSimulator.ClickMouse(action.Param1, action.Param2, action.Extra ?? "Left");
                break;

            case ActionType.MoveMouse:
                InputSimulator.MoveMouse(action.Param1, action.Param2);
                break;

            case ActionType.KeyPress:
                if (string.IsNullOrWhiteSpace(action.Extra))
                {
                    throw new InvalidOperationException("KeyPress action requires a key name.");
                }
                InputSimulator.PressKey(action.Extra);
                break;

            case ActionType.Wait:
                if (action.Param1 > 0)
                {
                    token.WaitHandle.WaitOne(action.Param1);
                    token.ThrowIfCancellationRequested();
                }
                break;

            default:
                throw new InvalidOperationException($"Unsupported action type: {action.Type}");
        }
    }

    private readonly record struct LoopFrame(int Start, int End, DateTime? Deadline);
}
