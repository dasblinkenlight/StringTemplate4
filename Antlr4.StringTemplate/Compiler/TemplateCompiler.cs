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

using System;

namespace Antlr4.StringTemplate.Compiler;

using System.Collections.Generic;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using Misc;

/** A compiler for a single template. */
public partial class TemplateCompiler
{
    public const string SubtemplatePrefix = "_sub";

    public const int InitialCodeSize = 15;

    public static readonly Dictionary<string,RenderOption> supportedOptions = new() {
        ["anchor"] = RenderOption.Anchor,
        ["format"] = RenderOption.Format,
        ["null"] = RenderOption.Null,
        ["separator"] = RenderOption.Separator,
        ["wrap"] = RenderOption.Wrap
    };

    public static readonly int NUM_OPTIONS = supportedOptions.Count;

    public static readonly Dictionary<string, string> defaultOptionValues = new() {
        ["anchor"] = "true",
        ["wrap"] =   "\n"
    };

    public static readonly Dictionary<string, Bytecode> funcs = new () {
        ["first"] = Bytecode.INSTR_FIRST,
        ["last"] = Bytecode.INSTR_LAST,
        ["rest"] = Bytecode.INSTR_REST,
        ["trunc"] = Bytecode.INSTR_TRUNC,
        ["strip"] = Bytecode.INSTR_STRIP,
        ["trim"] = Bytecode.INSTR_TRIM,
        ["length"] = Bytecode.INSTR_LENGTH,
        ["strlen"] = Bytecode.INSTR_STRLEN,
        ["reverse"] = Bytecode.INSTR_REVERSE,
    };

    /** Name subtemplates _sub1, _sub2, ... */
    public static int subtemplateCount;

    public TemplateCompiler(ITemplateGroup group) {
        Group = group as TemplateGroup ?? throw new ArgumentException(nameof(group));
    }

    public TemplateGroup Group { get; }

    public ErrorManager ErrorManager => Group.ErrorManager;

    private char DelimiterStartChar => Group.DelimiterStartChar;

    private char DelimiterStopChar => Group.DelimiterStopChar;

    public CompiledTemplate Compile(string template) {
        var code = Compile(null, null, null, template, null);
        code.HasFormalArgs = false;
        return code;
    }

    /** Compile full template with unknown formal args. */
    public CompiledTemplate Compile(string name, string template) {
        var code = Compile(null, name, null, template, null);
        code.HasFormalArgs = false;
        return code;
    }

    /** Compile full template with respect to a list of formal args. */
    public CompiledTemplate Compile(string srcName, string name, List<FormalArgument> args, string template, IToken templateToken) {
        var @is = new ANTLRStringStream(template, srcName) {
            name = srcName ?? name
        };
        TemplateLexer lexer;
        lexer = templateToken is { Type: GroupParser.BIGSTRING_NO_NL } ?
            new TemplateLexerNoNewlines(ErrorManager, @is, templateToken, DelimiterStartChar, DelimiterStopChar) :
            new TemplateLexer(ErrorManager, @is, templateToken, DelimiterStartChar, DelimiterStopChar);
        var tokens = new CommonTokenStream(lexer);
        var p = new TemplateParser(tokens, ErrorManager, templateToken);
        IAstRuleReturnScope<CommonTree> r;
        try {
            r = p.templateAndEOF();
        } catch (RecognitionException re) {
            ReportMessageAndThrowTemplateException(tokens, templateToken, p, re);
            return null;
        }

        if (p.NumberOfSyntaxErrors > 0 || r.Tree == null) {
            var impl = new CompiledTemplate();
            impl.DefineFormalArguments(args);
            return impl;
        }

        //System.out.println(((CommonTree)r.getTree()).toStringTree());
        var nodes = new CommonTreeNodeStream(r.Tree) {
            TokenStream = tokens
        };
        var gen = new CodeGenerator(nodes, this, name, template, templateToken);

        CompiledTemplate impl2 = null;
        try {
            impl2 = gen.template(name, args);
            impl2.NativeGroup = Group;
            impl2.Template = template;
            impl2.Ast = r.Tree;
            impl2.Ast.SetUnknownTokenBoundaries();
            impl2.Tokens = tokens;
        } catch (RecognitionException re) {
            ErrorManager.InternalError(null, "bad tree structure", re);
        }

        return impl2;
    }

    public static CompiledTemplate DefineBlankRegion(CompiledTemplate outermostImpl, IToken nameToken) {
        if (outermostImpl == null) {
            throw new ArgumentNullException(nameof(outermostImpl));
        }
        if (nameToken == null) {
            throw new ArgumentNullException(nameof(nameToken));
        }
        var outermostTemplateName = outermostImpl.Name;
        var mangled = TemplateGroup.GetMangledRegionName(outermostTemplateName, nameToken.Text);
        var blank = new CompiledTemplate {
            IsRegion = true,
            TemplateDefStartToken = nameToken,
            RegionDefType = CompiledTemplate.RegionType.Implicit,
            Name = mangled
        };
        outermostImpl.AddImplicitlyDefinedTemplate(blank);
        return blank;
    }

    public static string GetNewSubtemplateName() {
        subtemplateCount++;
        return SubtemplatePrefix + subtemplateCount;
    }

    private void ReportMessageAndThrowTemplateException(ITokenStream tokens, IToken templateToken,
        Parser parser, RecognitionException re) {
        if (re.Token.Type == TemplateLexer.EOF_TYPE) {
            var msg = "premature EOF";
            ErrorManager.CompiletimeError(ErrorType.SYNTAX_ERROR, templateToken, re.Token, msg);
        } else if (re is NoViableAltException) {
            var msg = "'" + re.Token.Text + "' came as a complete surprise to me";
            ErrorManager.CompiletimeError(ErrorType.SYNTAX_ERROR, templateToken, re.Token, msg);
        } else if (tokens.Index == 0) {
            // couldn't parse anything
            var msg = $"this doesn't look like a template: \"{tokens}\"";
            ErrorManager.CompiletimeError(ErrorType.SYNTAX_ERROR, templateToken, re.Token, msg);
        } else if (tokens.LA(1) == TemplateLexer.LDELIM) {
            // couldn't parse expr
            var msg = "doesn't look like an expression";
            ErrorManager.CompiletimeError(ErrorType.SYNTAX_ERROR, templateToken, re.Token, msg);
        } else {
            var msg = parser.GetErrorMessage(re, parser.TokenNames);
            ErrorManager.CompiletimeError(ErrorType.SYNTAX_ERROR, templateToken, re.Token, msg);
        }

        // we have reported the error, so just blast out
        throw new TemplateException();
    }
}
