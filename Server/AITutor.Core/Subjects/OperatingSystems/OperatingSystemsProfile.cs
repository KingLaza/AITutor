using System.Text.RegularExpressions;

namespace AITutor.Core.Subjects.OperatingSystems;

// Dodaci za Operativne sisteme: tačno rešavanje proračuna (raspoređivanje, disk, stranice, i-node, adrese, Banker, fork),
// obrasci za zadatke sa semaforima i monitorima i primeri za klasifikator.
public class OperatingSystemsProfile : ISubjectProfile
{
    private static readonly Regex SyncTaskPattern = new("monitor|semafor|semaphore|sinhroniz", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private const string SyncNotation = """
        NOTACIJA IZ PREDAVANJA (piši pseudo-kod u ovoj notaciji, bez POSIX funkcija i bez printf):
        - semafori: semaphore s = 1;  semWait(s);  semSignal(s);  procesi se pokreću sa parbegin(P1, P2);
        - monitor: monitor Ime { deljene promenljive; condition c; procedure p() { ...; cwait(c); ...; csignal(c); } inicijalizacija }
        - cwait(c) blokira proces na uslovnoj promenljivoj i oslobađa monitor; csignal(c) budi jedan blokirani proces.
        - monitor sam obezbeđuje uzajamno isključivanje, pa unutar njega ne trebaju dodatni semafori.
        """;

    // solverOverride služi testovima; u radu se koristi rešavač koji parametre izvlači pomoću modela.
    public OperatingSystemsProfile(IChatService chat, ITaskSolver? solverOverride = null) =>
        Solver = solverOverride ?? new OsTaskSolver(chat);

    public string Name => "Operativni sistemi";

    // Izmereno: pitanja iz domena do 0.638, van domena od 0.496; sivu zonu presuđuje model kroz prompt.
    public double MaxDistance => 0.64;

    public ITaskSolver? Solver { get; }

    // Zadaci sa semaforima i monitorima se na ispitu pišu kao pseudo-kod iz predavanja, a laboratorijske su POSIX C,
    // pa se primeri biraju iz proverenih obrazaca (dodatak, source_type "obrasci") i sa slajdova predavanja.
    public TaskGuidance? GetGuidance(string task) =>
        SyncTaskPattern.IsMatch(task)
            ? new TaskGuidance(SyncNotation, new[] { "obrasci" }, 2, new[] { "predavanja" }, 1)
            : null;

    public IReadOnlyList<string> ClassifierExamples { get; } = new[]
    {
        "\"Koji proces će se izvršavati u periodu 60-70ms ako se raspoređivanje obavlja po SJF, SRTF i Round Robin?\" -> TASK",
        "\"Napisati shell skriptu koja prebroji linije u svim .c fajlovima\" -> TASK",
        "\"Na sledeca pitanja odgovoriti sa Da ili Ne + objasnjenje: Niti istog procesa dele stek.\" -> SUBJECT",
        "\"U kom modu je sistem nakon sto proces pozove pthread_create()?\" -> SUBJECT",
    };
}
