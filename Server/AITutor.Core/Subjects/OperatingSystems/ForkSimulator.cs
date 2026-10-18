using System.Text.RegularExpressions;

namespace AITutor.Core.Subjects.OperatingSystems;

public record ForkResult(int Processes);

// Broji procese koje stvara C kod sa fork(): izvršava kod za svaki proces posebno (grananje na roditelja i dete).
// Podržano: fork(), pid = fork(), if/else, for, while, &&, ||, !, ==, !=, <, >, exit/exec*/return (završavaju proces),
// pozivi ostalih funkcija (printf, wait...) se ignorišu. Sve ostalo (break, goto, nizovi...) -> null.
public static class ForkSimulator
{
    public static ForkResult? TryCount(string task)
    {
        if (!task.Contains("fork", StringComparison.OrdinalIgnoreCase)) return null;
        try
        {
            var code = ExtractCode(task);
            if (code == null) return null;
            var program = new Parser(Tokenize(code)).ParseProgram();
            return new ForkResult(Run(program));
        }
        catch (Exception)
        {
            return null;
        }
    }

    // ---- izdvajanje koda iz teksta zadatka ----

    private static string? ExtractCode(string task)
    {
        var text = Regex.Replace(task, @"#\s*include\s*(<[^>]*>|""[^""]*"")", " ");
        var main = Regex.Match(text, @"\bmain\s*\([^)]*\)\s*\{");
        if (main.Success) return text[(main.Index + main.Length - 1)..];
        var start = Regex.Match(text, @"(?<![\w])(if\s*\(|for\s*\(|while\s*\(|fork\s*\(|\w+\s*=\s*fork)");
        return start.Success ? "{" + text[start.Index..] + "}" : null;
    }

    // ---- tokeni ----

    private static readonly Regex TokenRegex = new(
        @"\G\s*(==|!=|<=|>=|&&|\|\||\+\+|--|\+=|-=|[A-Za-z_]\w*|\d+|""(?:[^""\\]|\\.)*""|[(){};=<>!+\-*,])",
        RegexOptions.Compiled);

    private static List<string> Tokenize(string code)
    {
        var tokens = new List<string>();
        var pos = 0;
        while (pos < code.Length)
        {
            if (code.AsSpan(pos).Trim().IsEmpty) break;
            var m = TokenRegex.Match(code, pos);
            if (!m.Success) break; // tekst posle koda (npr. rečenica zadatka) se ignoriše
            tokens.Add(m.Groups[1].Value);
            pos = m.Index + m.Length;
        }
        return tokens;
    }

    // ---- sintaksno stablo ----

    private abstract record Expr;
    private sealed record Num(int V) : Expr;
    private sealed record Var(string Name) : Expr;
    private sealed record ForkCall : Expr;
    private sealed record OtherCall : Expr;
    private sealed record Unary(string Op, Expr E) : Expr;
    private sealed record Binary(string Op, Expr L, Expr R) : Expr;

    private abstract record Stmt;
    private sealed record Block(List<Stmt> Stmts) : Stmt;
    private sealed record If(Expr Cond, Stmt Then, Stmt? Else) : Stmt;
    private sealed record While(Expr Cond, Stmt Body) : Stmt;
    private sealed record Assign(string Name, Expr Value) : Stmt;
    private sealed record ExprStmt(Expr E) : Stmt;
    private sealed record Terminate : Stmt;
    private sealed record Nop : Stmt;

    private sealed class Parser
    {
        private readonly List<string> _t;
        private int _p;

        public Parser(List<string> tokens) => _t = tokens;

        private string? Peek => _p < _t.Count ? _t[_p] : null;
        private string Next() => _p < _t.Count ? _t[_p++] : throw new FormatException("Neočekivan kraj");
        private bool Accept(string s) { if (Peek == s) { _p++; return true; } return false; }
        private void Expect(string s) { if (!Accept(s)) throw new FormatException($"Očekivano {s}"); }

        public Stmt ParseProgram()
        {
            Expect("{");
            return ParseBlockBody();
        }

        // Posle prvog uspešno pročitanog iskaza, nerazumljiv ostatak (npr. tekst zadatka posle koda) se ignoriše.
        private Stmt ParseBlockBody()
        {
            var list = new List<Stmt>();
            while (Peek != null && Peek != "}")
            {
                var save = _p;
                try { list.Add(ParseStmt()); }
                catch (FormatException) when (list.Count > 0) { _p = save; break; }
            }
            Accept("}");
            return new Block(list);
        }

        private Stmt ParseStmt()
        {
            var tok = Peek ?? throw new FormatException("Kraj");
            if (Accept("{")) return ParseBlockBody();
            if (Accept(";")) return new Nop();

            switch (tok)
            {
                case "if":
                {
                    _p++; Expect("(");
                    var cond = ParseExpr(); Expect(")");
                    var then = ParseStmt();
                    Stmt? els = Accept("else") ? ParseStmt() : null;
                    return new If(cond, then, els);
                }
                case "while":
                {
                    _p++; Expect("(");
                    var cond = ParseExpr(); Expect(")");
                    return new While(cond, ParseStmt());
                }
                case "for":
                {
                    _p++; Expect("(");
                    var init = Peek == ";" ? new Nop() : ParseSimple(); Expect(";");
                    var cond = Peek == ";" ? new Num(1) : ParseExpr(); Expect(";");
                    var incr = Peek == ")" ? new Nop() : ParseSimple(); Expect(")");
                    var body = ParseStmt();
                    return new Block(new List<Stmt> { init, new While(cond, new Block(new List<Stmt> { body, incr })) });
                }
                case "return":
                    _p++;
                    if (Peek != ";") ParseExpr();
                    Expect(";");
                    return new Terminate();
                case "break" or "continue" or "goto" or "switch" or "do":
                    throw new NotSupportedException(tok);
            }

            if (tok == "exit" || tok.StartsWith("exec", StringComparison.Ordinal) || tok == "_exit")
            {
                _p++;
                SkipParens();
                Expect(";");
                return new Terminate();
            }

            var s = ParseSimple();
            Expect(";");
            return s;
        }

        private void SkipParens()
        {
            Expect("(");
            var depth = 1;
            while (depth > 0)
            {
                var t = Next();
                if (t == "(") depth++;
                else if (t == ")") depth--;
            }
        }

        private static readonly HashSet<string> TypeWords = new() { "int", "pid_t", "long", "char", "unsigned", "short" };

        private Stmt ParseSimple()
        {
            while (Peek != null && TypeWords.Contains(Peek)) _p++;
            var tok = Peek ?? throw new FormatException("Kraj");
            var isName = Regex.IsMatch(tok, @"^[A-Za-z_]\w*$");

            if (isName && _p + 1 < _t.Count)
            {
                var after = _t[_p + 1];
                if (after == "=") { _p += 2; return new Assign(tok, ParseExpr()); }
                if (after == "++") { _p += 2; return new Assign(tok, new Binary("+", new Var(tok), new Num(1))); }
                if (after == "--") { _p += 2; return new Assign(tok, new Binary("-", new Var(tok), new Num(1))); }
                if (after == "+=") { _p += 2; return new Assign(tok, new Binary("+", new Var(tok), ParseExpr())); }
                if (after == "-=") { _p += 2; return new Assign(tok, new Binary("-", new Var(tok), ParseExpr())); }
            }
            if (isName && (_p + 1 >= _t.Count || _t[_p + 1] is ";" or "," or ")")) { _p++; return new Nop(); } // deklaracija bez vrednosti
            return new ExprStmt(ParseExpr());
        }

        private Expr ParseExpr() => ParseOr();

        private Expr ParseOr()
        {
            var l = ParseAnd();
            while (Accept("||")) l = new Binary("||", l, ParseAnd());
            return l;
        }

        private Expr ParseAnd()
        {
            var l = ParseEq();
            while (Accept("&&")) l = new Binary("&&", l, ParseEq());
            return l;
        }

        private Expr ParseEq()
        {
            var l = ParseRel();
            while (Peek is "==" or "!=") { var op = Next(); l = new Binary(op, l, ParseRel()); }
            return l;
        }

        private Expr ParseRel()
        {
            var l = ParseAdd();
            while (Peek is "<" or ">" or "<=" or ">=") { var op = Next(); l = new Binary(op, l, ParseAdd()); }
            return l;
        }

        private Expr ParseAdd()
        {
            var l = ParseUnary();
            while (Peek is "+" or "-") { var op = Next(); l = new Binary(op, l, ParseUnary()); }
            return l;
        }

        private Expr ParseUnary()
        {
            if (Accept("!")) return new Unary("!", ParseUnary());
            if (Accept("-")) return new Unary("-", ParseUnary());
            return ParsePrimary();
        }

        private Expr ParsePrimary()
        {
            var tok = Next();
            if (tok == "(") { var e = ParseExpr(); Expect(")"); return e; }
            if (int.TryParse(tok, out var n)) return new Num(n);
            if (tok.StartsWith('"')) return new Num(1);
            if (Regex.IsMatch(tok, @"^[A-Za-z_]\w*$"))
            {
                if (Peek == "(")
                {
                    SkipParens();
                    return tok == "fork" ? new ForkCall() : new OtherCall();
                }
                return tok == "NULL" ? new Num(0) : new Var(tok);
            }
            throw new FormatException($"Neočekivano {tok}");
        }
    }

    // ---- izvršavanje ----

    private sealed class Proc
    {
        public List<Stmt> Pending = new();
        public Dictionary<string, int> Vars = new();

        public Proc Clone() => new() { Pending = new List<Stmt>(Pending), Vars = new Dictionary<string, int>(Vars) };
    }

    // Vrednost izraza za svaki proces koji nastaje njegovim izračunavanjem: fork() daje [roditelj=1, dete=0].
    private static List<int> Eval(Expr e, Proc p)
    {
        switch (e)
        {
            case Num n: return new List<int> { n.V };
            case Var v: return new List<int> { p.Vars.GetValueOrDefault(v.Name) };
            case OtherCall: return new List<int> { 1 };
            case ForkCall: return new List<int> { 1, 0 };
            case Unary u:
                return Eval(u.E, p).Select(x => u.Op == "!" ? (x == 0 ? 1 : 0) : -x).ToList();
            case Binary { Op: "&&" } b:
                return Eval(b.L, p).SelectMany(l => l == 0 ? new List<int> { 0 } : Eval(b.R, p).Select(r => r != 0 ? 1 : 0).ToList()).ToList();
            case Binary { Op: "||" } b:
                return Eval(b.L, p).SelectMany(l => l != 0 ? new List<int> { 1 } : Eval(b.R, p).Select(r => r != 0 ? 1 : 0).ToList()).ToList();
            case Binary b:
                return Eval(b.L, p).SelectMany(l => Eval(b.R, p).Select(r => b.Op switch
                {
                    "+" => l + r, "-" => l - r,
                    "==" => l == r ? 1 : 0, "!=" => l != r ? 1 : 0,
                    "<" => l < r ? 1 : 0, ">" => l > r ? 1 : 0, "<=" => l <= r ? 1 : 0, ">=" => l >= r ? 1 : 0,
                    _ => throw new NotSupportedException(b.Op),
                })).ToList();
            default: throw new NotSupportedException();
        }
    }

    private static int Run(Stmt program)
    {
        var queue = new Queue<Proc>();
        queue.Enqueue(new Proc { Pending = new List<Stmt> { program } });
        var total = 1;
        var steps = 0;

        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            while (p.Pending.Count > 0)
            {
                if (++steps > 200_000) throw new InvalidOperationException("Program se ne završava");
                var s = p.Pending[0];
                p.Pending.RemoveAt(0);

                switch (s)
                {
                    case Block b: p.Pending.InsertRange(0, b.Stmts); break;
                    case Nop: break;
                    case Terminate: p.Pending.Clear(); break;
                    case ExprStmt es: Branch(p, Eval(es.E, p), (_, __) => { }); break;
                    case Assign a: Branch(p, Eval(a.Value, p), (q, v) => q.Vars[a.Name] = v); break;
                    case If i:
                        Branch(p, Eval(i.Cond, p), (q, v) =>
                        {
                            var chosen = v != 0 ? i.Then : i.Else;
                            if (chosen != null) q.Pending.Insert(0, chosen);
                        });
                        break;
                    case While w:
                        Branch(p, Eval(w.Cond, p), (q, v) =>
                        {
                            if (v != 0) { q.Pending.Insert(0, w); q.Pending.Insert(0, w.Body); }
                        });
                        break;
                    default: throw new NotSupportedException();
                }
            }
        }
        return total;

        // Prvi ishod primenjuje se na proces p, a za svaki dodatni ishod (novi proces od fork()) pravi se kopija.
        void Branch(Proc p0, List<int> outcomes, Action<Proc, int> apply)
        {
            var clones = outcomes.Skip(1).Select(_ => p0.Clone()).ToList();
            apply(p0, outcomes[0]);
            for (var k = 0; k < clones.Count; k++)
            {
                apply(clones[k], outcomes[k + 1]);
                queue.Enqueue(clones[k]);
            }
            total += clones.Count;
        }
    }
}
