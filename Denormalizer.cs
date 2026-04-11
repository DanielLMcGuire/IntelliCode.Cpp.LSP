using System;

namespace IntelliCodeLsp;

public static class Denormalizer
{
    public static string? Denormalize(string[] tokens)
    {
        var parts = new System.Text.StringBuilder();

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];

            if (token is "<endofline>" or "<endoftext>" or "<endoffunc>")
                break;

            if (token == "<unk>")
                break;

            if (token is "<INDENT>" or "<DEDENT>")
                continue;

            if (token.StartsWith("<NUM_LIT:") && token.EndsWith(">"))
            {
                parts.Append(token[9..^1]);
                continue;
            }

            if (token == "<NUM_LIT>")
                return null;

            if (token.StartsWith("<STR_LIT:") && token.EndsWith(">"))
            {
                string inner = token[9..^1]
                    .Replace("U+0020", " ")
                    .Replace("U+002C", ",");
                parts.Append('"');
                parts.Append(inner);
                parts.Append('"');
                continue;
            }

            if (token == "<STR_LIT>")
                return null;

            if (token.StartsWith("<CHAR_LIT:") && token.EndsWith(">"))
            {
                string inner = token[10..^1];
                inner = inner switch
                {
                    "U+0020" => " ",
                    "U+002C" => ",",
                    _ when inner.StartsWith("U+") => "\\u" + inner[2..],
                    _ => inner
                };
                parts.Append('\'');
                parts.Append(inner);
                parts.Append('\'');
                continue;
            }

            if (token == "<CHAR_LIT>")
                return null;

            parts.Append(token);
        }

        while (parts.Length > 0 && char.IsWhiteSpace(parts[parts.Length - 1]))
            parts.Length--;

        return parts.Length > 0 ? parts.ToString() : null;
    }
}
