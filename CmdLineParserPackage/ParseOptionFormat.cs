using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CmdLineParserPackage
{
    /* Classe di sviluppo usata per sperimentare l'impostazione del formato delle opzioni
       mediante un pattern da interpretare con delle regular expression
       L'analisi mediante RegEx avviene in due fasi:

       - la prima, uguale per tutti i casi, ottiene il prefisso dell'opzioni: ^(-+|/+)
       - la seconda dipende dal formato, che può essere:

         X<prefisso>X  (nome e valore separati da un prefisso di qualsiasi lunghezza)
         XX   (nome e valore attaccati)
         X X  (nome e valore memorizzati in argomenti distinti, ma adiacenti, della rida di comando
    */

    public class ParseOptionFormat
    {
        public string optionPrefix = null;
        public string optionValuePrefix = null;

        // -X X  ->  ^(-+|/+)    ^X{1}\s+X{1}$
        // -XX   ->  ^(-+|/+)    ^X{2}$
        // -X:X  ->  ^(-+|/+)    ^X{1}[^(a-zA-Z0-9\s)]+X{1}$

        public void Parse(string format)
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
                optionValuePrefix = format.Substring(1, format.Length-2);
            else
                throw new FormatException($"Formato non valido: [{format}]");

        }
    }
}
