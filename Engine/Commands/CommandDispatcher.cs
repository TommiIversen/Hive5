using System.Threading.Tasks;
using Common.Models;

namespace Engine.Commands
{
    public class CommandDispatcher
    {
        public async Task<CommandResult> DispatchAsync(ICommand command)
        {
            return await command.ExecuteAsync();
        }
    }
}