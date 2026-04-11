using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.IntelliCode.Api.WholeLineCompletion;
using Microsoft.VisualStudio.IntelliCode.WholeLineCompletion.ModelInference;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IntelliCodeLsp;

class Program
{
    // Path to the model
    private const string ModelArchivePath = 
        @"E:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\Extensions\Microsoft\IntelliCode\BundledModels\all_line-completion2";
    private const string ModelExtractionPath = 
        @"C:\Users\daniel\AppData\Local\Microsoft\VisualStudio\18.0_f5fd3385\IntelliCodeModels\all_line-completion2_ExtractedData";

    private static readonly CodeGenerationConfig Config = new CodeGenerationConfig
    {
        OutputSeqLength  = 20,
        EodToken         = 1,
        BeamSize         = 4,
        NumberOfResults  = 4,
        MaxSequenceLength = 256
    };

    private static readonly Dictionary<string, string> Documents = new();

    private static CodeGenerator? _generator;

    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding  = Encoding.UTF8;

        var stdin  = new BinaryReader(Console.OpenStandardInput());
        var stdout = Console.OpenStandardOutput();

        try
        {
            var ctx = new LocalSystemContext();
            _generator = new CodeGenerator(ctx);
            _generator.Initialize(ModelArchivePath, ModelExtractionPath, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[IntelliCodeLsp] Model init failed: {ex.Message}");
        }

        while (true)
        {
            try
            {
                string? json = ReadMessage(stdin);
                if (json == null) break;

                var msg = JsonConvert.DeserializeObject<LspMessage>(json);
                if (msg == null) continue;

                HandleMessage(msg, stdout);
            }
            catch (EndOfStreamException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[IntelliCodeLsp] Error: {ex}");
            }
        }
    }

    static void HandleMessage(LspMessage msg, Stream stdout)
    {
        switch (msg.Method)
        {
            case "initialize":
                SendResult(stdout, msg.Id, new InitializeResult());
                break;

            case "initialized":
                break;

            case "shutdown":
                SendResult(stdout, msg.Id, null);
                break;

            case "exit":
                Environment.Exit(0);
                break;

            case "textDocument/didOpen":
            {
                var p = msg.Params!.ToObject<DidOpenTextDocumentParams>()!;
                Documents[p.TextDocument.Uri] = p.TextDocument.Text;
                break;
            }

            case "textDocument/didChange":
            {
                var p = msg.Params!.ToObject<DidChangeTextDocumentParams>()!;
                if (p.ContentChanges.Length > 0)
                    Documents[p.TextDocument.Uri] = p.ContentChanges[^1].Text;
                break;
            }

            case "textDocument/didClose":
            {
                var p = msg.Params!.ToObject<DidOpenTextDocumentParams>()!;
                Documents.Remove(p.TextDocument.Uri);
                break;
            }

            case "textDocument/completion":
            {
                var p    = msg.Params!.ToObject<TextDocumentPositionParams>()!;
                var items = GetCompletions(p);
                SendResult(stdout, msg.Id, items);
                break;
            }

            default:
                break;
        }
    }

    static List<CompletionItem> GetCompletions(TextDocumentPositionParams p)
    {
        var result = new List<CompletionItem>();

        if (_generator == null)
            return result;

        if (!Documents.TryGetValue(p.TextDocument.Uri, out string? docText))
            return result;

        string context = ContextExtractor.GetContext(docText, p.Position.Line, p.Position.Character);
        if (string.IsNullOrWhiteSpace(context))
            return result;

        try
        {
            WholeLineResponse response = _generator.Execute(
                new WholeLineRequest { Contexts = new System.Collections.Generic.List<string> { context } },
                Config,
                CancellationToken.None);

            if (response?.Generations == null || response.Generations.Count == 0)
                return result;

            for (int i = 0; i < response.Generations[0].Count; i++)
            {
                string[]? tokens = response.Generations[0][i];
                if (tokens == null || tokens.Length == 0) continue;

                string? text = Denormalizer.Denormalize(tokens);
                if (string.IsNullOrEmpty(text)) continue;

                float prob = response.LogProbs[0][i].Length > 0 
                    ? response.LogProbs[0][i][0] 
                    : 0f;

                result.Add(new CompletionItem
                {
                    Label        = text,
                    InsertText   = text,
                    Kind         = 1,
                    Detail       = $"IntelliCode ({Math.Exp(prob):P0})",
                    Documentation = "IntelliCode whole-line completion"
                });
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[IntelliCodeLsp] Inference error: {ex.Message}");
        }

        return result;
    }

    static string? ReadMessage(BinaryReader reader)
    {
        int contentLength = -1;

        while (true)
        {
            string line = ReadLine(reader);
            if (line.Length == 0) break;

            if (line.StartsWith("Content-Length: ", StringComparison.OrdinalIgnoreCase))
                contentLength = int.Parse(line["Content-Length: ".Length..]);
        }

        if (contentLength < 0) return null;

        byte[] body = reader.ReadBytes(contentLength);
        return Encoding.UTF8.GetString(body);
    }

    static string ReadLine(BinaryReader reader)
    {
        var sb = new StringBuilder();
        while (true)
        {
            byte b = reader.ReadByte();
            if (b == '\n') break;
            if (b != '\r') sb.Append((char)b);
        }
        return sb.ToString();
    }

    static void SendResult(Stream stdout, JToken? id, object? result)
    {
        var msg = new LspMessage
        {
            Id     = id,
            Result = result == null ? JValue.CreateNull() : JToken.FromObject(result)
        };
        SendMessage(stdout, msg);
    }

    static void SendMessage(Stream stdout, LspMessage msg)
    {
        string json  = JsonConvert.SerializeObject(msg);
        byte[] body  = Encoding.UTF8.GetBytes(json);
        byte[] header = Encoding.ASCII.GetBytes($"Content-Length: {body.Length}\r\n\r\n");

        stdout.Write(header, 0, header.Length);
        stdout.Write(body,   0, body.Length);
        stdout.Flush();
    }
}
