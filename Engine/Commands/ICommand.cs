using System.Threading.Tasks;

namespace Engine.Commands
{
    public interface ICommand
    {
        Task<CommandResult> ExecuteAsync();
    }
}