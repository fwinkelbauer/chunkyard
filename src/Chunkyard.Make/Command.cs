namespace Chunkyard.Make;

public interface ICommand
{
    void Handle(ICommandHandler handler);
}

public sealed class BuildCommand : ICommand
{
    public void Handle(ICommandHandler handler) => handler.Handle(this);
}

public sealed class CheckCommand : ICommand
{
    public void Handle(ICommandHandler handler) => handler.Handle(this);
}

public sealed class CleanCommand : ICommand
{
    public void Handle(ICommandHandler handler) => handler.Handle(this);
}

public sealed class FormatCommand : ICommand
{
    public void Handle(ICommandHandler handler) => handler.Handle(this);
}

public sealed class HelpCommand : ICommand
{
    public HelpCommand(
        IReadOnlyCollection<Usage> usages,
        IReadOnlyCollection<string> errors)
    {
        Usages = usages;
        Errors = errors;
    }

    public IReadOnlyCollection<Usage> Usages { get; }

    public IReadOnlyCollection<string> Errors { get; }

    public void Handle(ICommandHandler handler) => handler.Handle(this);
}

public sealed class PublishCommand : ICommand
{
    public void Handle(ICommandHandler handler) => handler.Handle(this);
}

public sealed class ReleaseCommand : ICommand
{
    public void Handle(ICommandHandler handler) => handler.Handle(this);
}
