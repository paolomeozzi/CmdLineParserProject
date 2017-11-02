# CmdLineParserProject

Il progetto definisce una classe, **CmdLineParser**, in grado di eseguire il parsing degli argomenti della linea di comando di un'applicazione console.
## Esempio d'uso della classe

Supponiamo di voler analizzare la linea di comando di un'applicazione simile a Tracert (Windows). La linea di comando ha il seguente formato (per brevità riporto soltanto 3 opzioni, più la destinazione):
```
tracert [-d] [-h max_salti] [-w timeout] destinazione
```
La prima opzione esprime un valore booleano, mentre -h e -w specificano dei valori interi, rispettivamente il massimo numero di *hop* e il timeout espresso in millisecondi.
Ecco come è possibile validare la riga di comando ed estrarne i parametri per memorizzari in variabili del programma:


```C#
public class TraceOptions
{
    public string HostName;
    public bool SuppressHostnameResolution = false;
    public int Timeout = 2000;
    public int MaxHops = 20;
}

//...

public void Test()
{
    //simula parametri linea di comando
    string[] args = new string[] { "www.google.it", "-d", "-w", "1000", "-h", "17" };
    var ra = new TraceOptions();
    var success = new CmdLineParser(args)
        .OptionFormat("-x x")
        .OnArgument(hn => ra.HostName = hn)
        .OnOption("d", () => ra.SuppressHostnameResolution = true)
        .OnOption<int>("w", to => ra.Timeout = to)
        .OnOption<int>("h", mh => ra.MaxHops = mh)        
        .Parse() == ParseResult.Success;

    Assert.IsTrue(success);
    Assert.AreEqual(true, ra.SuppressHostnameResolution);
    Assert.AreEqual(1000, ra.Timeout);
    Assert.AreEqual(17, ra.MaxHops);
}
```
## Metodi di "configurazione"
Tutti i metodi della classe, eccetto `Parse()`,  servono alla configurazione dell'oggetto di parsing. In sintesi:
- `OptionFormat()`: permette di stabilire il prefisso e il formato delle opzioni. Nell'esempio viene usato il formato tipico di Windows, nel quale il valore dell'opzione è memorizzato nell'argomento successivo.
- I metodi `On<Argument|Option>()`: consentono di stabilire l'azione da eseguire quando il parser incontra un argomento o un'opzione

(La classe fornisce altre opzioni di configurazione, che consentono di stabilire se ignorare il case, accettare argomenti multipli, impostare un handler per gli errori, stabilire l'opzione di help, eccetera.

## Parsing
E' il metodo `Parse()` ad eseguire il parsing; restituisce un valore del tipo `ParseResult`, che stabilisce se l'operazione ha avuto successo o è stato riscontrato un errore.


