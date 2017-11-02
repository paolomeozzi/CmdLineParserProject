using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CmdLineParserPackage
{
    public class CmdLineParser
    {
        private readonly string[] args;        
        private string optionPrefix = "-";
        private string optionValuePrefix = ":";
        private bool helpOnZeroArguments = false;
        ArgumentPolicy argPolicy = ArgumentPolicy.Optional;
        IEqualityComparer<string> comparer = IgnoreCaseComparer.Instance;

        Action<string> argumentHandler;
        Action<ParsingErrorInfo> errorHandler;
        Action helpHandler;
        Dictionary<string, OptionInfo> optHandlers;

        bool oneArgumentParsed;
        ParseResult result;
        string[] helps = new string[0];
        public CmdLineParser(string[] args, bool ignoreCase = true)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            this.args = args;
            errorHandler = pei => { };            
            CmdLine = string.Join(" ", args);

            var comparer = ignoreCase? IgnoreCaseComparer.Instance : EqualityComparer<string>.Default;
            optHandlers = new Dictionary<string, OptionInfo>(comparer);
        }

        public string CmdLine { get; private set; }
        public ParsingErrorInfo Error { get; private set; }
        public string LastOptionParsed { get; private set; }
        public CmdLineParser OptionFormat(string format)
        {
            if (format == null)
                throw new ArgumentNullException(nameof(format));

            optionPrefix = Regex.Match(format, @"^(-+|/+)", RegexOptions.IgnoreCase).Value;
            if (optionPrefix.Length == 0)
                throw new FormatException($"Formato non valido: [{format}]");

            format = format.Substring(optionPrefix.Length);
            if (Regex.IsMatch(format, @"^X{1}\s+X{1}$", RegexOptions.IgnoreCase))
                optionValuePrefix = null;

            else if (Regex.IsMatch(format, @"^X{2}$", RegexOptions.IgnoreCase))
                optionValuePrefix = "";

            else if (Regex.IsMatch(format, @"^X{1}[^(a-zA-Z0-9\s)]+X{1}$", RegexOptions.IgnoreCase))
                optionValuePrefix = format.Substring(1, format.Length - 2);
            else
                throw new FormatException($"Formato non valido: [{format}]");

            return this;
        }
        public CmdLineParser HelpOnZeroArguments(bool value = true)
        {
            helpOnZeroArguments = value;
            return this;
        }
        public CmdLineParser OnError(Action<ParsingErrorInfo> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            errorHandler = handler;
            return this;
        }
        public CmdLineParser OnOption<T>(string opt, Action<T> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            CheckDoubleHandledOption(opt);
            //!converte Action<T> in Action<object>
            Action<object> act = new Action<object>(o => handler((T)o));
            optHandlers.Add(opt, new OptionInfo { Action = act, Type = typeof(T) });
            return this;
        }
        public CmdLineParser OnOption(string opt, Action handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            CheckDoubleHandledOption(opt);
            //!converte Action in Action<object>
            Action<object> act = new Action<object>(o => handler());
            optHandlers.Add(opt, new OptionInfo { Action = act, Type = null });
            return this;
        }
        public CmdLineParser OnArgument(Action<string> handler, ArgumentPolicy argPolicy = ArgumentPolicy.MultipleAllowed)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (argumentHandler != null)
                throw new InvalidOperationException("E' già stato registrato un handler per gli argomenti");

            this.argPolicy = argPolicy;
            argumentHandler = handler;
            return this;
        }
        public CmdLineParser OnHelp(string help, Action handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            helpHandler = handler;
            helps = help.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).ToArray();
            return this;
        }


        int argIndex = 0;
        private string GetNextArg()
        {
            if (argIndex < args.Length)
                return args[argIndex++];
            return null;
        }
        public ParseResult Parse()
        {
            if (args.Length == 0 && helpOnZeroArguments)
            {
                if (helpHandler == null)
                    throw new InvalidOperationException("Non è stato specificato un handler per l'help");
                helpHandler();
            }
            result = ParseResult.Success;
            oneArgumentParsed = false;
            var arg = GetNextArg();
            while (arg != null)
            {
                if (result == ParseResult.Error)
                    break;

                if (IsHelp(arg))
                {
                    helpHandler?.Invoke();
                    return ParseResult.Help;
                }

                if (arg.StartsWith(optionPrefix))
                    ParseOption(arg.Substring(optionPrefix.Length));
                else
                    ParseArgument(arg);
                arg = GetNextArg();
            }

            if (!oneArgumentParsed && argPolicy != ArgumentPolicy.Optional)
            {
                OnError(null, ParseErrorType.ArgomentRequired);
            }
            return result;
        }
        private bool IsHelp(string arg)
        {
            return helps.Contains(arg, IgnoreCaseComparer.Instance);
        }
        private void ParseArgument(string arg)
        {
            ArgParsingInfo argInfo = new ArgParsingInfo() { Text = arg };
            if (oneArgumentParsed && argPolicy != ArgumentPolicy.MultipleAllowed)
            {
                OnError(argInfo, ParseErrorType.MultipleArgumentNotAllowed);
                return;
            }
            oneArgumentParsed = true;

            if (argumentHandler == null)
                OnError(argInfo, ParseErrorType.UnandledArgument);
            else
                argumentHandler(arg);
            
        }

        //[-i10]  [-i:10]  [-i 10]
        private void ParseOption(string opt)
        {
            OptionInfo optInfo = default(OptionInfo);
            ArgParsingInfo argInfo = new ArgParsingInfo() { Text = opt, Name = opt, Value = null };
            bool found;
            if (!string.IsNullOrEmpty(optionValuePrefix))  // -i:10
                found = ParseOptionWithPrefix(opt, out optInfo, ref argInfo);

            else if (optionValuePrefix == null)  // -i 10
                found = ParseOptionWithoutPrefix(opt, out optInfo, ref argInfo);

            else  // -i10
                found = ParseOptionWithZeroPrefix(opt, out optInfo, ref argInfo);

            if (!found)
            {
                OnError(argInfo, ParseErrorType.UnknowOption);
                return;
            }

            if (!optInfo.HasValue)
            {
                if (!string.IsNullOrEmpty(argInfo.Value))
                {
                    OnError(argInfo, ParseErrorType.ValueNotAllowed);
                    return;
                }
                optInfo.Action(null);
            }
            else
            {
                if (string.IsNullOrEmpty(argInfo.Value))
                {
                    OnError(argInfo, ParseErrorType.ValueExpected);
                    return;
                }
                try
                {
                    object o = Convert.ChangeType(argInfo.Value, optInfo.Type);
                    optInfo.Action(o);
                    LastOptionParsed = opt;
                }
                catch (Exception e)
                {
                    OnError(argInfo, ParseErrorType.InvalidValue, e);
                };
            }
        }
        private bool ParseOptionWithPrefix(string opt, out OptionInfo optInfo, ref ArgParsingInfo argInfo)
        {
            var items = opt.Split(new string[] { optionValuePrefix }, StringSplitOptions.None);
            argInfo.Name = items[0];
            argInfo.Value = items.Length == 2 ? items[1] : null;
            return optHandlers.TryGetValue(items[0], out optInfo);
        }
        private bool ParseOptionWithoutPrefix(string opt, out OptionInfo optInfo, ref ArgParsingInfo argInfo)
        {
            bool found = optHandlers.TryGetValue(opt, out optInfo);
            argInfo.Value = (found && optInfo.HasValue) ? GetNextArg() : null;
            return found;
        }
        private bool ParseOptionWithZeroPrefix(string opt, out OptionInfo optInfo, ref ArgParsingInfo argInfo)
        {
            optInfo = default(OptionInfo);
            foreach (var key in optHandlers.Keys)
            {
                if (StartWith(opt, key))
                {
                    argInfo.Value = opt.Substring(key.Length);
                    optInfo = optHandlers[key];
                    return true;
                }
            }
            return false;
        }
        private void CheckDoubleHandledOption(string opt)
        {
            if (optHandlers.ContainsKey(opt))
                throw new InvalidOperationException("E' già stato registrato un gestore per l'opzione: " + opt);
        }
        private bool StartWith(string target, string value)
        {
            if (comparer == IgnoreCaseComparer.Instance)
                return target.StartsWith(value);
            return target.StartsWith(value, StringComparison.InvariantCultureIgnoreCase);
        }
        private void OnError(ArgParsingInfo arg, ParseErrorType error, Exception ex = null)
        {
            result = ParseResult.Error;
            Error = new ParsingErrorInfo { Arg = arg, ErrorType = error, CmdLine = this.CmdLine, Exception = ex };
            errorHandler(Error);
        }
    }

    struct OptionInfo
    {
        public Type Type;
        public Action<object> Action;
        public bool HasValue { get { return Type != null; } }
    }

    public class ArgParsingInfo
    {
        public string Text;
        public string Name;
        public string Value;
    }
    public class ParsingErrorInfo
    {
        public string CmdLine;
        public ArgParsingInfo Arg;
        public ParseErrorType ErrorType;
        public Exception Exception;
    }
    public enum ParseErrorType
    {
        UnknowOption,
        ValueExpected,
        ValueNotAllowed,
        InvalidValue,
        MultipleArgumentNotAllowed,
        UnandledArgument,
        ArgomentRequired
    }

    public enum ParseResult
    {
        Success,
        Help,
        Error
    }

    public enum ArgumentPolicy
    {
        Optional,
        Once,
        MultipleAllowed
    }

    public class IgnoreCaseComparer : IEqualityComparer<string>
    {
        public static readonly IEqualityComparer<string> Instance = new IgnoreCaseComparer();
        public bool Equals(string x, string y)
        {
            return x.Equals(y, StringComparison.InvariantCultureIgnoreCase);
        }
        public int GetHashCode(string obj)
        {
            return obj.ToUpper().GetHashCode();
        }
    }

}
