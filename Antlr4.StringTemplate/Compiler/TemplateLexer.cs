/*
 * [The "BSD license"]
 * Copyright (c) 2011 Terence Parr
 * All rights reserved.
 *
 * Conversion to C#:
 * Copyright (c) 2011 Sam Harwell, Tunnel Vision Laboratories, LLC
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. The name of the author may not be used to endorse or promote products
 *    derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

namespace Antlr4.StringTemplate.Compiler;

using System.Collections.Generic;
using Antlr.Runtime;
using Misc;
using NumberStyles = System.Globalization.NumberStyles;
using StringBuilder = System.Text.StringBuilder;

/** This class represents the tokenizer for templates. It operates in two modes:
 *  inside and outside of expressions. It behaves like an ANTLR TokenSource,
 *  implementing nextToken().  Outside of expressions, we can return these
 *  token types: TEXT, INDENT, LDELIM (start of expr), RCURLY (end of subtemplate),
 *  and NEWLINE. Inside an expression, this lexer returns all the tokens
 *  needed by the STParser. From the parser's point of view, it can treat a
 *  template as a simple stream of elements.
 *
 *  This class defines the token types and communicates these values to STParser.g
 *  via TemplateLexer.tokens file (which must remain consistent).
 */
public class TemplateLexer : ITokenSource
{
    private const char EOF = char.MaxValue;            // EOF char
    public const int EOF_TYPE = CharStreamConstants.EndOfFile;  // EOF token type

    private static readonly IToken SkipToken = new TemplateToken(-1, "<skip>");

    // must follow TemplateLexer.tokens file that STParser.g loads
    private const int RBRACK = 17;
    private const int LBRACK = 16;
    private const int ELSE = 5;
    private const int ELLIPSIS = 11;
    private const int LCURLY = 20;
    private const int BANG = 10;
    private const int EQUALS = 12;
    private const int TEXT = 22;
    private const int ID = 25;
    internal const int SEMI = 9;
    private const int LPAREN = 14;
    private const int IF = 4;
    private const int ELSEIF = 6;
    private const int COLON = 13;
    private const int RPAREN = 15;
    private const int COMMA = 18;
    internal const int RCURLY = 21;
    private const int ENDIF = 7;
    private const int RDELIM = 24;
    private const int SUPER = 8;
    private const int DOT = 19;
    internal const int LDELIM = 23;
    private const int STRING = 26;
    private const int PIPE = 28;
    private const int OR = 29;
    private const int AND = 30;
    internal const int INDENT = 31;
    internal const int NEWLINE = 32;
    private const int AT = 33;
    private const int REGION_END = 34;
    private const int TRUE = 35;
    private const int FALSE = 36;
    private const int COMMENT = 37;

    /** What char starts an expression? */
    private readonly char delimiterStartChar;
    private readonly char delimiterStopChar;

    /** This keep track of the mode of the lexer. Are we inside or outside
     *  a Template expression?
     */
    private bool scanningInsideExpr;

    /** To be able to properly track the inside/outside mode, we need to
     *  track how deeply nested we are in some templates. Otherwise, we
     *  know whether a RCURLY and the outermost subtemplate to send this back to
     *  outside mode.
     */
    public int subtemplateDepth; // start out *not* in a {...} subtemplate

    private readonly ErrorManager errMgr;

    private readonly IToken templateToken; // template embedded in a group file? this is the template

    private readonly ICharStream input;
    private char c;        // current character

    /** When we started token, track initial coordinates so we can properly
     *  build token objects.
     */
    private int startCharIndex;
    private int startLine;
    private int startCharPositionInLine;

    /** Our lexer routines might have to emit more than a single token. We
     *  buffer everything through this list.
     */
    private readonly Queue<IToken> tokens = new();

    public TemplateLexer(ErrorManager errMgr, ICharStream input, IToken templateToken = null,
        char delimiterStartChar = '<', char delimiterStopChar = '>') {
        this.errMgr = errMgr;
        this.input = input;
        c = (char)input.LA(1); // prime lookahead
        this.templateToken = templateToken;
        this.delimiterStartChar = delimiterStartChar;
        this.delimiterStopChar = delimiterStopChar;
    }

    public virtual string SourceName => "no idea";

    public string[] TokenNames => TemplateParser.tokenNames;

    public virtual IToken NextToken() => tokens.Count > 0 ? tokens.Dequeue() : NextTokenImpl();

    /** Ensure x is next character on the input stream */
    private void Match(char x) {
        if (c != x) {
            var e = new NoViableAltException(string.Empty, 0, 0, input);
            errMgr.LexerError(input.SourceName, $"expecting '{x}', found '{GetCharString(c)}'", templateToken, e);
        }
        Consume();
    }

    private void Consume() {
        input.Consume();
        c = (char)input.LA(1);
    }

    private void Emit(IToken token) {
        tokens.Enqueue(token);
    }

    private IToken NextTokenImpl() {
        //System.out.println("nextToken: c="+(char)c+"@"+input.Index());
        while (true) {
            // lets us avoid recursion when skipping stuff
            startCharIndex = input.Index;
            startLine = input.Line;
            startCharPositionInLine = input.CharPositionInLine;
            if (c == EOF) {
                return NewToken(EOF_TYPE);
            }
            var t = scanningInsideExpr ? NextTokenInside() : NextTokenOutside();
            if (t != SkipToken) {
                return t;
            }
        }
    }

    private IToken NextTokenOutside() {
        if (input.CharPositionInLine == 0 && (c == ' ' || c == '\t')) {
            while (c == ' ' || c == '\t') {
                Consume(); // scarf indent
            }
            return NewToken(c != EOF ? INDENT : TEXT);
        }
        if (c == delimiterStartChar) {
            Consume();
            switch (c) {
                case '!':
                    return MatchComment();
                case '\\':
                    return MatchEscape(); // <\\> <\uFFFF> <\n> etc...
                default:
                    scanningInsideExpr = true;
                    return NewToken(LDELIM);
            }
        }

        switch (c) {
            case '\r':
                Consume();
                Consume();
                return NewToken(NEWLINE); // \r\n -> \n
            case '\n':
                Consume();
                return NewToken(NEWLINE);
            case '}' when subtemplateDepth > 0:
                scanningInsideExpr = true;
                subtemplateDepth--;
                Consume();
                return NewTokenFromPreviousChar(RCURLY);
            default:
                return MatchText();
        }
    }

    private IToken NextTokenInside() {
        while (true) {
            switch (c) {
                case ' ':
                case '\t':
                case '\n':
                case '\r':
                    Consume();
                    return SkipToken;

                case '.':
                    Consume();
                    if (input.LA(1) == '.' && input.LA(2) == '.') {
                        Consume();
                        Match('.');
                        return NewToken(ELLIPSIS);
                    }
                    return NewToken(DOT);

                case ',':
                    Consume();
                    return NewToken(COMMA);

                case ':':
                    Consume();
                    return NewToken(COLON);

                case ';':
                    Consume();
                    return NewToken(SEMI);

                case '(':
                    Consume();
                    return NewToken(LPAREN);

                case ')':
                    Consume();
                    return NewToken(RPAREN);

                case '[':
                    Consume();
                    return NewToken(LBRACK);

                case ']':
                    Consume();
                    return NewToken(RBRACK);

                case '=':
                    Consume();
                    return NewToken(EQUALS);

                case '!':
                    Consume();
                    return NewToken(BANG);

                case '@':
                    Consume();
                    if (c == 'e' && input.LA(2) == 'n' && input.LA(3) == 'd')
                    {
                        Consume();
                        Consume();
                        Consume();
                        return NewToken(REGION_END);
                    }
                    return NewToken(AT);

                case '"':
                    return MatchString();

                case '&':
                    Consume();
                    Match('&');
                    return NewToken(AND); // &&

                case '|':
                    Consume();
                    Match('|');
                    return NewToken(OR); // ||

                case '{':
                    return MatchSubTemplate();

                default:
                    if (c == delimiterStopChar) {
                        Consume();
                        scanningInsideExpr = false;
                        return NewToken(RDELIM);
                    }

                    if (IsIDStartLetter(c)) {
                        var id = MatchIdentifier();
                        return (id.Text ?? string.Empty) switch {
                            "if" => NewToken(IF),
                            "endif" => NewToken(ENDIF),
                            "else" => NewToken(ELSE),
                            "elseif" => NewToken(ELSEIF),
                            "super" => NewToken(SUPER),
                            "true" => NewToken(TRUE),
                            "false" => NewToken(FALSE),
                            _ => id
                        };
                    }
                    RecognitionException re = new NoViableAltException(string.Empty, 0, 0, input);
                    re.Line = startLine;
                    re.CharPositionInLine = startCharPositionInLine;
                    errMgr.LexerError(input.SourceName, $"invalid character '{GetCharString(c)}'", templateToken, re);
                    if (c == EOF) {
                        return NewToken(EOF_TYPE);
                    }
                    Consume();
                    break;
            }
        }
    }

    private IToken MatchSubTemplate() {
        // look for "{ args ID (',' ID)* '|' ..."
        subtemplateDepth++;
        var m = input.Mark();
        var curlyStartChar = startCharIndex;
        var curlyLine = startLine;
        var curlyPos = startCharPositionInLine;
        var argTokens = new List<IToken>();
        Consume();
        var curly = NewTokenFromPreviousChar(LCURLY);
        ConsumeWhitespace();
        argTokens.Add(MatchIdentifier());
        ConsumeWhitespace();
        while (c == ',') {
            Consume();
            argTokens.Add(NewTokenFromPreviousChar(COMMA));
            ConsumeWhitespace();
            argTokens.Add(MatchIdentifier());
            ConsumeWhitespace();
        }

        ConsumeWhitespace();
        if (c == '|') {
            Consume();
            argTokens.Add(NewTokenFromPreviousChar(PIPE));
            if (IsWS(c)) {
                Consume(); // ignore a single whitespace after |
            }
            //System.out.println("matched args: "+argTokens);
            foreach (var t in argTokens) {
                Emit(t);
            }
            input.Release(m);
            scanningInsideExpr = false;
            startCharIndex = curlyStartChar; // reset state
            startLine = curlyLine;
            startCharPositionInLine = curlyPos;
            return curly;
        }
        input.Rewind(m);
        startCharIndex = curlyStartChar; // reset state
        startLine = curlyLine;
        startCharPositionInLine = curlyPos;
        Consume();
        scanningInsideExpr = false;
        return curly;
    }

    private IToken MatchEscape() {
        startCharIndex = input.Index;
        startCharPositionInLine = input.CharPositionInLine;
        Consume(); // kill \\
        if (c == 'u') {
            return MatchUnicode();
        }
        string text;
        switch (c) {
            case '\\':
                ConsumeLineBreak();
                return SkipToken;
            case 'n':
                text = "\n";
                break;
            case 't':
                text = "\t";
                break;
            case ' ':
                text = " ";
                break;
            default:
                var e = new NoViableAltException(string.Empty, 0, 0, input);
                errMgr.LexerError(input.SourceName, $"invalid escaped char: '{GetCharString(c)}'", templateToken, e);
                Consume();
                Match(delimiterStopChar);
                return SkipToken;
        }
        Consume();
        var t = NewToken(TEXT, text, input.CharPositionInLine - 2);
        Match(delimiterStopChar);
        return t;
    }

    private IToken MatchUnicode() {
        Consume();
        var chars = new char[4];
        if (!IsUnicodeLetter(c)) {
            var e = new NoViableAltException(string.Empty, 0, 0, input);
            errMgr.LexerError(input.SourceName, $"invalid unicode char: '{GetCharString(c)}'", templateToken, e);
        }
        chars[0] = c;
        Consume();
        if (!IsUnicodeLetter(c)) {
            var e = new NoViableAltException(string.Empty, 0, 0, input);
            errMgr.LexerError(input.SourceName, $"invalid unicode char: '{GetCharString(c)}'", templateToken, e);
        }
        chars[1] = c;
        Consume();
        if (!IsUnicodeLetter(c)) {
            var e = new NoViableAltException(string.Empty, 0, 0, input);
            errMgr.LexerError(input.SourceName, $"invalid unicode char: '{GetCharString(c)}'", templateToken, e);
        }
        chars[2] = c;
        Consume();
        if (!IsUnicodeLetter(c)) {
            var e = new NoViableAltException(string.Empty, 0, 0, input);
            errMgr.LexerError(input.SourceName, $"invalid unicode char: '{GetCharString(c)}'", templateToken, e);
        }
        chars[3] = c;
        // ESCAPE kills >
        var uc = (char)int.Parse(new string(chars), NumberStyles.HexNumber);
        var t = NewToken(TEXT, uc.ToString(), input.CharPositionInLine - 6);
        Consume();
        Match(delimiterStopChar);
        return t;
    }

    private IToken MatchText() {
        var modifiedText = false;
        var buf = new StringBuilder();
        while (c != EOF && c != delimiterStartChar) {
            if (c is '\r' or '\n') {
                break;
            }
            if (c == '}' && subtemplateDepth > 0) {
                break;
            }
            if (c == '\\') {
                if (input.LA(2) == '\\') {
                    // convert \\ to \
                    Consume();
                    Consume();
                    buf.Append('\\');
                    modifiedText = true;
                    continue;
                }
                if (input.LA(2) == delimiterStartChar || input.LA(2) == '}') {
                    modifiedText = true;
                    // toss out \ char
                    Consume();
                    buf.Append(c);
                    Consume();
                } else {
                    buf.Append(c);
                    Consume();
                }
                continue;
            }
            buf.Append(c);
            Consume();
        }
        return modifiedText ? NewToken(TEXT, buf.ToString()) : NewToken(TEXT);
    }

    /** ID  :   ('a'..'z'|'A'..'Z'|'_'|'/') ('a'..'z'|'A'..'Z'|'0'..'9'|'_'|'/')* ; */
    private IToken MatchIdentifier() {
        // called from subTemplate; so keep resetting position during speculation
        startCharIndex = input.Index;
        startLine = input.Line;
        startCharPositionInLine = input.CharPositionInLine;
        Consume();
        while (IsIDLetter(c)) {
            Consume();
        }
        return NewToken(ID);
    }

    /** STRING : '"' ( '\\' '"' | '\\' ~'"' | ~('\\'|'"') )* '"' ; */
    private IToken MatchString() {
        //{setText(getText().substring(1, getText().length()-1));}
        var sawEscape = false;
        var buf = new StringBuilder();
        buf.Append(c);
        Consume();
        while (c != '"') {
            if (c == '\\') {
                sawEscape = true;
                Consume();
                switch (c) {
                    case 'n':
                        buf.Append('\n');
                        break;

                    case 'r':
                        buf.Append('\r');
                        break;

                    case 't':
                        buf.Append('\t');
                        break;

                    default:
                        buf.Append(c);
                        break;
                }

                Consume();
                continue;
            }
            buf.Append(c);
            Consume();
            if (c == EOF) {
                RecognitionException re = new MismatchedTokenException('"', input);
                re.Line = input.Line;
                re.CharPositionInLine = input.CharPositionInLine;
                errMgr.LexerError(input.SourceName, "EOF in string", templateToken, re);
                break;
            }
        }
        buf.Append(c);
        Consume();
        return sawEscape ? NewToken(STRING, buf.ToString()) : NewToken(STRING);
    }

    private void ConsumeWhitespace() {
        while (c is ' ' or '\t' or '\n' or '\r') {
            Consume();
        }
    }

    private IToken MatchComment() {
        Match('!');
        while (!(c == '!' && input.LA(2) == delimiterStopChar)) {
            if (c == EOF) {
                RecognitionException re = new MismatchedTokenException('!', input);
                re.Line = input.Line;
                re.CharPositionInLine = input.CharPositionInLine;
                var message =
                    $"Non-terminated comment starting at {startLine}:{startCharPositionInLine}: '!{delimiterStopChar}' missing";
                errMgr.LexerError(input.SourceName, message, templateToken, re);
                break;
            }
            Consume();
        }
        Consume();
        Consume(); // grab !>
        return NewToken(COMMENT);
    }

    private void ConsumeLineBreak() {
        Match('\\'); // only kill 2nd \ as MatchEscape() kills first one
        Match(delimiterStopChar);
        while (c is ' ' or '\t') {
            Consume(); // scarf WS after <\\>
        }
        if (c == EOF) {
            var re = new RecognitionException(input) {
                Line = input.Line,
                CharPositionInLine = input.CharPositionInLine
            };
            errMgr.LexerError(input.SourceName, "Missing newline after newline escape <\\\\>",
                templateToken, re);
            return;
        }
        if (c == '\r') {
            Consume();
        }
        Match('\n');
        while (c is ' ' or '\t') {
            Consume(); // scarf any indent
        }
    }

    private static bool IsIDStartLetter(char c) {
        return IsIDLetter(c);
    }

    private static bool IsIDLetter(char c) {
        return c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '/';
    }

    private static bool IsWS(char c) {
        return c is ' ' or '\t' or '\n' or '\r';
    }

    private static bool IsUnicodeLetter(char c) {
        return c is >= 'a' and <= 'f' or >= 'A' and <= 'F' or >= '0' and <= '9';
    }

    private IToken NewToken(int tType) {
        var t = new TemplateToken(input, tType, startCharIndex, input.Index - 1) {
            Line = startLine,
            CharPositionInLine = startCharPositionInLine
        };
        return t;
    }

    private IToken NewTokenFromPreviousChar(int tType) {
        var t = new TemplateToken(input, tType, input.Index - 1, input.Index - 1) {
            Line = input.Line,
            CharPositionInLine = input.CharPositionInLine - 1
        };
        return t;
    }

    private IToken NewToken(int tType, string text, int pos) {
        var t = new TemplateToken(tType, text) {
            StartIndex = startCharIndex,
            StopIndex = input.Index - 1,
            Line = input.Line,
            CharPositionInLine = pos
        };
        return t;
    }

    private IToken NewToken(int tType, string text) {
        var t = new TemplateToken(tType, text) {
            StartIndex = startCharIndex,
            StopIndex = input.Index - 1,
            Line = startLine,
            CharPositionInLine = startCharPositionInLine
        };
        return t;
    }

    private static string GetCharString(char c) {
        return c == EOF ? "<EOF>" : c.ToString();
    }

    /** We build STToken tokens instead of relying on CommonToken so we
     *  can override ToString(). It just converts token types to
     *  token names like 23 to LDELIM.
     */
    public class TemplateToken : CommonToken {

        public TemplateToken(ICharStream input, int type, int start, int stop)
        : base(input, type, TokenChannels.Default, start, stop) {
        }

        public TemplateToken(int type, string text)
        : base(type, text) {
        }

        public override string ToString() {
            var channelStr = string.Empty;
            if (Channel > 0) {
                channelStr = ",channel=" + Channel;
            }
            var txt = Text;
            txt = txt != null ? Utility.ReplaceEscapes(txt) : "<no text>";
            var tokenName = Type == EOF_TYPE ? "EOF" : TemplateParser.tokenNames[Type];
            return $"[@{TokenIndex},{StartIndex}:{StopIndex}='{txt}',<{tokenName}>{channelStr},{Line}:{CharPositionInLine}]";
        }
    }
}
