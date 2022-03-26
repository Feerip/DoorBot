using Discord;
using Discord.Interactions;

using Microsoft.Extensions.Configuration;

using System.Threading.Tasks;

namespace DoorBot.Modules
{
    // Interation modules must be public and inherit from an IInterationModuleBase
    public class GeneralModule : InteractionModuleBase<SocketInteractionContext>
    {
        // Dependencies can be accessed through Property injection, public properties with public setters will be set by the service provider
        public InteractionService Commands { get; set; }

        private CommandHandler _handler;

        // Constructor injection is also a valid way to access the dependecies
        public GeneralModule(CommandHandler handler)
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

        [SlashCommand("generate-dobby-interface", "Posts a message in this channel with the Dobby interface.")]
        [RequireOwner]
        public async Task GenerateDobbyInterface()
        {
            IRole dobbyRequiredRole = Context.Guild.GetRole(DoorUserDB._config.GetValue<ulong>("dobbyRequiredRole"));


            ComponentBuilder componentBuilder = new();

            ButtonBuilder openDoorBuilder = new ButtonBuilder()
                .WithDisabled(false)
                .WithLabel("Open Door")
                .WithStyle(ButtonStyle.Primary)
                .WithCustomId("dobbyOpen")
                ;

            componentBuilder.WithButton(openDoorBuilder);

            string message = $"Door control ({dobbyRequiredRole.Mention} only) for:\n" +
                "`1611 McClellan Drive`\n" +
                "`Klamath Falls, OR, 97603`";

            await RespondAsync(text: message, components: componentBuilder.Build());
        }

        [ComponentInteraction("dobbyOpen")]
        //[RequireRole(947640008850964530)] // Testing with CAB role
        //[RequireRole(947633653570207785)] // Testing with Brothers role
        public async Task DobbyOpen()
        {
            GpioOutput gpioOutput = GpioOutput.GetInstance();
            DoorUserDB db = DoorUserDB.GetInstance();
            IGuildUser? user = Context.User as IGuildUser;
            IRole authorizedRole = Context.Guild.GetRole(DoorUserDB._config.GetValue<ulong>("dobbyRequiredRole"));
            bool isAuthorized = false;
            string? message;
            string? reason = null;
            string keyTypeString = "Discord (Dobby Button - Brothers Only)";
            string colorString = "";
            string? userNickname = null;

            // If user is not null, that means the conversion to IGuildUser was successful, so we can pull the server nickname.
            if (user is not null)
            {
                userNickname = user.Nickname;
            }
            // If user is null, that means the conversion failed, and we have to set the nickname to the user's full Username#Discriminator for logging purposes.
            if (user is null)
            {
                //await RespondAsync("Error: Not authorized. Reason unknown.", ephemeral: true);
                isAuthorized = false;
                reason = "Unknown.";
                userNickname = Context.User.Username + "#" + Context.User.Discriminator;
            }
            else if (!user.RoleIds.Contains(authorizedRole.Id))
            {
                //await RespondAsync($"Error: Not authorized. Reason: You must have the {authorizedRole.Mention} role to use this feature.", ephemeral: true);
                reason = $"You must have the {authorizedRole.Mention} role to use this feature.";
            }
            // If neither of the above conditions are triggered, the user is authorized.
            else
            {
                isAuthorized = true;
            }

            // Set the response to the user based on authorization
            if (isAuthorized)
                message = "**Door open command has been sent.**";
            else
                message = $"**Error: Not authorized.** Reason: {reason}";

            // Respond to the user first before logging so that the interface doesn't break from the user perspective.
            await RespondAsync($"{message}\n" +
                $"Note: " +
                $"`{(isAuthorized ? "Authorized" : "Unauthorized")}`" +
                $" access by {Context.User.Mention} has been logged.",
                ephemeral: true);

            // Regardless of authorization, log the entry before going any further. If failed, the exception thrown will prevent the door from being opened.
            await db.AddToLog(id: Context.User.Id.ToString(), name: userNickname, keyType: keyTypeString, color: colorString, accessGranted: isAuthorized);
            
            // Once we reach this point we really don't care if the door command finishes or not, so just discard it.
            if (isAuthorized)
                _ = gpioOutput.OpenDoorWithBeep(); 
            else
                _ = gpioOutput.BadBeep();
        }

        [SlashCommand("door-message", "Posts the door open message")]
        [RequireOwner]
        public async Task DoorMessage()
        {
            ComponentBuilder comp_builder = new ComponentBuilder();
            ButtonBuilder button_builder = new ButtonBuilder()
                .WithDisabled(false)
                .WithLabel("Open Door")
                .WithStyle(ButtonStyle.Success)
                .WithCustomId("openDoor");

            comp_builder.WithButton(button_builder);

            // CALL DOOR OPEN CODE HERE

            await RespondAsync("test", components: comp_builder.Build());

        }



        // [Summary] lets you customize the name and the description of a parameter
        [SlashCommand("echo", "Repeat the input")]
        public async Task Echo(string echo, [Summary(description: "mention the user")] bool mention = false)
        {
            await RespondAsync(echo + (mention ? Context.User.Mention : string.Empty));
        }

        // [Group] will create a command group. [SlashCommand]s and [ComponentInteraction]s will be registered with the group prefix
        [Group("test_group", "This is a command group")]
        public class GroupExample : InteractionModuleBase<SocketInteractionContext>
        {
            // You can create command choices either by using the [Choice] attribute or by creating an enum. Every enum with 25 or less values will be registered as a multiple
            // choice option
            [SlashCommand("choice_example", "Enums create choices")]
            public async Task ChoiceExample(ExampleEnum input)
            {
                await RespondAsync(input.ToString());
            }
        }




        // User Commands can only have one parameter, which must be a type of SocketUser
        [UserCommand("SayHello")]
        public async Task SayHello(IUser user)
        {
            await RespondAsync($"Hello, {user.Mention}");
        }

        // Message Commands can only have one parameter, which must be a type of SocketMessage
        [MessageCommand("Delete")]
        [Attributes.RequireOwner]
        public async Task DeleteMesage(IMessage message)
        {
            await message.DeleteAsync();
            await RespondAsync("Deleted message.");
        }

        // Use [ComponentInteraction] to handle message component interactions. Message component interaction with the matching customId will be executed.
        // Alternatively, you can create a wild card pattern using the '*' character. Interaction Service will perform a lazy regex search and capture the matching strings.
        // You can then access these capture groups from the method parameters, in the order they were captured. Using the wild card pattern, you can cherry pick component interactions.
        [ComponentInteraction("musicSelect:*,*")]
        public async Task ButtonPress(string id, string name)
        {
            // ...
            await RespondAsync($"Playing song: {name}/{id}");
        }

        // Select Menu interactions, contain ids of the menu options that were selected by the user. You can access the option ids from the method parameters.
        // You can also use the wild card pattern with Select Menus, in that case, the wild card captures will be passed on to the method first, followed by the option ids.
        [ComponentInteraction("roleSelect")]
        public async Task RoleSelect(params string[] selections)
        {
            throw new NotImplementedException();
            // implement
        }
    }
}