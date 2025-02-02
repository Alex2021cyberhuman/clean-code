﻿using Markdown.Contracts;
using Markdown.HtmlParsers;
using Markdown.States;
using Markdown.Tokens;

namespace Markdown;

public class Md
{
    private readonly ITokenParser documentParser;
    private readonly ITracer? tracer;

    public Md(ITokenParser documentParser, ITracer? tracer = null)
    {
        this.documentParser = documentParser;
        this.tracer = tracer;
    }

    public static Md Html(ITracer? tracer)
    {
        var parsers = new Dictionary<TokenType, ITokenParser>
        {
            { TokenType.Italic, new HtmlItalicParser() },
            { TokenType.Plain, new HtmlTextParser() }
        };
        parsers.Add(TokenType.Bold, new HtmlBoldTextParser(parsers));
        parsers.Add(TokenType.UnorderedListItem, new HtmlUnorderedListItemParser(parsers));
        parsers.Add(TokenType.UnorderedList, new HtmlUnorderedListParser(parsers));
        parsers.Add(TokenType.Header, new HtmlHeaderParser(parsers));
        parsers.Add(TokenType.Paragraph, new HtmlParagraphParser(parsers));
        var documentParser = new DocumentParser(parsers);
        return new(documentParser, tracer);
    }

    public string Render(string markdown)
    {
        var transitions = new List<Transition>
        {
            new ReadItalicTextErrorPossibleBoldTextCausedTransition(),
            new ReadItalicTextErrorAlfaNumericTransition(),
            new ReadItalicTextErrorInSeparateWordsTransition(),
            new ReadItalicTextErrorEndCausedTransition(),
            new ReadBoldTextErrorEmptyCausedTransition(),
            new ReadBoldTextErrorAlfaNumericTransition(),
            new ReadBoldTextErrorInSeparateWordsTransition(),
            new ReadBoldTextErrorEndCausedTransition(),
            new EndReadItalicTextTransition(),
            new EndReadPlainTextTransition(),
            new EndReadBoldTextTransition(),
            new EndReadUnorderedListItemTransition(),
            new EndReadUnorderedListTransition(),
            new EndReadHeaderTransition(),
            new EndReadParagraphTransition(),
            new EndReadDocumentTransition(),
            new ReadUnorderedListTransition(),
            new ReadUnorderedListItemTransition(),
            new ReadHeaderTransition(),
            new ReadParagraphTransition(),
            new ReadBoldTextTransition(),
            new ReadItalicTextTransition(),
            new ReadPlainTextTransition(),
            new ReadDocumentTransition()
        };
        var state = new State(markdown);

        while (state.Process != ProcessState.EndReadDocument)
        {
            tracer?.TraceState(state);
            DoTransition(transitions, state);
        }

        tracer?.TraceState(state);

        var html = documentParser.Parse(state.Document);

        return html;
    }

    private void DoTransition(IEnumerable<Transition> transitions, State state)
    {
        var transition =
            transitions.FirstOrDefault(x => state.IgnoredTransitions.All(t => t.transition != x) && x.When(state));
        if (transition is null)
            throw new ApplicationException(
                $"Cannot parse markdown, transition not found for state {state}");
        tracer?.TraceTransition(transition);
        transition.Do(state);
    }
}