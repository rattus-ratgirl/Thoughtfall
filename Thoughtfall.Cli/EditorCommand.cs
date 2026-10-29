namespace Thoughtfall.Cli;

using System.Diagnostics;
using System.Text;

/// <summary>Parses editor executables and arguments without invoking a shell.</summary>
public static class EditorCommand
{
    /// <summary>Builds an editor invocation with a separate draft argument.</summary>
    /// <param name="command">The configured editor command.</param>
    /// <param name="draft">The draft filename.</param>
    /// <returns>The editor process configuration.</returns>
    public static ProcessStartInfo Create(string command, string draft)
    {
        var words = new List<string>();
        var word = new StringBuilder();
        char quote = '\0';
        var started = false;
        foreach (var character in command)
        {
            if (quote != '\0')
            {
                if (character == quote)
                    quote = '\0';
                else
                    word.Append(character);
            }
            else if (character is '\'' or '"')
            {
                quote = character;
                started = true;
            }
            else if (char.IsWhiteSpace(character))
            {
                if (started)
                {
                    words.Add(word.ToString());
                    word.Clear();
                    started = false;
                }
            }
            else
            {
                word.Append(character);
                started = true;
            }
        }

        if (quote != '\0')
            throw new ArgumentException("The editor command has an unclosed quote.");
        if (started)
            words.Add(word.ToString());
        if (words.Count == 0 || string.IsNullOrWhiteSpace(words[0]))
            throw new ArgumentException("The editor command is empty.");
        var result = new ProcessStartInfo(words[0]) { UseShellExecute = false };
        foreach (var argument in words.Skip(1))
            result.ArgumentList.Add(argument);
        result.ArgumentList.Add(draft);
        return result;
    }
}
