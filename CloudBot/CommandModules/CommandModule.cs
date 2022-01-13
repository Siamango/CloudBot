using Discord;
using Discord.WebSocket;

namespace CloudBot.CommandModules;

public class CommandModule : AbstractCommandModule
{
    protected override void BuildCommands(List<SlashCommand> commands)
    {
        commands.Add(new SlashCommand(
            new SlashCommandBuilder()
            .WithName("invites")
            .WithDescription("Get the user invites amount")
            .Build(),
            GetInvites));
    }

    private async Task GetInvites(SocketSlashCommand command)
    {
        if (command.User is SocketGuildUser guildUser)
        {
            var invites = await guildUser.Guild.GetInvitesAsync();
            int uses = invites.Where(i => i.Inviter.Id.Equals(guildUser.Id)).Sum(i => i.Uses) ?? 0;
            await command.RespondAsync($"{guildUser.Mention} You invited {uses} Citizens 😎");
        }
    }
}