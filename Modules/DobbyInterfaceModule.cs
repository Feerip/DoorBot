using Discord;
using Discord.Interactions;

using Microsoft.Extensions.Configuration;

using System.Threading.Tasks;

using DoorBot.Hardware;

namespace DoorBot.Modules
{
    // Interation modules must be public and inherit from an IInterationModuleBase
    public class DobbyInterfaceModule : InteractionModuleBase<SocketInteractionContext>
    {
        // Dependencies can be accessed through Property injection, public properties with public setters will be set by the service provider
        public InteractionService Commands { get; set; }

        private CommandHandler _handler;

        // Constructor injection is also a valid way to access the dependecies
        public DobbyInterfaceModule(CommandHandler handler)
        {
            _handler = handler;
        }

        //// Slash Commands are declared using the [SlashCommand], you need to provide a name and a description, both following the Discord guidelines
        //[SlashCommand("ping", "Recieve a pong")]
        //// By setting the DefaultPermission to false, you can disable the command by default. No one can use the command until you give them permission
        //[DefaultPermission(true)]
        //public async Task Ping()
        //{
        //    await RespondAsync("pong");
        //}

        // You can use a number of parameter types in you Slash Command handlers (string, int, double, bool, IUser, IChannel, IMentionable, IRole, Enums) by default. Optionally,
        // you can implement your own TypeConverters to support a wider range of parameter types. For more information, refer to the library documentation.
        // Optional method parameters(parameters with a default value) also will be displayed as optional on Discord.

        [SlashCommand("test-buzzer", "Debugging command")]
        [RequireOwner]
        public async Task TestBuzzer()
        {
            await DeferAsync();
            await Task.Delay(5000);
            GpioOutput.TestBeep();

            await FollowupAsync("Done", ephemeral: true);
        }

        [SlashCommand("generate-dobby-interface", "Posts a message in this channel with the Dobby interface.")]
        [RequireOwner]
        public async Task GenerateDobbyInterface(IRole FrontDoorRequiredRole, IRole GarageDoorRequiredRole)
        {
            string message = "**DO NOT** open the garage door as a joke. I'm not kidding, I will seriously remove your access if you do. " +
                "Unlike the front door, the garage door does not close/lock automatically, so if you aren't here, you're (much more obviously) " +
                "opening up my house for anyone to just walk in and steal stuff. There will be no warnings on this one, first offense = access removed.";
                
            string address = "`1611 McClellan Dr. Klamath Falls, OR, 97603`";

            EmbedBuilder embedBuilder = new EmbedBuilder()
                //.WithAuthor(Context.User)
                .WithTitle("Dobby's Door Control v2.0")
                .WithThumbnailUrl("https://c.tenor.com/EmLDxJ5BUCoAAAAM/bad-dobby.gif")
                .WithDescription(address)
                .AddField("\u200B", "Authorized Users: ")
                .AddField("Front Door", FrontDoorRequiredRole.Mention, inline: true)
                .AddField("Garage Door", GarageDoorRequiredRole.Mention, inline: true)
                .WithColor(Color.Green)
                ;


            ButtonBuilder openFrontDoorBuilder = new ButtonBuilder()
                .WithDisabled(false)
                .WithLabel("Open Front Door")
                .WithStyle(ButtonStyle.Success)
                .WithCustomId($"doorCommand:0:{FrontDoorRequiredRole.Id}")
                .WithEmote(new Emoji("🔓"))
                ;
            ButtonBuilder openGarageDoorBuilder = new ButtonBuilder()
                .WithDisabled(false)
                .WithLabel("Open Garage Door")
                .WithStyle(ButtonStyle.Success)
                .WithCustomId($"doorCommand:1:{GarageDoorRequiredRole.Id}")
                .WithEmote(new Emoji("🔓"))
                ;
            ButtonBuilder closeGarageDoorBuilder = new ButtonBuilder()
                .WithDisabled(false)
                .WithLabel("Close Garage Door")
                .WithStyle(ButtonStyle.Danger)
                .WithCustomId($"doorCommand:2:{GarageDoorRequiredRole.Id}")
                .WithEmote(new Emoji("🔒"))
                ;
            ButtonBuilder getGarageDoorStatus = new ButtonBuilder()
                .WithDisabled(false)
                .WithLabel("Garage Door Status")
                .WithStyle(ButtonStyle.Primary)
                .WithCustomId($"doorCommand:3:{GarageDoorRequiredRole.Id}")
                ;

            ComponentBuilder componentBuilder = new ComponentBuilder()
                .WithButton(openFrontDoorBuilder)
                .WithButton(openGarageDoorBuilder, row: 1)
                .WithButton(closeGarageDoorBuilder, row: 1)
                .WithButton(getGarageDoorStatus, row: 1)
                ;



            await RespondAsync(text: message, embed: embedBuilder.Build(), components: componentBuilder.Build()); 
        }

        [ComponentInteraction("doorCommand:*:*")]
        public async Task OpenFrontDoor(string rawDoorCommandID, string rawRequiredRoleID)
        {
            await DeferAsync();

            // Setup
            bool isAuthorized = false;
            string? rejectionReason = null;
            string? userNickname = null;

            // Check 1: Role ID Parse
            ulong requiredRoleID;
            if (!ulong.TryParse(rawRequiredRoleID, out requiredRoleID))
            {
                await FollowupAsync("Error: Required ID parse failed.", ephemeral: true);
                return;
            }

            // Check 2: Door Command ID Parse
            int doorCommandID;
            if (!int.TryParse(rawDoorCommandID, out doorCommandID))
            {
                await FollowupAsync("Error: Door command ID parse failed.", ephemeral: true);
                return;
            }

            // Pull some contextual information
            IRole authorizedRole = Context.Guild.GetRole(requiredRoleID);
            IGuildUser? user = Context.User as IGuildUser;
            // If user is not null, that means the conversion to IGuildUser was successful, so we can pull the server nickname.
            if (user is not null)
            {
                userNickname = user.Nickname;
            }
            // Check 3: Guild user cast failure. If it fails we can't pull the user's roles, so we can't authorize.
            else if (user is null)
            {
                await FollowupAsync("Error: Guild user cast failed for unknown reason.", ephemeral: true);
                return;
                //userNickname = Context.User.Username + "#" + Context.User.Discriminator;
            }
            // Check 4: Authorized role check
            if (!user.RoleIds.Contains(authorizedRole.Id))
            {
                rejectionReason = $"You must have the {authorizedRole.Mention} role to use this feature.";
            }
            // If none of the above conditions are triggered, the user is authorized.
            else
            {
                isAuthorized = true;
            }

            ExecuteDoorCommand(doorCommandID, isAuthorized, rejectionReason, userNickname);

        }

        private void LogAndRespondToDoorCommand(int command, bool isAuthorized, string message, string? userNickname, string keyTypeString, string colorString )
        {
            DoorUserDB db = DoorUserDB.GetInstance();
            string commandUsed;

            if (command == 0)
                commandUsed = "FrontOpen";
            else if (command == 1)
                commandUsed = "GarageOpen";
            else if (command == 2)
                commandUsed = "GarageClose";
            else if (command == 3)
                commandUsed = "GarageCheck";
            else
                commandUsed = "Unknown";

            // Respond to the user first before logging so that the interface doesn't break from the user perspective.
            FollowupAsync($"{message}\n" +
                $"Note: " +
                $"`{(isAuthorized ? "Authorized" : "Unauthorized")}`" +
                $" usage by {Context.User.Mention} has been logged.",
                ephemeral: true).Wait();

            // Regardless of authorization, log the entry before going any further. If failed, the exception thrown will prevent the door from being opened.
            db.AddToLog(id: Context.User.Id.ToString(), name: userNickname, keyType: $"{commandUsed} via {keyTypeString}", color: colorString, accessGranted: isAuthorized);
        }

        private void ExecuteDoorCommand(int command, bool isAuthorized, string? rejectionReason, string? userNickname)
        {
            // Setup
            string keyTypeString = $"Dobby in {Context.Guild.Name}";
            string colorString = "";
            string message;
            

            // Command 0: Open front door
            if (command == 0)
            {
                // Set the response to the user based on authorization
                if (isAuthorized)
                    message = "**Door open command has been sent.**";
                else
                    message = $"**Error: Not authorized.** Reason: {rejectionReason}";

                LogAndRespondToDoorCommand(command, isAuthorized, message, userNickname, keyTypeString, colorString);

                // Once we reach this point we really don't care if the door command finishes or not, so just discard it.
                if (isAuthorized)
                    _ = GpioOutput.OpenDoorWithBeep();
                else
                    _ = GpioOutput.BadBeep();
            }
            // Commands 1-3: Garage door stuff
            else if (command >= 1 && command <= 3)
            {
                if (isAuthorized)
                {
                    GarageDoorControl.GarageDoorState result;
                    switch (command)
                    {
                        // Case 1: Open garage door
                        case 1:
                            result = GarageDoorControl.OpenGarageDoor(); // Garage door command execution
                            if (result == GarageDoorControl.GarageDoorState.AlreadyOpen)
                                message = "Error: The garage door is already `open`.";
                            else if (result == GarageDoorControl.GarageDoorState.CommandSuccess)
                                message = "Success: Garage door is now `open`.";
                            else
                                message = "Command Failed: `Unknown error`";
                            break;
                        // Case 2: Close garage door
                        case 2:
                            result = GarageDoorControl.CloseGarageDoor(); // Garage door command execution
                            if (result == GarageDoorControl.GarageDoorState.AlreadyClosed)
                                message = "Error: The garage door is already `closed`.";
                            else if (result == GarageDoorControl.GarageDoorState.CommandSuccess)
                                message = "Success: Garage door is now `closed`.";
                            else
                                message = "Command Failed: `Unknown error`";
                            break;
                        // Case 3: Just checking the state of the garage door
                        case 3: 
                            result = GarageDoorControl.GetGarageDoorState(); // Garage door command execution
                            if (result == GarageDoorControl.GarageDoorState.Open)
                                message = "The garage door is currently `open`.";
                            else if (result == GarageDoorControl.GarageDoorState.Closed)
                                message = "The garage door is currently `closed`.";
                            else
                                message = "Command Failed: `Unknown error`";
                            break;
                        default:
                            message = "Command Failed: `Unknown command`";
                            break;
                    }
                }
                else
                {
                    message = $"**Error: Not authorized.** Reason: {rejectionReason}";
                }
                LogAndRespondToDoorCommand(command, isAuthorized, message, userNickname, keyTypeString, colorString);
            }
        }
    }
}