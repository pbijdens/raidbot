using Botje.Core;
using Botje.Core.Utils;
using Botje.DB;
using Botje.Messaging;
using Botje.Messaging.Events;
using Botje.Messaging.Models;
using NGettext;
using Ninject;
using PokemonRaidBot.Entities;
using System;
using System.Linq;

namespace PokemonRaidBot.ChatCommands
{
    public abstract class ChatCommandModuleBase : IBotModule
    {
        protected readonly Func<string, string> _HTML_ = (s) => MessageUtils.HtmlEscape(s);

        public enum Source
        {
            Public,
            Private,
            Channel
        }

        protected ILogger Log;

        /// <summary></summary>
        [Inject]
        public ICatalog I18N { get; set; }

        /// <summary></summary>
        [Inject]
        public IMessagingClient Client { get; set; }

        /// <summary></summary>
        [Inject]
        public ILoggerFactory LoggerFactory { set { Log = value.Create(GetType()); } }

        [Inject]
        public IDatabase DB { get; set; }

        /// <summary>
        /// </summary>
        public virtual void Shutdown()
        {
            Client.OnPrivateMessage -= Client_OnPrivateMessage;
            Client.OnChannelMessage -= Client_OnChannelMessage;
            Client.OnPublicMessage -= Client_OnPublicMessage;
            Log.Trace($"Shut down {GetType().Name}");
        }

        /// <summary>
        /// </summary>
        public virtual void Startup()
        {
            Log.Trace($"Started {GetType().Name}");
            Client.OnPrivateMessage += Client_OnPrivateMessage;
            Client.OnChannelMessage += Client_OnChannelMessage;
            Client.OnPublicMessage += Client_OnPublicMessage;
        }

        private void Client_OnPublicMessage(object sender, PublicMessageEventArgs e)
        {
            ProcessMessage(Source.Public, e.Message);
        }

        private void Client_OnChannelMessage(object sender, ChannelMessageEventArgs e)
        {
            ProcessMessage(Source.Channel, e.Message);
        }

        private void Client_OnPrivateMessage(object sender, PrivateMessageEventArgs e)
        {
            ProcessMessage(Source.Private, e.Message);
        }

        private void ProcessMessage(Source source, Message message)
        {
            var me = Client.GetMe();
            var firstEntity = message?.Entities?.FirstOrDefault();
            if (null != firstEntity && firstEntity.Type == "bot_command" && firstEntity.Offset == 0)
            {
                string myName = Client.GetMe().Username;
                string command = message.Text.Substring(firstEntity.Offset, firstEntity.Length);
                if (command.Contains("@") && !command.EndsWith($"@{myName}", StringComparison.InvariantCultureIgnoreCase))
                {
                    // not for me
                    Log.Trace($"Got command '{command}' but it is not for me.");
                }
                else
                {
                    command = command.Split("@").First()?.ToLowerInvariant();
                    var args = CommandLineUtils.SplitArgs(message.Text.Substring(firstEntity.Length).Trim()).ToArray();
                    ProcessCommand(source, message, command, args);
                }
            }
        }

        public static object UserSettingsLock = new object();
        protected UserSettings GetOrCreateUserSettings(User user, out DbSet<UserSettings> userSettingsCollection)
        {
            lock (UserSettingsLock)
            {
                userSettingsCollection = DB.GetCollection<UserSettings>();
                var result = userSettingsCollection.Find(x => x.User.ID == user.ID).FirstOrDefault();
                if (result == null)
                {
                    result = new UserSettings { User = user };
                    userSettingsCollection.Insert(result);
                }
                return result;
            }
        }

        /// <summary>
        /// Process a command message with (potentially quoted) arguments on public, private and channel-chats.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public abstract void ProcessCommand(Source source, Message message, string command, string[] args);
    }
}
