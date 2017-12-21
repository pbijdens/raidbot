# raidbot
Telegram bot for Pokemon GO raids

## Getting started

Clone the repository, then add two files being "settings.debug.json" and "settings.release.json". Use the default-settings.json file to bootstrap these. Make sure you have to telegram bits set up, one for testing and one for production.

## Deployment

Publish using visual studio, or build on the commandline `dotnet publish -r ubuntu.16.04-x64 -c Release` then publish the publish-folder.
