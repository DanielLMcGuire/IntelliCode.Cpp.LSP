using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IntelliCodeLsp;

public class LspMessage
{
    [JsonProperty("jsonrpc")] public string JsonRpc { get; set; } = "2.0";
    [JsonProperty("id")]      public JToken? Id { get; set; }
    [JsonProperty("method")]  public string? Method { get; set; }
    [JsonProperty("params")]  public JToken? Params { get; set; }
    [JsonProperty("result")]  public JToken? Result { get; set; }
    [JsonProperty("error")]   public JToken? Error { get; set; }
}

public class InitializeResult
{
    [JsonProperty("capabilities")] public ServerCapabilities Capabilities { get; set; } = new();
}

public class ServerCapabilities
{
    [JsonProperty("completionProvider")] public CompletionOptions CompletionProvider { get; set; } = new();
}

public class CompletionOptions
{
    [JsonProperty("triggerCharacters")] public string[] TriggerCharacters { get; set; } 
        = new[] { ".", "(", " ", "\n", ">" };
}

public class TextDocumentPositionParams
{
    [JsonProperty("textDocument")] public TextDocumentIdentifier TextDocument { get; set; } = new();
    [JsonProperty("position")]     public Position Position { get; set; } = new();
}

public class TextDocumentIdentifier
{
    [JsonProperty("uri")] public string Uri { get; set; } = "";
}

public class Position
{
    [JsonProperty("line")]      public int Line { get; set; }
    [JsonProperty("character")] public int Character { get; set; }
}

public class CompletionItem
{
    [JsonProperty("label")]         public string Label { get; set; } = "";
    [JsonProperty("kind")]          public int Kind { get; set; } = 1; // Text
    [JsonProperty("insertText")]    public string InsertText { get; set; } = "";
    [JsonProperty("detail")]        public string? Detail { get; set; }
    [JsonProperty("documentation")] public string? Documentation { get; set; }
}

public class DidOpenTextDocumentParams
{
    [JsonProperty("textDocument")] public TextDocumentItem TextDocument { get; set; } = new();
}

public class DidChangeTextDocumentParams
{
    [JsonProperty("textDocument")]   public TextDocumentIdentifier TextDocument { get; set; } = new();
    [JsonProperty("contentChanges")] public TextDocumentContentChangeEvent[] ContentChanges { get; set; } = Array.Empty<TextDocumentContentChangeEvent>();
}

public class TextDocumentItem
{
    [JsonProperty("uri")]        public string Uri { get; set; } = "";
    [JsonProperty("languageId")] public string LanguageId { get; set; } = "";
    [JsonProperty("version")]    public int Version { get; set; }
    [JsonProperty("text")]       public string Text { get; set; } = "";
}

public class TextDocumentContentChangeEvent
{
    [JsonProperty("text")] public string Text { get; set; } = "";
}
