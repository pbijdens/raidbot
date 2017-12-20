using Ninject;
using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace Botje.Core.Commands
{
    /// <summary>
    /// https://stackoverflow.com/questions/57615/how-to-add-a-timeout-to-console-readline
    /// </summary>
    public class ConsoleLoop
    {
        [Inject]
        public IConsoleCommand[] Commands { get; set; }

        [Inject]
        public ILoggerFactory LoggerFactory { set { _log = value.Create(GetType()); } }

        private ILogger _log;

        private class Reader
        {
            private static Thread inputThread;
            private static AutoResetEvent getInput, gotInput;
            private static string input;

            static Reader()
            {
                getInput = new AutoResetEvent(false);
                gotInput = new AutoResetEvent(false);
                inputThread = new Thread(reader);
                inputThread.IsBackground = true;
                inputThread.Start();
            }

            private static void reader()
            {
                while (true)
                {
                    try
                    {
                        getInput.WaitOne();
                        input = System.Console.ReadLine();
                        gotInput.Set();
                    }
                    catch (ThreadAbortException) { return; }
                    catch (Exception)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                }
            }

            // omit the parameter to read a line without a timeout
            public static string ReadLine(int timeOutMillisecs = Timeout.Infinite)
            {
                getInput.Set();
                bool success = gotInput.WaitOne(timeOutMillisecs);
                if (success)
                    return input;
                else
                    throw new TimeoutException("User did not provide input within the timelimit.");
            }

            public static bool TryReadLine(out string line, int timeOutMillisecs = Timeout.Infinite)
            {
                getInput.Set();
                bool success = gotInput.WaitOne(timeOutMillisecs);
                if (success)
                    line = input;
                else
                    line = null;
                return success;
            }
        }

        /// <summary>
        /// Will process the console input in a loop until the cancellation token is canceled.
        /// </summary>
        /// <param name="token"></param>
        public void Run(CancellationToken token)
        {
            // All commands should know the console was started
            foreach (var commandObj in Commands)
            {
                commandObj.OnStart(_log);
            }

            Prompt();
            _log.Trace($"Console loop was started, running until aborted.");
            while (!token.IsCancellationRequested)
            {
                if (Reader.TryReadLine(out string commandLine, 1000))
                {
                    if (!string.IsNullOrWhiteSpace(commandLine))
                    {
                        (string command, string[] args) = ParseCommandLine(commandLine);
                        string argstr = string.Join(", ", args.Select(x => $"\"{x}\"").ToArray());
                        _log.Trace($"Processing command \"{command}\" with {args.Length} arguments: {argstr}");
                        bool found = false;
                        foreach (var commandObj in Commands)
                        {
                            try
                            {
                                if (string.Equals(commandObj.Info.Command, command, StringComparison.InvariantCultureIgnoreCase) ||
                                    (commandObj.Info.Aliases != null && commandObj.Info.Aliases.Where(x => string.Equals(x, command, StringComparison.InvariantCultureIgnoreCase)).Any()))
                                {
                                    if (commandObj.OnInput(command, args))
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _log.Error(ex, $"Error processing command {commandObj.GetType().Name} for \"{commandLine}\"");
                            }
                        }
                        if (!found && !string.IsNullOrWhiteSpace(command))
                        {
                            string commandsStr = string.Join(", ", Commands.Select(x => x.Info.Command));
                            Console.WriteLine($"Command '{command}' is not supported. Try one of {commandsStr}.");
                        }
                    }
                    Prompt();
                }
            }
            _log.Trace($"Console loop shut down. Have a nice day.");
        }

        private static void Prompt()
        {
            Console.Write($"\r\n{DateTime.Now} : {Assembly.GetEntryAssembly().EntryPoint.DeclaringType.FullName}> ");
        }

        private (string command, string[] args) ParseCommandLine(string commandLine)
        {
            var re = new Regex("(?<=\")[^\"]*(?=\")|[^\" ]+");
            var strings = re.Matches(commandLine).Cast<Match>().Select(m => m.Value).ToArray();
            return (strings.FirstOrDefault(), strings.Skip(1).ToArray());
        }
    }
}
