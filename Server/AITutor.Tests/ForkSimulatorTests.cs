using AITutor.Core.Subjects.OperatingSystems;

namespace AITutor.Tests;

public class ForkSimulatorTests
{
    [Theory]
    [InlineData("fork(); fork(); fork();", 8)]
    [InlineData("fork();", 2)]
    [InlineData("if(fork()) if(fork()) if(fork());", 4)]
    [InlineData("if(fork()) if(fork()) fork(); execle(\"./a.out\", \"./a.out\", NULL);", 4)]
    [InlineData("for (i = 0; i < 3; i++) fork();", 8)]
    [InlineData("for (int i = 0; i < 4; i++) { fork(); }", 16)]
    [InlineData("pid = fork(); if (pid == 0) { printf(\"dete\"); } else { printf(\"roditelj\"); }", 2)]
    [InlineData("if (fork() && fork()) fork();", 4)]
    [InlineData("if (fork() || fork()) fork();", 5)]
    [InlineData("if (fork() == 0) { fork(); exit(0); } fork();", 4)]
    [InlineData("int pid = fork(); if (!pid) fork(); fork();", 6)]
    public void Counts_processes_for_classic_exam_snippets(string code, int expected)
    {
        var result = ForkSimulator.TryCount("Koliko procesa se generise sledecim kodom? " + code);
        Assert.NotNull(result);
        Assert.Equal(expected, result!.Processes);
    }

    [Fact]
    public void Reads_a_full_program_with_includes_and_main_and_ignores_the_trailing_sentence()
    {
        const string task = "Koliko procesa se generise sledecim programskim kodom. Obrazloziti ukratko! #include <stdio.h> #include <unistd.h> int main() { if(fork()) if(fork()) if(fork()); return 0; } Nacrtati stablo procesa.";
        Assert.Equal(4, ForkSimulator.TryCount(task)!.Processes);
    }

    [Theory]
    [InlineData("Koliko je 2+2?")]
    [InlineData("Sta radi fork()?")]
    [InlineData("while (1) fork();")]
    [InlineData("for (i = 0; i < 3; i++) { if (fork()) break; }")]
    public void Unsupported_or_non_code_input_returns_null(string text) => Assert.Null(ForkSimulator.TryCount(text));
}
