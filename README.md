# raidbot
Telegram bot for Pokemon GO raids

Based on the lightweight telegram bot framework [botje](https://github.com/pbijdens/botje).

## Getting started

- Clone the repository
- Add two files being "settings.debug.json" and "settings.release.json" (use default-settings.json as a template). 
- Make sure you have to telegram bots set up in botmaster, one for testing and one for production.

## Deployment

Publish using visual studio, or build on the commandline `dotnet publish -r ubuntu.16.04-x64 -c Release` then publish the publish-folder. If you publish to a Windows server, you can just publish the release build.
