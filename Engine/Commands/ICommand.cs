using Common.Models;

namespace Engine.Commands
{
    public interface ICommand
    {
        Task<CommandResult> ExecuteAsync();
    }
}