# SFS TF2 Serveme Discord Bot.

This is a discord bot, coded in C# that is designed to use [na.serveme.tf](https://na.serveme.tf/), [serveme.tf](https://serveme.tf/), [au.serveme.tf](https://au.serveme.tf/), and [sea.serveme.tf](https://sea.serveme.tf/) apis to find servers, reserve servers, and update pre-existing reservations ready to go out of the box.

This bot can now also read recent log files from [logs.tf](https://logs.tf/) and make links for them

> [!WARNING]
> Due to API Changes on Serveme as a whole. You may need to request an ip whitelist for your bot to contact the API, otherwise you will get blocked at the cloudflare CAPTCHA challenge page. Join the [Serveme.tf discord server](https://discord.gg/0s38RdItLiCmARMm) and ask for help regarding it in #help 

> [!WARNING]
> You will need to setup an appsettings.json file for a discord bot token and serveme api keys in the directory where the console program is built, such as \bin\[debug/release]\net8.0
> ```
> {
>   "DiscordToken" : "Your Discord Bot Key",
>   "ServemeApiKeyNA": "Your NA Serveme API Key",
>   "ServemeApiKeyEU": "Your EU Serveme API Key",
>   "ServemeApiKeyAU": "Your AU Serveme API Key",
>   "ServemeApiKeySEA": "Your SEA Serveme API Key",
>   "LogsApiKey": "Your Logs.tf API key"
> }
> ```
>
> You only need the API Key dependent on the region you want to use the bot in, so as an example if you only plan on using it in the North American area, you only need the North American API key, however an error will be thrown if users try any other region

### Command List
Command | Command Description
:---: | :------
/find_server | Finds a server for a reservation.
/reserve_server | Reserves a server.
/update_reservation | Allows users to update parts of the server reservation.
/help | Shows all the parameters and a description about it
/ping | Gets the latency of the bot
/time_zone_help | Goes over how time zones work in the US

> [!IMPORTANT]
> You're unable to update the reservation to change the remote console (RCON) password

> [!NOTE]
> When a server is reserved, most of the info will be posted into the channel that it was ran in.
> 
> The info for remote console (RCON) will be sent to the dms of the user that ran it.
