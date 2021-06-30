//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.9.2
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from Arithmetic.g4 by ANTLR 4.9.2

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using DFA = Antlr4.Runtime.Dfa.DFA;

[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.9.2")]
[System.CLSCompliant(false)]
public partial class ArithmeticParser : Parser {
	protected static DFA[] decisionToDFA;
	protected static PredictionContextCache sharedContextCache = new PredictionContextCache();
	public const int
		VARIABLE=1, SCIENTIFIC_NUMBER=2, LPAREN=3, RPAREN=4, PLUS=5, MINUS=6, 
		TIMES=7, DIV=8, GT=9, LT=10, EQ=11, POINT=12, POW=13, SEMI=14, WS=15;
	public const int
		RULE_file_ = 0, RULE_expression = 1, RULE_atom = 2, RULE_scientific = 3, 
		RULE_variable = 4;
	public static readonly string[] ruleNames = {
		"file_", "expression", "atom", "scientific", "variable"
	};

	private static readonly string[] _LiteralNames = {
		null, null, null, "'('", "')'", "'+'", "'-'", "'*'", "'/'", "'>'", "'<'", 
		"'='", "'.'", "'^'", "';'"
	};
	private static readonly string[] _SymbolicNames = {
		null, "VARIABLE", "SCIENTIFIC_NUMBER", "LPAREN", "RPAREN", "PLUS", "MINUS", 
		"TIMES", "DIV", "GT", "LT", "EQ", "POINT", "POW", "SEMI", "WS"
	};
	public static readonly IVocabulary DefaultVocabulary = new Vocabulary(_LiteralNames, _SymbolicNames);

	[NotNull]
	public override IVocabulary Vocabulary
	{
		get
		{
			return DefaultVocabulary;
		}
	}

	public override string GrammarFileName { get { return "Arithmetic.g4"; } }

	public override string[] RuleNames { get { return ruleNames; } }

	public override string SerializedAtn { get { return new string(_serializedATN); } }

	static ArithmeticParser() {
		decisionToDFA = new DFA[_ATN.NumberOfDecisions];
		for (int i = 0; i < _ATN.NumberOfDecisions; i++) {
			decisionToDFA[i] = new DFA(_ATN.GetDecisionState(i), i);
		}
	}

		public ArithmeticParser(ITokenStream input) : this(input, Console.Out, Console.Error) { }

		public ArithmeticParser(ITokenStream input, TextWriter output, TextWriter errorOutput)
		: base(input, output, errorOutput)
	{
		Interpreter = new ParserATNSimulator(this, _ATN, decisionToDFA, sharedContextCache);
	}

	public partial class File_Context : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public ExpressionContext[] expression() {
			return GetRuleContexts<ExpressionContext>();
		}
		[System.Diagnostics.DebuggerNonUserCode] public ExpressionContext expression(int i) {
			return GetRuleContext<ExpressionContext>(i);
		}
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode Eof() { return GetToken(ArithmeticParser.Eof, 0); }
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode[] SEMI() { return GetTokens(ArithmeticParser.SEMI); }
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode SEMI(int i) {
			return GetToken(ArithmeticParser.SEMI, i);
		}
		public File_Context(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_file_; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			IArithmeticListener typedListener = listener as IArithmeticListener;
			if (typedListener != null) typedListener.EnterFile_(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			IArithmeticListener typedListener = listener as IArithmeticListener;
			if (typedListener != null) typedListener.ExitFile_(this);
		}
	}

	[RuleVersion(0)]
	public File_Context file_() {
		File_Context _localctx = new File_Context(Context, State);
		EnterRule(_localctx, 0, RULE_file_);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 10;
			expression(0);
			State = 15;
			ErrorHandler.Sync(this);
			_la = TokenStream.LA(1);
			while (_la==SEMI) {
				{
				{
				State = 11;
				Match(SEMI);
				State = 12;
				expression(0);
				}
				}
				State = 17;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
			}
			State = 18;
			Match(Eof);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class ExpressionContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode LPAREN() { return GetToken(ArithmeticParser.LPAREN, 0); }
		[System.Diagnostics.DebuggerNonUserCode] public ExpressionContext[] expression() {
			return GetRuleContexts<ExpressionContext>();
		}
		[System.Diagnostics.DebuggerNonUserCode] public ExpressionContext expression(int i) {
			return GetRuleContext<ExpressionContext>(i);
		}
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode RPAREN() { return GetToken(ArithmeticParser.RPAREN, 0); }
		[System.Diagnostics.DebuggerNonUserCode] public AtomContext atom() {
			return GetRuleContext<AtomContext>(0);
		}
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode[] PLUS() { return GetTokens(ArithmeticParser.PLUS); }
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode PLUS(int i) {
			return GetToken(ArithmeticParser.PLUS, i);
		}
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode[] MINUS() { return GetTokens(ArithmeticParser.MINUS); }
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode MINUS(int i) {
			return GetToken(ArithmeticParser.MINUS, i);
		}
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode POW() { return GetToken(ArithmeticParser.POW, 0); }
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode TIMES() { return GetToken(ArithmeticParser.TIMES, 0); }
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode DIV() { return GetToken(ArithmeticParser.DIV, 0); }
		public ExpressionContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_expression; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			IArithmeticListener typedListener = listener as IArithmeticListener;
			if (typedListener != null) typedListener.EnterExpression(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			IArithmeticListener typedListener = listener as IArithmeticListener;
			if (typedListener != null) typedListener.ExitExpression(this);
		}
	}

	[RuleVersion(0)]
	public ExpressionContext expression() {
		return expression(0);
	}

	private ExpressionContext expression(int _p) {
		ParserRuleContext _parentctx = Context;
		int _parentState = State;
		ExpressionContext _localctx = new ExpressionContext(Context, _parentState);
		ExpressionContext _prevctx = _localctx;
		int _startState = 2;
		EnterRecursionRule(_localctx, 2, RULE_expression, _p);
		int _la;
		try {
			int _alt;
			EnterOuterAlt(_localctx, 1);
			{
			State = 32;
			ErrorHandler.Sync(this);
			switch (TokenStream.LA(1)) {
			case LPAREN:
				{
				State = 21;
				Match(LPAREN);
				State = 22;
				expression(0);
				State = 23;
				Match(RPAREN);
				}
				break;
			case VARIABLE:
			case SCIENTIFIC_NUMBER:
			case PLUS:
			case MINUS:
				{
				State = 28;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
				while (_la==PLUS || _la==MINUS) {
					{
					{
					State = 25;
					_la = TokenStream.LA(1);
					if ( !(_la==PLUS || _la==MINUS) ) {
					ErrorHandler.RecoverInline(this);
					}
					else {
						ErrorHandler.ReportMatch(this);
					    Consume();
					}
					}
					}
					State = 30;
					ErrorHandler.Sync(this);
					_la = TokenStream.LA(1);
				}
				State = 31;
				atom();
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
			Context.Stop = TokenStream.LT(-1);
			State = 45;
			ErrorHandler.Sync(this);
			_alt = Interpreter.AdaptivePredict(TokenStream,4,Context);
			while ( _alt!=2 && _alt!=global::Antlr4.Runtime.Atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					if ( ParseListeners!=null )
						TriggerExitRuleEvent();
					_prevctx = _localctx;
					{
					State = 43;
					ErrorHandler.Sync(this);
					switch ( Interpreter.AdaptivePredict(TokenStream,3,Context) ) {
					case 1:
						{
						_localctx = new ExpressionContext(_parentctx, _parentState);
						PushNewRecursionContext(_localctx, _startState, RULE_expression);
						State = 34;
						if (!(Precpred(Context, 5))) throw new FailedPredicateException(this, "Precpred(Context, 5)");
						State = 35;
						Match(POW);
						State = 36;
						expression(6);
						}
						break;
					case 2:
						{
						_localctx = new ExpressionContext(_parentctx, _parentState);
						PushNewRecursionContext(_localctx, _startState, RULE_expression);
						State = 37;
						if (!(Precpred(Context, 4))) throw new FailedPredicateException(this, "Precpred(Context, 4)");
						State = 38;
						_la = TokenStream.LA(1);
						if ( !(_la==TIMES || _la==DIV) ) {
						ErrorHandler.RecoverInline(this);
						}
						else {
							ErrorHandler.ReportMatch(this);
						    Consume();
						}
						State = 39;
						expression(5);
						}
						break;
					case 3:
						{
						_localctx = new ExpressionContext(_parentctx, _parentState);
						PushNewRecursionContext(_localctx, _startState, RULE_expression);
						State = 40;
						if (!(Precpred(Context, 3))) throw new FailedPredicateException(this, "Precpred(Context, 3)");
						State = 41;
						_la = TokenStream.LA(1);
						if ( !(_la==PLUS || _la==MINUS) ) {
						ErrorHandler.RecoverInline(this);
						}
						else {
							ErrorHandler.ReportMatch(this);
						    Consume();
						}
						State = 42;
						expression(4);
						}
						break;
					}
					} 
				}
				State = 47;
				ErrorHandler.Sync(this);
				_alt = Interpreter.AdaptivePredict(TokenStream,4,Context);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			UnrollRecursionContexts(_parentctx);
		}
		return _localctx;
	}

	public partial class AtomContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public ScientificContext scientific() {
			return GetRuleContext<ScientificContext>(0);
		}
		[System.Diagnostics.DebuggerNonUserCode] public VariableContext variable() {
			return GetRuleContext<VariableContext>(0);
		}
		public AtomContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_atom; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			IArithmeticListener typedListener = listener as IArithmeticListener;
			if (typedListener != null) typedListener.EnterAtom(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			IArithmeticListener typedListener = listener as IArithmeticListener;
			if (typedListener != null) typedListener.ExitAtom(this);
		}
	}

	[RuleVersion(0)]
	public AtomContext atom() {
		AtomContext _localctx = new AtomContext(Context, State);
		EnterRule(_localctx, 4, RULE_atom);
		try {
			State = 50;
			ErrorHandler.Sync(this);
			switch (TokenStream.LA(1)) {
			case SCIENTIFIC_NUMBER:
				EnterOuterAlt(_localctx, 1);
				{
				State = 48;
				scientific();
				}
				break;
			case VARIABLE:
				EnterOuterAlt(_localctx, 2);
				{
				State = 49;
				variable();
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class ScientificContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode SCIENTIFIC_NUMBER() { return GetToken(ArithmeticParser.SCIENTIFIC_NUMBER, 0); }
		public ScientificContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_scientific; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			IArithmeticListener typedListener = listener as IArithmeticListener;
			if (typedListener != null) typedListener.EnterScientific(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			IArithmeticListener typedListener = listener as IArithmeticListener;
			if (typedListener != null) typedListener.ExitScientific(this);
		}
	}

	[RuleVersion(0)]
	public ScientificContext scientific() {
		ScientificContext _localctx = new ScientificContext(Context, State);
		EnterRule(_localctx, 6, RULE_scientific);
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 52;
			Match(SCIENTIFIC_NUMBER);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class VariableContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode VARIABLE() { return GetToken(ArithmeticParser.VARIABLE, 0); }
		public VariableContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_variable; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			IArithmeticListener typedListener = listener as IArithmeticListener;
			if (typedListener != null) typedListener.EnterVariable(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			IArithmeticListener typedListener = listener as IArithmeticListener;
			if (typedListener != null) typedListener.ExitVariable(this);
		}
	}

	[RuleVersion(0)]
	public VariableContext variable() {
		VariableContext _localctx = new VariableContext(Context, State);
		EnterRule(_localctx, 8, RULE_variable);
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 54;
			Match(VARIABLE);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public override bool Sempred(RuleContext _localctx, int ruleIndex, int predIndex) {
		switch (ruleIndex) {
		case 1: return expression_sempred((ExpressionContext)_localctx, predIndex);
		}
		return true;
	}
	private bool expression_sempred(ExpressionContext _localctx, int predIndex) {
		switch (predIndex) {
		case 0: return Precpred(Context, 5);
		case 1: return Precpred(Context, 4);
		case 2: return Precpred(Context, 3);
		}
		return true;
	}

	private static char[] _serializedATN = {
		'\x3', '\x608B', '\xA72A', '\x8133', '\xB9ED', '\x417C', '\x3BE7', '\x7786', 
		'\x5964', '\x3', '\x11', ';', '\x4', '\x2', '\t', '\x2', '\x4', '\x3', 
		'\t', '\x3', '\x4', '\x4', '\t', '\x4', '\x4', '\x5', '\t', '\x5', '\x4', 
		'\x6', '\t', '\x6', '\x3', '\x2', '\x3', '\x2', '\x3', '\x2', '\a', '\x2', 
		'\x10', '\n', '\x2', '\f', '\x2', '\xE', '\x2', '\x13', '\v', '\x2', '\x3', 
		'\x2', '\x3', '\x2', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', 
		'\x3', '\x3', '\x3', '\x3', '\x3', '\a', '\x3', '\x1D', '\n', '\x3', '\f', 
		'\x3', '\xE', '\x3', ' ', '\v', '\x3', '\x3', '\x3', '\x5', '\x3', '#', 
		'\n', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', 
		'\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\x3', '\a', '\x3', 
		'.', '\n', '\x3', '\f', '\x3', '\xE', '\x3', '\x31', '\v', '\x3', '\x3', 
		'\x4', '\x3', '\x4', '\x5', '\x4', '\x35', '\n', '\x4', '\x3', '\x5', 
		'\x3', '\x5', '\x3', '\x6', '\x3', '\x6', '\x3', '\x6', '\x2', '\x3', 
		'\x4', '\a', '\x2', '\x4', '\x6', '\b', '\n', '\x2', '\x4', '\x3', '\x2', 
		'\a', '\b', '\x3', '\x2', '\t', '\n', '\x2', '<', '\x2', '\f', '\x3', 
		'\x2', '\x2', '\x2', '\x4', '\"', '\x3', '\x2', '\x2', '\x2', '\x6', '\x34', 
		'\x3', '\x2', '\x2', '\x2', '\b', '\x36', '\x3', '\x2', '\x2', '\x2', 
		'\n', '\x38', '\x3', '\x2', '\x2', '\x2', '\f', '\x11', '\x5', '\x4', 
		'\x3', '\x2', '\r', '\xE', '\a', '\x10', '\x2', '\x2', '\xE', '\x10', 
		'\x5', '\x4', '\x3', '\x2', '\xF', '\r', '\x3', '\x2', '\x2', '\x2', '\x10', 
		'\x13', '\x3', '\x2', '\x2', '\x2', '\x11', '\xF', '\x3', '\x2', '\x2', 
		'\x2', '\x11', '\x12', '\x3', '\x2', '\x2', '\x2', '\x12', '\x14', '\x3', 
		'\x2', '\x2', '\x2', '\x13', '\x11', '\x3', '\x2', '\x2', '\x2', '\x14', 
		'\x15', '\a', '\x2', '\x2', '\x3', '\x15', '\x3', '\x3', '\x2', '\x2', 
		'\x2', '\x16', '\x17', '\b', '\x3', '\x1', '\x2', '\x17', '\x18', '\a', 
		'\x5', '\x2', '\x2', '\x18', '\x19', '\x5', '\x4', '\x3', '\x2', '\x19', 
		'\x1A', '\a', '\x6', '\x2', '\x2', '\x1A', '#', '\x3', '\x2', '\x2', '\x2', 
		'\x1B', '\x1D', '\t', '\x2', '\x2', '\x2', '\x1C', '\x1B', '\x3', '\x2', 
		'\x2', '\x2', '\x1D', ' ', '\x3', '\x2', '\x2', '\x2', '\x1E', '\x1C', 
		'\x3', '\x2', '\x2', '\x2', '\x1E', '\x1F', '\x3', '\x2', '\x2', '\x2', 
		'\x1F', '!', '\x3', '\x2', '\x2', '\x2', ' ', '\x1E', '\x3', '\x2', '\x2', 
		'\x2', '!', '#', '\x5', '\x6', '\x4', '\x2', '\"', '\x16', '\x3', '\x2', 
		'\x2', '\x2', '\"', '\x1E', '\x3', '\x2', '\x2', '\x2', '#', '/', '\x3', 
		'\x2', '\x2', '\x2', '$', '%', '\f', '\a', '\x2', '\x2', '%', '&', '\a', 
		'\xF', '\x2', '\x2', '&', '.', '\x5', '\x4', '\x3', '\b', '\'', '(', '\f', 
		'\x6', '\x2', '\x2', '(', ')', '\t', '\x3', '\x2', '\x2', ')', '.', '\x5', 
		'\x4', '\x3', '\a', '*', '+', '\f', '\x5', '\x2', '\x2', '+', ',', '\t', 
		'\x2', '\x2', '\x2', ',', '.', '\x5', '\x4', '\x3', '\x6', '-', '$', '\x3', 
		'\x2', '\x2', '\x2', '-', '\'', '\x3', '\x2', '\x2', '\x2', '-', '*', 
		'\x3', '\x2', '\x2', '\x2', '.', '\x31', '\x3', '\x2', '\x2', '\x2', '/', 
		'-', '\x3', '\x2', '\x2', '\x2', '/', '\x30', '\x3', '\x2', '\x2', '\x2', 
		'\x30', '\x5', '\x3', '\x2', '\x2', '\x2', '\x31', '/', '\x3', '\x2', 
		'\x2', '\x2', '\x32', '\x35', '\x5', '\b', '\x5', '\x2', '\x33', '\x35', 
		'\x5', '\n', '\x6', '\x2', '\x34', '\x32', '\x3', '\x2', '\x2', '\x2', 
		'\x34', '\x33', '\x3', '\x2', '\x2', '\x2', '\x35', '\a', '\x3', '\x2', 
		'\x2', '\x2', '\x36', '\x37', '\a', '\x4', '\x2', '\x2', '\x37', '\t', 
		'\x3', '\x2', '\x2', '\x2', '\x38', '\x39', '\a', '\x3', '\x2', '\x2', 
		'\x39', '\v', '\x3', '\x2', '\x2', '\x2', '\b', '\x11', '\x1E', '\"', 
		'-', '/', '\x34',
	};

	public static readonly ATN _ATN =
		new ATNDeserializer().Deserialize(_serializedATN);


}
