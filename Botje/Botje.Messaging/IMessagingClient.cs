using Botje.Messaging.Events;
using Botje.Messaging.Models;
using System;
using System.Collections.Generic;

namespace Botje.Messaging
{
    /// <summary>
    /// Messaging client interface.
    /// </summary>
    public interface IMessagingClient
    {
        /// <summary>
        /// Start receiving.
        /// </summary>
        void Start();

        /// <summary>
        /// Stop receiving.
        /// </summary>
        void Stop();

        /// <summary>
        /// Private message was received.
        /// </summary>
        event EventHandler<PrivateMessageEventArgs> OnPrivateMessage;

        /// <summary>
        /// Public message was received.
        /// </summary>
        event EventHandler<PublicMessageEventArgs> OnPublicMessage;

        /// <summary>
        /// A message arrived in a channel.
        /// </summary>
        event EventHandler<ChannelMessageEventArgs> OnChannelMessage;

        /// <summary>
        /// Inline query was requested.
        /// </summary>
        event EventHandler<InlineQueryEventArgs> OnInlineQuery;

        /// <summary>
        /// Inline query was requested.
        /// </summary>
        event EventHandler<ChosenInlineQueryResultEventArgs> OnChosenInlineQueryResult;

        /// <summary>
        /// Query callback reply was received.
        /// </summary>
        event EventHandler<QueryCallbackEventArgs> OnQueryCallback;

        /// <summary>
        /// A private message was edited.
        /// </summary>
        event EventHandler<ChannelMessageEditedEventArgs> OnChannelMessageEdited;

        /// <summary>
        /// A private message was edited.
        /// </summary>
        event EventHandler<PrivateMessageEditedEventArgs> OnPrivateMessageEdited;

        /// <summary>
        /// A public message was edited.
        /// </summary>
        event EventHandler<PublicMessageEditedEventArgs> OnPublicMessageEdited;

        /// <summary>
        /// Who is the bot?
        /// </summary>
        /// <returns></returns>
        User GetMe();

        /// <summary>
        /// See TeleGram API documentation for sendMessage
        /// </summary>
        /// <param name="chatID"></param>
        /// <param name="text"></param>
        /// <param name="parseMode"></param>
        /// <param name="disableWebPagePreview"></param>
        /// <param name="disableNotification"></param>
        /// <param name="replyToMessageId"></param>
        /// <param name="replyMarkup"></param>
        /// <returns></returns>
        Message SendMessageToChat(long chatID, string text, string parseMode = "HTML", bool? disableWebPagePreview = null, bool? disableNotification = null, long? replyToMessageId = null, InlineKeyboardMarkup replyMarkup = null);

        /// <summary>
        /// See telegram API documentation
        /// </summary>
        /// <param name="callbackQueryID"></param>
        /// <param name="text"></param>
        /// <param name="showAlert"></param>
        /// <returns></returns>
        bool AnswerCallbackQuery(string callbackQueryID, string text = null, bool? showAlert = false);

        /// <summary>
        /// See Telegram API documentation
        /// </summary>
        /// <param name="chatID"></param>
        /// <param name="messageID"></param>
        /// <param name="inlineMessageID"></param>
        /// <param name="text"></param>
        /// <param name="parseMode"></param>
        /// <param name="disableWebPagePreview"></param>
        /// <param name="replyMarkup"></param>
        /// <param name="chatType"></param>
        void EditMessageText(string chatID, long? messageID, string inlineMessageID, string text, string parseMode = null, bool? disableWebPagePreview = null, InlineKeyboardMarkup replyMarkup = null, string chatType = "private");

        /// <summary>
        /// See Telegram API documentation
        /// </summary>
        /// <param name="queryID"></param>
        /// <param name="results"></param>
        void AnswerInlineQuery(string queryID, List<InlineQueryResultArticle> results);

        void ForwardMessageToChat(long chatID, long sourceChat, long sourceMessageID);
    }
}
