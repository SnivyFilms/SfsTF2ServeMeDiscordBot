# SFS TF2 Serveme Discord Bot.

This is a discord bot, coded in C# that is designed to use [na.serveme.tf](https://na.serveme.tf/) api to find servers, reserve servers, and update pre-existing reservations ready to go out of the box.

> [!NOTE]
> You will need to setup an appsettings.json file for a discord bot token and a na.serveme.tf api key in the directory where the console program is built, such as \bin\[debug/release]\net8.0
> ```
> {
>   "DiscordToken" : "Your Discord Bot Key"
>   "ServemeApiKey" : "Your na.serveme.tf API Key"
> }
> ```

### Command List
Command | Command Description
:---: | :------
/find_server | Finds a server for a reservation.
/reserve_server | Reserves a server.
/update_reservation | Allows users to update parts of the server reservation.
/help | Shows all the parameters and a description about it
/ping | Gets the latency of the bot

> [!IMPORTANT]
> As of now, all time parameters are in US East.
> You're unable to update the reservation to change the remote console (RCON) password

> [!NOTE]
> When a server is reserved, most of the info will be posted into the channel that it was ran in.
> The info for remote console (RCON) will be sent to the dms of the user that ran it.
