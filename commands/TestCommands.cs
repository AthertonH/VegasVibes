using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VegasVibes.commands
{
    internal class TestCommands : BaseCommandModule
    {
        // Declare command
        [Command("test")]
        // Declare async Task
        public async Task MyFirstCommand(CommandContext ctx)
        {
            // await because every task is async
            await ctx.Channel.SendMessageAsync($"Hello {ctx.User.Username}");
        }

        [Command("add")]
        public async Task Add(CommandContext ctx, int number1, int number2)
        {
            int result = number1 + number2;
            await ctx.Channel.SendMessageAsync(result.ToString());
        }
    }
}
