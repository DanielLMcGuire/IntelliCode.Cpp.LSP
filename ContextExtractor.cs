using System;
namespace IntelliCodeLsp;

public static class ContextExtractor
{
    private const int MaxContextLines = 50;

    public static string GetContext(string documentText, int line, int character)
    {
        string[] lines = documentText.Split('\n');

        if (line >= lines.Length)
            return "";

        string currentLine = lines[line];
        if (character <= currentLine.Length)
            lines[line] = currentLine.Substring(0, character);

        int startLine = Math.Max(0, line - MaxContextLines);
        string[] contextLines = lines[startLine..(line + 1)];

        return string.Join("\n", contextLines);
    }
}
