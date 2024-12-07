using System;
using System.Text;
using Antlr4.StringTemplate.Misc;

namespace Antlr4.StringTemplate;

public class TemplateGroupBuilder {

    private bool _enableCache;
    private char _delimiterStartChar = '<';
    private char _delimiterStopChar = '>';
    private Encoding _encoding = Encoding.UTF8;
    private ITemplateErrorListener _errorListener;
    private ErrorManager _errorManager;
    private Func<TemplateGroup> _make;

    internal static TemplateGroupBuilder ForGroup() {
        var res = new TemplateGroupBuilder();
        res._make = res.makeGroup;
        return res;
    }

    internal static TemplateGroupBuilder ForGroupDirectory(string directory) {
        var res = new TemplateGroupBuilder();
        res._make = () => res.makeGroupDirectory(directory);
        return res;
    }

    internal static TemplateGroupBuilder ForRawGroupDirectory(string directory) {
        var res = new TemplateGroupBuilder();
        res._make = () => res.makeRawGroupDirectory(directory);
        return res;
    }

    internal static TemplateGroupBuilder ForGroupFile(string fullyQualifiedName) {
        var res = new TemplateGroupBuilder();
        res._make = () => res.MakeGroupFile(fullyQualifiedName);
        return res;
    }

    internal static TemplateGroupBuilder ForString(string name, string text) {
        var res = new TemplateGroupBuilder();
        res._make = () => res.MakeGroupString(name, text);
        return res;
    }

    private TemplateGroupBuilder() {
    }

    public TemplateGroupBuilder WithErrorListener(ITemplateErrorListener errorListener) {
        if (_errorListener != null) {
            throw new InvalidOperationException($"{nameof(errorListener)} cannot be specified more than once.");
        }
        if (_errorManager != null) {
            throw new InvalidOperationException($"{nameof(errorListener)} cannot be specified together with error manager.");
        }
        _errorListener = errorListener;
        return this;
    }

    public TemplateGroupBuilder WithErrorManager(ErrorManager errorManager) {
        if (_errorManager != null) {
            throw new InvalidOperationException($"{nameof(errorManager)} cannot be specified more than once.");
        }
        if (_errorListener != null) {
            throw new InvalidOperationException($"{nameof(errorManager)} cannot be specified together with error listener.");
        }
        _errorManager = errorManager;
        return this;
    }

    public TemplateGroupBuilder WithCaching() {
        _enableCache = true;
        return this;
    }

    public TemplateGroupBuilder WithDelimiters(char delimiterStart, char delimiterStop) {
        _delimiterStartChar = delimiterStart;
        _delimiterStopChar = delimiterStop;
        return this;
    }

    public TemplateGroupBuilder WithEncoding(Encoding encoding) {
        _encoding = encoding;
        return this;
    }

    public ITemplateGroup Build() {
        var res = _make();
        if (_errorListener != null) {
            res.Listener = _errorListener;
        }
        if (_errorManager != null) {
            res.ErrorManager = _errorManager;
        }
        if (_enableCache) {
            res.EnableCache = true;
        }
        return res;
    }

    private TemplateGroup makeGroup() => new (_delimiterStartChar, _delimiterStopChar);

    private TemplateGroup makeGroupDirectory(string dirName) =>
        new TemplateGroupDirectory(dirName, _encoding, _delimiterStartChar, _delimiterStopChar);

    private TemplateGroup makeRawGroupDirectory(string dirName) =>
        new TemplateRawGroupDirectory(dirName, _encoding, _delimiterStartChar, _delimiterStopChar);

    private TemplateGroup MakeGroupFile(string fullyQualifiedFileName) =>
        new TemplateGroupFile(fullyQualifiedFileName, _encoding, _delimiterStartChar, _delimiterStopChar);

    private TemplateGroup MakeGroupString(string text, string name) =>
        new TemplateGroupString(name, text, _delimiterStartChar, _delimiterStopChar);
}
