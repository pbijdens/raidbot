namespace Botje.Core.Utils
{
    /// <summary>
    /// Utils for working with messages.
    /// </summary>
    public static class MessageUtils
    {
        public static string HtmlEscape(string s) => (s ?? "").Replace("<", "&lt;").Replace(">", "&gt;").Replace("&", "&amp;");
    }
}
