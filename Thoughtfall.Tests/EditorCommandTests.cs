namespace Thoughtfall.Tests;

using NUnit.Framework;
using Thoughtfall.Cli;

/// <summary>Checks shell-free editor invocation.</summary>
public sealed class EditorCommandTests
{
    /// <summary>Preserves quoted executables, arguments, and a separate draft filename.</summary>
    [Test]
    public void CreatePreservesArguments()
    {
        var result = EditorCommand.Create(@"""C:\Program Files\Editor\edit.exe"" --wait ""two words""", @"C:\draft folder\note.txt");
        Assert.Multiple(() =>
        {
            Assert.That(result.FileName, Is.EqualTo(@"C:\Program Files\Editor\edit.exe"));
            Assert.That(result.ArgumentList, Is.EqualTo(new[] { "--wait", "two words", @"C:\draft folder\note.txt" }));
            Assert.That(result.UseShellExecute, Is.False);
        });
    }

    /// <summary>Rejects empty commands and unmatched quotes.</summary>
    /// <param name="command">The invalid editor command.</param>
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("editor 'unfinished")]
    [TestCase("\"\"")]
    public void CreateRejectsInvalidCommand(string command) => Assert.Throws<ArgumentException>(() => EditorCommand.Create(command, "draft.txt"));

    /// <summary>Leaves shell operators as literal arguments.</summary>
    [Test]
    public void CreateDoesNotInterpretShellSyntax()
    {
        var result = EditorCommand.Create("editor --wait ';' '$HOME'", "draft.txt");
        Assert.That(result.ArgumentList, Is.EqualTo(new[] { "--wait", ";", "$HOME", "draft.txt" }));
    }
}
