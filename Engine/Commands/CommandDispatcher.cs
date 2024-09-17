using System.Threading.Tasks;

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