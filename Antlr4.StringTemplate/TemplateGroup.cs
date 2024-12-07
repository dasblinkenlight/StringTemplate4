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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Antlr.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ArgumentException = System.ArgumentException;
using ArgumentNullException = System.ArgumentNullException;
using BinaryReader = System.IO.BinaryReader;
using BinaryWriter = System.IO.BinaryWriter;
using DateTime = System.DateTime;
using DateTimeKind = System.DateTimeKind;
using Directory = System.IO.Directory;
using Environment = System.Environment;
using Exception = System.Exception;
using File = System.IO.File;
using IDictionary = System.Collections.IDictionary;
using IOException = System.IO.IOException;
using MemoryStream = System.IO.MemoryStream;
using NotImplementedException = System.NotImplementedException;
using NotSupportedException = System.NotSupportedException;
using Path = System.IO.Path;
using SeekOrigin = System.IO.SeekOrigin;
using Stream = System.IO.Stream;
using StreamReader = System.IO.StreamReader;
using StringBuilder = System.Text.StringBuilder;
using Type = System.Type;
using Uri = System.Uri;
using UriFormatException = System.UriFormatException;

namespace Antlr4.StringTemplate;

using Compiler;
using Extensions;
using Misc;
using TemplateToken = Compiler.TemplateLexer.TemplateToken;

/** A directory or directory tree of .st template files and/or group files.
 *  Individual template files contain formal template definitions. In a sense,
 *  it's like a single group file broken into multiple files, one for each template.
 *  Template v3 had just the pure template inside, not the template name and header.
 *  Name inside must match filename (minus suffix).
 */
public class TemplateGroup : ITemplateGroup {

    protected const string GroupFileExtension = ".stg";
    protected const string TemplateFileExtension = ".st";

    /** When we use key as a value in a dictionary, this is how we signify. */
    public const string DictionaryKey = "key";
    public const string DefaultKey = "default";

    protected ILogger<TemplateGroup> _logger = NullLogger<TemplateGroup>.Instance;

    /** Load files using what encoding? */
    private Encoding _encoding = Encoding.UTF8;

    /** Every group can import templates/dictionaries from other groups.
     *  The list must be synchronized (see ImportTemplates).
     */
    private readonly List<TemplateGroup> _imports = [];

    private readonly List<TemplateGroup> _importsToClearOnUnload = [];

    public char DelimiterStartChar { get; internal set; } // Use <expr> by default

    public char DelimiterStopChar { get; internal set; }

    /** Maps template name to StringTemplate object. synchronized. */
    private readonly Dictionary<string,CompiledTemplate> templates = new();

    /** Maps dict names to HashMap objects.  This is the list of dictionaries
     *  defined by the user like typeInitMap ::= ["int":"0"]
     */
    private readonly Dictionary<string,IDictionary<string,object>> dictionaries = new();

    /** A dictionary that allows people to register a renderer for
     *  a particular kind of object for any template evaluated relative to this
     *  group.  For example, a date should be formatted differently depending
     *  on the culture.  You can set `Date.class` to an object whose
     *  ToString(Object) method properly formats a Date attribute
     *  according to culture.  Or you can have a different renderer object
     *  for each culture.
     *
     *  Order of addition is recorded and matters.  If more than one
     *  renderer works for an object, the first registered has priority.
     *
     *  Renderer associated with type t works for object o if
     *
     * 		t.isAssignableFrom(o.getClass()) // would assignment t = o work?
     *
     *  So it works if o is subclass or implements t.
     *
     *  This structure is synchronized.
     */
    private TypeRegistry<IAttributeRenderer> renderers;

    private TypeRegistry<ITypeProxyFactory> _proxyFactories;

    /** A dictionary that allows people to register a model adaptor for
     *  a particular kind of object (subclass or implementation). Applies
     *  for any template evaluated relative to this group.
     *
     *  Template initializes with model adaptors that know how to pull
     *  properties out of Objects, Maps, and STs.
     */
    private readonly TypeRegistry<IModelAdaptor> adaptors = new() {
        {typeof(object), new ObjectModelAdaptor()},
        {typeof(Template), new TemplateModelAdaptor()},
        {typeof(IDictionary), new MapModelAdaptor()},
        {typeof(Aggregate), new AggregateModelAdaptor()},
    };

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public bool EnableCache { get; set; }

    /** Used to indicate that the template doesn't exist.
     *  Prevents duplicate group file loads and unnecessary file checks.
     */
    private static CompiledTemplate NotFoundTemplate { get; } = new();

    public static ErrorManager DefaultErrorManager { get; } = new();

    /** The error manager for entire group; all compilations and executions.
     *  This gets copied to parsers, walkers, and interpreters.
     */
    private ErrorManager _errorManager = DefaultErrorManager;

    private static TemplateGroup _defaultGroup = new();

    public TemplateGroup(char delimiterStartChar = '<', char delimiterStopChar = '>') {
        DelimiterStartChar = delimiterStartChar;
        DelimiterStopChar = delimiterStopChar;
    }

    public static TemplateGroup DefaultGroup {
        get => _defaultGroup;
        set => _defaultGroup = value ?? throw new ArgumentNullException(nameof(value));
    }

    protected ICollection<CompiledTemplate> CompiledTemplates => templates.Values;

    internal Encoding Encoding {
        get => _encoding;
        set => _encoding = value ?? throw new ArgumentNullException(nameof(value));
    }

    public ErrorManager ErrorManager {
        get => _errorManager;
        set => _errorManager = value ?? throw new ArgumentNullException(nameof(value));
    }

    /** For debugging with STViz. Records where in code an ST was created
     *  and where code added attributes.
     */
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public bool TrackCreationEvents { get; set; }

    /** v3 compatibility; used to iterate across values not keys like v4.
     *  But to convert ANTLR templates, it's too hard to find without
     *  static typing in templates.
     */
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public bool IterateAcrossValues { get; set; }

    private ReadOnlyCollection<TemplateGroup> ImportedGroups => _imports.AsReadOnly();

    /** The primary means of getting an instance of a template from this
     *  group. Names must be absolute, fully-qualified names like a/b
     */
    internal Template GetInstanceOf(string name) {
        if (name == null) {
            return null;
        }
        if (!name.StartsWith("/")) {
            name = "/" + name;
        }
        _logger.LogDebug("{GroupName}.GetInstanceOf({InstanceName})", Name, name);
        var c = LookupTemplate(name);
        return c != null ? CreateStringTemplate(c) : null;
    }

    public ITemplate FindTemplate(string name) => GetInstanceOf(name);

    protected internal Template GetEmbeddedInstanceOf(TemplateFrame frame, string name) {
        var fullyQualifiedName = name;
        if (!name.StartsWith("/")) {
            fullyQualifiedName = frame.Template.impl.Prefix + name;
        }
        _logger.LogDebug("GetEmbeddedInstanceOf({FullyQualifiedName})", fullyQualifiedName);
        var st = GetInstanceOf(fullyQualifiedName);
        if (st == null) {
            ErrorManager.RuntimeError(frame, ErrorType.NO_SUCH_TEMPLATE, fullyQualifiedName);
            return CreateStringTemplateInternally(new CompiledTemplate());
        }
        // this is only called internally. wack any debug ST create events
        if (TrackCreationEvents) {
            // toss it out
            st.DebugState.NewTemplateEvent = null;
        }
        return st;
    }

    /** Create singleton template for use with dictionary values */
    public Template CreateSingleton(IToken templateToken) {
        string template;
        if (templateToken.Type == GroupParser.BIGSTRING || templateToken.Type == GroupParser.BIGSTRING_NO_NL) {
            template = Utility.Strip(templateToken.Text, 2);
        } else {
            template = Utility.Strip(templateToken.Text, 1);
        }
        var impl = Compile(FileName, null, null, template, templateToken);
        var st = CreateStringTemplateInternally(impl);
        st.Group = this;
        st.impl.HasFormalArgs = false;
        st.impl.Name = Template.UnknownName;
        st.impl.DefineImplicitlyDefinedTemplates(this);
        return st;
    }

    /** Is this template defined in this group or from this group below?
     *  Names must be absolute, fully-qualified names like /a/b
     */
    public virtual bool IsDefined(string name) {
        return LookupTemplate(name) != null;
    }

    /** Look up a fully-qualified name */
    internal CompiledTemplate LookupTemplate(string name) {
        if (name[0] != '/') {
            name = "/" + name;
        }
        _logger.LogDebug("{GroupName}.LookupTemplate({TemplateName})", Name, name);
        templates.TryGetValue(name, out var code);
        if (code == NotFoundTemplate) {
            _logger.LogDebug("{TemplateName} previously seen as not found", name);
            return null;
        }
        // try to load from disk and look up again
        code ??= Load(name) ?? LookupImportedTemplate(name);

        if (code == null) {
            _logger.LogDebug("{TemplateName} recorded not found", name);
            templates[name] = NotFoundTemplate;
        } else {
            _logger.LogDebug("{GroupName}.LookupTemplate({TemplateName}) found", Name, name);
        }
        return code;
    }

    /** "Unload" all templates and dictionaries but leave renderers, adaptors,
     *  and import relationships.  This essentially forces next GetInstanceOf
     *  to reload templates.
     */
    public virtual void Unload() {
        lock (this) {
            templates.Clear();
            dictionaries.Clear();
            foreach (var import in _imports) {
                import.Unload();
            }
            foreach (var import in _importsToClearOnUnload) {
                _imports.Remove(import);
            }
            _importsToClearOnUnload.Clear();
        }
    }

    /** Load st from disk if dir or load whole group file if .stg file (then
     *  return just one template). name is fully-qualified.
     */
    protected virtual CompiledTemplate Load(string name) {
        return null;
    }

    /** Force a load if it makes sense for the group */
    public virtual void Load() {
    }

    protected internal CompiledTemplate LookupImportedTemplate(string name) {
        if (_imports == null) {
            return null;
        }
        foreach (var g in _imports) {
            _logger.LogDebug("Checking {GroupName} for imported {TemplateName}", g.Name, name);
            var code = g.LookupTemplate(name);
            if (code != null) {
                _logger.LogDebug("{GroupName}.LookupImportedTemplate({TemplateName}) found", g.Name, name);
                return code;
            }
        }
        _logger.LogDebug("{TemplateName} not found in {GroupName} imports", name, Name);
        return null;
    }

    public CompiledTemplate RawGetTemplate(string name) {
        templates.TryGetValue(name, out var template);
        return template;
    }

    public IDictionary<string, object> RawGetDictionary(string name) {
        return dictionaries.TryGetValue(name, out var dictionary) ? dictionary : null;
    }

    public bool IsDictionary(string name) {
        return RawGetDictionary(name) != null;
    }

    // for testing
    internal void DefineTemplate(string name, string template) {
        if (name[0] != '/') {
            name = "/" + name;
        }
        try {
            DefineTemplate(name, new CommonToken(GroupParser.ID, name), null, template, null);
        } catch (TemplateException ex) {
            _logger.LogError("BUG: Unable to define template {Name}. {Exception}", name, ex);
        }
    }

    // for testing
    internal void DefineTemplate(string name, string template, string[] arguments) {
        if (name[0] != '/') {
            name = "/" + name;
        }
        var a = new List<FormalArgument>();
        foreach (var arg in arguments) {
            a.Add(new FormalArgument(arg));
        }
        DefineTemplate(name, new CommonToken(GroupParser.ID, name), a, template, null);
    }

    private void DefineTemplate(string fullyQualifiedTemplateName,
        IToken nameT,
        List<FormalArgument> args,
        string template,
        IToken templateToken) {
        _logger.LogDebug("DefineTemplate({TemplateName})", fullyQualifiedTemplateName);
        if (fullyQualifiedTemplateName == null) {
            throw new ArgumentNullException(nameof(fullyQualifiedTemplateName));
        }
        if (fullyQualifiedTemplateName.Length == 0) {
            throw new ArgumentException("empty template name", nameof(fullyQualifiedTemplateName));
        }
        if (fullyQualifiedTemplateName.IndexOf('.') >= 0) {
            throw new ArgumentException("cannot have '.' in template names", nameof(fullyQualifiedTemplateName));
        }
        if (fullyQualifiedTemplateName[0] != '/') {
            throw new ArgumentException("Expected a fully qualified template name.", nameof(fullyQualifiedTemplateName));
        }
        template = Utility.TrimOneStartingNewline(template);
        template = Utility.TrimOneTrailingNewline(template);
        // compile, passing in templateName as enclosing name for any embedded regions
        var code = Compile(FileName, fullyQualifiedTemplateName, args, template, templateToken);
        code.Name = fullyQualifiedTemplateName;
        RawDefineTemplate(fullyQualifiedTemplateName, code, nameT);
        code.DefineArgumentDefaultValueTemplates(this);
        code.DefineImplicitlyDefinedTemplates(this); // define any anonymous subtemplates
    }

    /** Make name and alias for target.  Replace any previous def of name */
    public CompiledTemplate DefineTemplateAlias(IToken aliasT, IToken targetT) {
        var alias = aliasT.Text;
        var target = targetT.Text;
        var targetCode = RawGetTemplate("/" + target);
        if (targetCode == null) {
            ErrorManager.CompiletimeError(ErrorType.ALIAS_TARGET_UNDEFINED, null, aliasT, alias, target);
            return null;
        }
        RawDefineTemplate("/" + alias, targetCode, aliasT);
        return targetCode;
    }

    private void DefineRegion(string enclosingTemplateName, IToken regionT, string template, IToken templateToken) {
        var name = regionT.Text;
        template = Utility.TrimOneStartingNewline(template);
        template = Utility.TrimOneTrailingNewline(template);
        var code = Compile(FileName, enclosingTemplateName, null, template, templateToken);
        var mangled = GetMangledRegionName(enclosingTemplateName, name);
        if (LookupTemplate(mangled) == null) {
            ErrorManager.CompiletimeError(ErrorType.NO_SUCH_REGION, null, regionT, enclosingTemplateName, name);
            return;
        }
        code.Name = mangled;
        code.IsRegion = true;
        code.RegionDefType = Template.RegionType.Explicit;
        code.TemplateDefStartToken = regionT;
        RawDefineTemplate(mangled, code, regionT);
        code.DefineArgumentDefaultValueTemplates(this);
        code.DefineImplicitlyDefinedTemplates(this); // define any anonymous subtemplates
    }

    public void DefineTemplateOrRegion(
        string fullyQualifiedTemplateName,
        string regionSurroundingTemplateName,
        IToken templateToken,
        string template,
        IToken nameToken,
        List<FormalArgument> args) {
        if (fullyQualifiedTemplateName[0] != '/') {
            throw new ArgumentException("Expected a fully qualified template name.", nameof(fullyQualifiedTemplateName));
        }
        try {
            if (regionSurroundingTemplateName != null) {
                DefineRegion(regionSurroundingTemplateName, nameToken, template, templateToken);
            } else {
                DefineTemplate(fullyQualifiedTemplateName, nameToken, args, template, templateToken);
            }
        } catch (TemplateException) {
            // after getting syntax error in a template, we emit msg
            // and throw exception to blast all the way out to here.
        }
    }

    public void RawDefineTemplate(string name, CompiledTemplate code, IToken defT) {
        if (templates.TryGetValue(name, out var prev) && prev != null) {
            if (!prev.IsRegion) {
                ErrorManager.CompiletimeError(ErrorType.TEMPLATE_REDEFINITION, null, defT);
                return;
            }
            // ReSharper disable once GrammarMistakeInComment
            /* If this region was previously defined, the following actions should be taken:
             *
             *      Previous type   Current type   Result   Applied     Reason
             *      -------------   ------------   ------   -------     ------
             *      Implicit        Implicit       Success  Previous    A rule may make multiple implicit references to the same region.
             *                                                          Keeping either has the same semantics, so the existing one is
             *                                                          used for slightly less overhead.
             *      Implicit        Explicit       Success  Current     A region with previous implicit references is now being explicitly
             *                                                          defined.
             *      Implicit        Embedded       Success  Current     A region with previous implicit references is now being defined
             *                                                          with an embedded region.
             *      Explicit        Implicit       Success  Previous    An explicitly defined region is now being implicitly referenced.
             *                                                          Make sure to keep the previous explicit definition as the actual
             *                                                          definition.
             *      Explicit        Explicit       Error    Previous    Multiple explicit definitions exist for the same region (template
             *                                                          redefinition error). Give an error and use the previous one.
             *      Explicit        Embedded       Warning  Previous    An explicit region definition already exists for the current
             *                                                          embedded region definition. The explicit definition overrides the
             *                                                          embedded definition and a warning is given since the embedded
             *                                                          definition is hidden.
             *      Embedded        Implicit       Success  Previous    A region with an embedded definition is now being implicitly
             *                                                          referenced. The embedded definition should be used.
             *      Embedded        Explicit       Warning  Current     A region with an embedded definition is now being explicitly
             *                                                          defined. The explicit definition overrides the embedded
             *                                                          definition and a warning is given since the embedded definition
             *                                                          is hidden.
             *      Embedded        Embedded       Error    Previous    Multiple embedded definitions of the same region were given in a
             *                                                          template. Give an error and use the previous one.
             */
            // handle the Explicit/Explicit and Embedded/Embedded error cases
            if (code.RegionDefType != Template.RegionType.Implicit && code.RegionDefType == prev.RegionDefType) {
                ErrorManager.CompiletimeError(
                    code.RegionDefType == Template.RegionType.Embedded
                        ? ErrorType.EMBEDDED_REGION_REDEFINITION
                        : ErrorType.REGION_REDEFINITION, null, defT, GetUnmangledTemplateName(name));
                // keep the previous one
                return;
            }
            switch (code.RegionDefType) {
                // handle the Explicit/Embedded and Embedded/Explicit warning cases
                case Template.RegionType.Embedded when prev.RegionDefType == Template.RegionType.Explicit:
                case Template.RegionType.Explicit when prev.RegionDefType == Template.RegionType.Embedded: {
                    // TODO: can we make this a warning?
                    ErrorManager.CompiletimeError(ErrorType.HIDDEN_EMBEDDED_REGION_DEFINITION, null, defT, GetUnmangledTemplateName(name));
                    // keep the previous one only if that's the explicit definition
                    if (prev.RegionDefType == Template.RegionType.Explicit) {
                        return;
                    }
                    break;
                }
                // else if the current definition type is implicit, keep the previous one
                case Template.RegionType.Implicit: {
                    return;
                }
            }
        }
        code.NativeGroup = this;
        code.TemplateDefStartToken = defT;
        templates[name] = code;
    }

    public void UndefineTemplate(string name) {
        templates.Remove(name);
    }

    /** Compile a template */
    public CompiledTemplate Compile(string srcName,
        string name,
        List<FormalArgument> args,
        string template,
        IToken templateToken) {  // for error location
        _logger.LogTrace("TemplateGroup.Compile: {EnclosingTemplateName}", template);
        var c = new TemplateCompiler(this);
        return c.Compile(srcName, name, args, template, templateToken);
    }

    /** The "foo" of t() ::= "&lt;@foo()&gt;" is mangled to "/region__/t__foo" */
    public static string GetMangledRegionName(string enclosingTemplateName, string name) {
        if (enclosingTemplateName[0] != '/') {
            enclosingTemplateName = '/' + enclosingTemplateName;
        }
        return "/region__" + enclosingTemplateName + "__" + name;
    }

    /** Return "t.foo" from "/region__/t__foo" */
    public static string GetUnmangledTemplateName(string mangledName) {
        var t = mangledName.Substring("/region__".Length, mangledName.LastIndexOf("__", StringComparison.Ordinal) - "/region__".Length);
        var r = mangledName.Substring(
            mangledName.LastIndexOf("__", StringComparison.Ordinal) + 2,
            mangledName.Length - mangledName.LastIndexOf("__", StringComparison.Ordinal) - 2);
        return t + '.' + r;
    }

    /** Define a map for this group; not thread safe...do not keep adding
     *  these while you reference them.
     */
    public void DefineDictionary(string name, IDictionary<string, object> mapping) {
        dictionaries[name] = mapping;
    }

    public void SetDelimiters(IToken openDelimiter, IToken closeDelimiter) {
        if (openDelimiter == null) {
            throw new ArgumentNullException(nameof(openDelimiter));
        }
        if (closeDelimiter == null) {
            throw new ArgumentNullException(nameof(closeDelimiter));
        }
        var openDelimiterText = openDelimiter.Text.Trim('"');
        if (openDelimiterText.Length != 1) {
            ErrorManager.CompiletimeError(ErrorType.INVALID_DELIMITER, null, openDelimiter, openDelimiterText);
            return;
        }
        var closeDelimiterText = closeDelimiter.Text.Trim('"');
        if (closeDelimiterText.Length != 1) {
            ErrorManager.CompiletimeError(ErrorType.INVALID_DELIMITER, null, openDelimiter, closeDelimiterText);
            return;
        }
        SetDelimiters(openDelimiterText[0], closeDelimiterText[0]);
    }

    private void SetDelimiters(char delimiterStartChar, char delimiterStopChar) {
        DelimiterStartChar = delimiterStartChar;
        DelimiterStopChar = delimiterStopChar;
    }

    /** Make this group import templates/dictionaries from g. */
    public void ImportTemplates(ITemplateGroup g) {
        ImportTemplates(g as TemplateGroup, false);
    }

    /** Make this group import templates/dictionaries from g. */
    private void ImportTemplates(TemplateGroup group, bool clearOnUnload) {
        if (group == null) {
            return;
        }
        _imports.Add(group);
        if (clearOnUnload) {
            _importsToClearOnUnload.Add(group);
        }
    }

    /** Import template files, directories, and group files.
     *  Priority is given to templates defined in the current group;
     *  this, in effect, provides inheritance. Polymorphism is in effect so
     *  that if an inherited template references template t() then we
     *  search for t() in the subgroup first.
     *
     *  Templates are loaded on-demand from import dirs.  Imported groups are
     *  loaded on-demand when searching for a template.
     *
     *  The listener of this group is passed to the import group so errors
     *  found while loading imported element are sent to listener of this group.
     */
    public virtual void ImportTemplates(IToken fileNameToken) {
        var fileName = fileNameToken.Text;
        _logger.LogDebug("ImportTemplates({FileName})", fileName);
        // do nothing upon syntax error
        if (fileName == null || fileName.Equals("<missing STRING>")) {
            return;
        }
        fileName = Utility.Strip(fileName, 1);

        _logger.LogTrace("import {FileName}", fileName);
        var isGroupFile = fileName.EndsWith(GroupFileExtension);
        var isTemplateFile = fileName.EndsWith(TemplateFileExtension);
        TemplateGroup g;
        // search path is: working dir, g.stg's dir, CLASSPATH
        var thisRoot = RootDirUri;
        Uri fileUnderRoot;
        _logger.LogTrace("thisRoot={Root}", thisRoot);
        try {
            fileUnderRoot = new Uri(thisRoot + "/" + fileName);
        } catch (UriFormatException mfe) {
            ErrorManager.InternalError(null, $"can't build URL for {thisRoot}/{fileName}", mfe);
            return;
        }
        if (isTemplateFile) {
            g = new TemplateGroup(DelimiterStartChar, DelimiterStopChar) {
                Listener = Listener
            };
            if (TryOpenStream(fileUnderRoot, out var stream, out _)) {
                try {
                    var templateStream = new ANTLRInputStream(stream) {
                        name = fileName
                    };
                    var code = g.LoadTemplateFile("/", fileName, templateStream);
                    if (code == null) {
                        g = null;
                    }
                } catch (IOException ioe) {
                    ErrorManager.InternalError(null, $"can't read from {fileUnderRoot}", ioe);
                    g = null;
                }
            } else {
                g = null;
            }
        } else if (isGroupFile) {
            _logger.LogTrace("Looking for fileUnderRoot: {FileUnderRoot}", fileUnderRoot.LocalPath);
            if (File.Exists(fileUnderRoot.LocalPath)) {
                g = new TemplateGroupFile(fileUnderRoot, Encoding, DelimiterStartChar, DelimiterStopChar);
            } else {
                g = new TemplateGroupFile(fileName, Encoding, DelimiterStartChar, DelimiterStopChar);
            }
            g.Listener = Listener;
        } else /*isGroupDir*/ {
            if (Directory.Exists(fileUnderRoot.LocalPath)) {
                _logger.LogTrace("Try dir {Directory}", fileUnderRoot.LocalPath);
                g = new TemplateGroupDirectory(fileUnderRoot, Encoding, DelimiterStartChar, DelimiterStopChar);
            } else {
                _logger.LogTrace("Try in resource {FileName}", fileName);
                g = new TemplateGroupDirectory(fileName, Encoding, DelimiterStartChar, DelimiterStopChar);
            }
            g.Listener = Listener;
        }
        if (g == null) {
            ErrorManager.CompiletimeError(ErrorType.CANT_IMPORT, null, fileNameToken, fileName);
        } else {
            ImportTemplates(g, true);
        }
    }

    /** Load a group file with full path fileName; it's relative to root by prefix. */
    protected void LoadGroupFile(string prefix, Uri fileUri) {
        var fileName = fileUri.LocalPath;
        _logger.LogDebug("{Type}.LoadGroupFile(prefix={Prefix}, fileName={FileName})",
            GetType().FullName, prefix, fileName);
        try {
            if (!TryOpenStream(fileUri, out var stream, out var accessTime)) {
                throw new InvalidOperationException($"no such group: {fileUri}");
            }
            var fs = new ANTLRReaderStream(new StreamReader(stream, Encoding));
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var cachePath = Path.Combine(Path.GetTempPath(), "ST4TemplateCache");
            if (EnableCache && TryLoadGroupFromCache(cachePath, prefix, fileName, accessTime)) {
                System.Diagnostics.Debug.WriteLine("Successfully loaded the group from cache {0} in {1}ms.", Name, timer.ElapsedMilliseconds);
            } else {
                var lexer = new GroupLexer(fs);
                fs.name = fileName;
                var tokens = new CommonTokenStream(lexer);
                var parser = new GroupParser(tokens);
                parser.group(this, prefix);

                System.Diagnostics.Debug.WriteLine("Successfully loaded the group {0} in {1}ms.", Name, timer.ElapsedMilliseconds);

                if (EnableCache) {
                    CacheCompiledGroup(cachePath, prefix, fileName, accessTime);
                }
            }
        } catch (Exception e) when (!e.IsCritical()) {
            ErrorManager.IOError(null, ErrorType.CANT_LOAD_GROUP_FILE, e, fileName);
        }
    }

    private bool TryLoadGroupFromCache(string cachePath, string prefix, string fileName, DateTime accessTime) {
        var cacheFileName = Path.GetFileNameWithoutExtension(fileName) + (uint)fileName.GetHashCode() +
            prefix.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
        cacheFileName = Path.Combine(cachePath, cacheFileName);
        if (!File.Exists(cacheFileName)) {
            return false;
        }
        try {
            var data = File.ReadAllBytes(cacheFileName);
            return TryLoadCachedGroup(data, accessTime);
        } catch (IOException) {
            return false;
        }
    }

    private bool TryLoadCachedGroup(byte[] data, DateTime lastWriteTime) {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var reader = new BinaryReader(new MemoryStream(data), Encoding.UTF8);
        var cacheTime = new DateTime(reader.ReadInt64(), DateTimeKind.Utc);
        if (cacheTime != lastWriteTime) {
            return false;
        }
        var objects = new Dictionary<int, object> { { 0, null } };
        // first pass constructs objects
        var objectTableOffset = reader.BaseStream.Position;
        var objectCount = reader.ReadInt32();
        for (var i = 0; i < objectCount; i++) {
            var key = reader.ReadInt32();
            var obj = CreateGroupObject(reader, objects);
            objects.Add(key, obj);
        }
        reader.BaseStream.Seek(objectTableOffset + 4, SeekOrigin.Begin);
        for (var i = 0; i < objectCount; i++) {
            var key = reader.ReadInt32();
            LoadGroupObject(reader, key, objects);
        }

        var importsToClearOnUnload = new List<TemplateGroup>();
        var localTemplates = new Dictionary<string, CompiledTemplate>();
        var localDictionaries = new Dictionary<string, IDictionary<string, object>>();

        // imported groups
        var importCount = reader.ReadInt32();
        for (var i = 0; i < importCount; i++) {
            importsToClearOnUnload.Add((TemplateGroup)objects[reader.ReadInt32()]);
        }

        // delimiters
        var delimiterStartChar = reader.ReadChar();
        var delimiterStopChar = reader.ReadChar();

        // templates & aliases
        var templateCount = reader.ReadInt32();
        for (var i = 0; i < templateCount; i++) {
            var key = reader.ReadString();
            var value = (CompiledTemplate)objects[reader.ReadInt32()];
            localTemplates[key] = value;
        }

        // dictionaries
        var dictionaryCount = reader.ReadInt32();
        for (var i = 0; i < dictionaryCount; i++) {
            var name = reader.ReadString();
            IDictionary<string, object> dictionary = new Dictionary<string, object>();
            localDictionaries[name] = dictionary;
            var valueCount = reader.ReadInt32();
            for (var j = 0; j < valueCount; j++) {
                var key = reader.ReadString();
                var value = objects[reader.ReadInt32()];
                dictionary[key] = value;
            }
        }
        _importsToClearOnUnload.AddRange(importsToClearOnUnload);
        DelimiterStartChar = delimiterStartChar;
        DelimiterStopChar = delimiterStopChar;
        foreach (var pair in localTemplates) {
            templates[pair.Key] = pair.Value;
        }
        foreach (var pair in localDictionaries) {
            dictionaries[pair.Key] = pair.Value;
        }

        System.Diagnostics.Debug.WriteLine(
            $"Successfully loaded the cached group {Name} in {timer.ElapsedMilliseconds}ms.");
        return true;
    }

    private object CreateGroupObject(BinaryReader reader, Dictionary<int,object> objects) {
        var typeKey = reader.ReadInt32();
        if (typeKey == 0) {
            // this is a string
            return reader.ReadString();
        }
        var typeName = (string)objects[typeKey];
        if (typeName == typeof(bool).FullName) {
            return reader.ReadBoolean();
        }
        if (typeName == typeof(TemplateToken).FullName || typeName == typeof(CommonToken).FullName) {
            var channel = reader.ReadInt32();
            var charPositionInLine = reader.ReadInt32();
            var line = reader.ReadInt32();
            var startIndex = reader.ReadInt32();
            var stopIndex = reader.ReadInt32();
            var text = reader.ReadString();
            var tokenIndex = reader.ReadInt32();
            var type = reader.ReadInt32();
            var token = new CommonToken(type, text) {
                Channel = channel,
                CharPositionInLine = charPositionInLine,
                Line = line,
                StartIndex = startIndex,
                StopIndex = stopIndex,
                TokenIndex = tokenIndex,
            };

            return token;
        }
        if (typeName == typeof(CompiledTemplate).FullName) {
            var compiledTemplate = new CompiledTemplate {
                Name = reader.ReadString(),
                Prefix = reader.ReadString(),
                Template = reader.ReadString()
            };
            reader.ReadInt32(); // templateDefStartTokenObject
            compiledTemplate.HasFormalArgs = reader.ReadBoolean();
            reader.ReadInt32(); // nativeGroupObject
            compiledTemplate.IsRegion = reader.ReadBoolean();
            compiledTemplate.RegionDefType = (Template.RegionType)reader.ReadInt32();
            compiledTemplate.IsAnonSubtemplate = reader.ReadBoolean();

            var formalArgsLength = reader.ReadInt32();
            if (formalArgsLength > 0) {
                for (var i = 0; i < formalArgsLength; i++) {
                    reader.ReadInt32(); // formalArgObject
                }
            }

            var stringsLength = reader.ReadInt32();
            if (stringsLength >= 0) {
                compiledTemplate.strings = new string[stringsLength];
                for (var i = 0; i < stringsLength; i++) {
                    compiledTemplate.strings[i] = reader.ReadString();
                }
            }
            var instrsLength = reader.ReadInt32();
            if (instrsLength >= 0) {
                compiledTemplate.instrs = reader.ReadBytes(instrsLength);
            }
            compiledTemplate.codeSize = reader.ReadInt32();
            var sourceMapLength = reader.ReadInt32();
            if (sourceMapLength >= 0) {
                compiledTemplate.sourceMap = new Interval[sourceMapLength];
                for (var i = 0; i < sourceMapLength; i++) {
                    var start = reader.ReadInt32();
                    var length = reader.ReadInt32();
                    if (length >= 0) {
                        compiledTemplate.sourceMap[i] = new Interval(start, length);
                    }
                }
            }
            return compiledTemplate;
        }
        if (typeName == typeof(FormalArgument).FullName) {
            var name = reader.ReadString();
            var index = reader.ReadInt32();
            var defaultValueToken = (IToken)objects[reader.ReadInt32()];
            reader.ReadInt32(); // defaultValueObject
            reader.ReadInt32(); // compiledDefaultValue
            var formalArgument = new FormalArgument(name, defaultValueToken) {
                Index = index
            };
            return formalArgument;
        }
        if (typeName == typeof(Template).FullName) {
            reader.ReadInt32(); // implObject
            var localsCount = reader.ReadInt32();
            for (var i = 0; i < localsCount; i++) {
                reader.ReadInt32(); // localObject
            }
            reader.ReadInt32(); // groupObject
            return new Template(group:this);
        }
        if (typeName == typeof(TemplateGroupFile).FullName) {
            var isDefaultGroup = reader.ReadBoolean();
            if (!isDefaultGroup) {
                return this;
            }
            throw new NotSupportedException();
        }
        if (typeName == typeof(TemplateGroup).FullName) {
            var isDefaultGroup = reader.ReadBoolean();
            if (isDefaultGroup) {
                return DefaultGroup;
            }
            throw new NotSupportedException();
        }
        throw new NotImplementedException();
    }

    private void LoadGroupObject(BinaryReader reader, int key, Dictionary<int,object> objects) {
        var typeKey = reader.ReadInt32();
        if (typeKey == 0) {
            // this is a string, nothing more to load
            reader.ReadString();
            return;
        }
        var typeName = (string)objects[typeKey];
        if (typeName == typeof(bool).FullName) {
            // nothing more to load
            reader.ReadBoolean();
            return;
        }
        if (typeName == typeof(TemplateToken).FullName || typeName == typeof(CommonToken).FullName) {
            // nothing more to load
            reader.ReadInt32(); // channel
            reader.ReadInt32(); // charPositionInLine
            reader.ReadInt32(); // line
            reader.ReadInt32(); // startIndex
            reader.ReadInt32(); // stopIndex
            reader.ReadString(); // text
            reader.ReadInt32(); // tokenIndex
            reader.ReadInt32(); // type
            return;
        }
        if (typeName == typeof(CompiledTemplate).FullName) {
            var compiledTemplate = (CompiledTemplate)objects[key];
            reader.ReadString(); // name
            reader.ReadString(); // prefix
            reader.ReadString(); // template
            var templateDefStartTokenObject = reader.ReadInt32();
            reader.ReadBoolean(); // hasFormalArgs
            reader.ReadInt32(); // nativeGroupObject
            reader.ReadBoolean(); // isRegion
            reader.ReadInt32(); // regionDefType
            reader.ReadBoolean(); // isAnonSubtemplate
            compiledTemplate.TemplateDefStartToken = (IToken)objects[templateDefStartTokenObject];
            compiledTemplate.NativeGroup = this;
            var formalArgsLength = reader.ReadInt32();
            if (formalArgsLength >= 0) {
                var formalArguments = new List<FormalArgument>(formalArgsLength);
                for (var i = 0; i < formalArgsLength; i++) {
                    var formalArgObject = reader.ReadInt32();
                    formalArguments.Add((FormalArgument)objects[formalArgObject]);
                }
                compiledTemplate.FormalArguments = formalArguments;
            }
            var stringsLength = reader.ReadInt32();
            for (var i = 0; i < stringsLength; i++) {
                reader.ReadString();
            }
            var instrsLength = reader.ReadInt32();
            if (instrsLength >= 0) {
                reader.ReadBytes(instrsLength);
            }
            reader.ReadInt32(); // codeSize
            var sourceMapLength = reader.ReadInt32();
            for (var i = 0; i < sourceMapLength; i++) {
                reader.ReadInt32(); // start
                reader.ReadInt32(); // length
            }
            return;
        }
        if (typeName == typeof(FormalArgument).FullName) {
            var formalArgument = (FormalArgument)objects[key];
            reader.ReadString(); // name
            reader.ReadInt32(); // index
            reader.ReadInt32(); // index of defaultValueToken
            var defaultValueObject = reader.ReadInt32();
            var compiledDefaultValue = reader.ReadInt32();
            formalArgument.DefaultValue = objects[defaultValueObject];
            formalArgument.CompiledDefaultValue = (CompiledTemplate)objects[compiledDefaultValue];
            return;
        }
        if (typeName == typeof(Template).FullName) {
            var template = (Template)objects[key];
            var implObject = reader.ReadInt32();
            template.impl = (CompiledTemplate)objects[implObject];
            var localsCount = reader.ReadInt32();
            if (localsCount >= 0) {
                template.locals = new object[localsCount];
                for (var i = 0; i < localsCount; i++) {
                    var localObject = reader.ReadInt32();
                    template.locals[i] = objects[localObject];
                }
            }
            var groupObject = reader.ReadInt32();
            template.Group = (TemplateGroup)objects[groupObject];
            return;
        }
        if (typeName == typeof(TemplateGroupFile).FullName) {
            reader.ReadBoolean(); // isDefaultGroup
            return;
        }
        if (typeName == typeof(TemplateGroup).FullName) {
            reader.ReadBoolean(); // isDefaultGroup
            return;
        }
        throw new NotImplementedException();
    }

    private void CacheCompiledGroup(string cachePath, string prefix, string fileName, DateTime lastWriteTime) {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var comparer = ObjectReferenceEqualityComparer<object>.Default;
        var serializedObjects = new HashSet<object>(ObjectReferenceEqualityComparer<object>.Default);
        // start with the root set
        serializedObjects.UnionWith(_importsToClearOnUnload.Where(o => o != null));
        serializedObjects.UnionWith(templates.Values.Where(o => o != null));
        serializedObjects.UnionWith(dictionaries.Values.SelectMany(i => i.Values).Where(o => o != null));
        // update to the reachable set
        serializedObjects = CalculateReachableSerializedObjects(serializedObjects.ToArray());
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream, Encoding.UTF8);
        writer.Write(lastWriteTime.Ticks);
        // objects
        var orderedObjectsForExport = GetOrderedExports(serializedObjects);
        writer.Write(orderedObjectsForExport.Count);
        foreach (var obj in orderedObjectsForExport) {
            WriteGroupObject(writer, obj);
        }
        // imported groups
        writer.Write(_importsToClearOnUnload.Count);
        foreach (var group in _importsToClearOnUnload) {
            writer.Write(comparer.GetHashCode(group));
        }
        // delimiters
        writer.Write(DelimiterStartChar);
        writer.Write(DelimiterStopChar);
        // templates & aliases
        writer.Write(templates.Count);
        foreach (var template in templates) {
            writer.Write(template.Key);
            writer.Write(comparer.GetHashCode(template.Value));
        }
        // dictionaries
        writer.Write(dictionaries.Count);
        foreach (var dictionary in dictionaries) {
            writer.Write(dictionary.Key);
            writer.Write(dictionary.Value.Count);
            foreach (var dictionaryValue in dictionary.Value) {
                writer.Write(dictionaryValue.Key);
                writer.Write(comparer.GetHashCode(dictionaryValue.Value));
            }
        }
        System.Diagnostics.Debug.WriteLine(
            $"Successfully cached the group {Name} in {timer.ElapsedMilliseconds}ms ({stream.Length} bytes).");
        Directory.CreateDirectory(cachePath);
        var cacheFileName = Path.GetFileNameWithoutExtension(fileName) + (uint)fileName.GetHashCode() +
            prefix.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
        cacheFileName = Path.Combine(cachePath, cacheFileName);
        File.WriteAllBytes(cacheFileName, stream.ToArray());
    }

    private List<object> GetOrderedExports(IEnumerable<object> serializedObjects) {
        var exportList = new List<object>();
        var visited = new HashSet<object>(ObjectReferenceEqualityComparer<object>.Default);
        foreach (var obj in serializedObjects) {
            GetOrderedExports(obj, exportList, visited);
        }
        return exportList;
    }

    private void GetOrderedExports(object currentObject, List<object> exportList, HashSet<object> visited) {
        if (currentObject == null || !visited.Add(currentObject)) {
            return;
        }
        // constructor dependencies
        if (currentObject is not Type && currentObject is not string) {
            GetOrderedExports(currentObject.GetType(), exportList, visited);
        }
        if (currentObject is FormalArgument formalArgument) {
            GetOrderedExports(formalArgument.DefaultValueToken, exportList, visited);
        }
        exportList.Add(currentObject);
    }

    private void WriteGroupObjectReference(BinaryWriter writer, object obj) {
        writer.Write(obj != null ? ObjectReferenceEqualityComparer<object>.Default.GetHashCode(obj) : 0);
    }

    private void WriteGroupObject(BinaryWriter writer, object obj) {
        var comparer = ObjectReferenceEqualityComparer<object>.Default;
        writer.Write(comparer.GetHashCode(obj));
        switch (obj) {
            case string str:
                writer.Write(0);
                writer.Write(str);
                return;
            case Type type:
                writer.Write(0);
                writer.Write(type.FullName!);
                return;
        }
        WriteGroupObjectReference(writer, obj.GetType());
        switch (obj) {
            case bool b:
                writer.Write(b);
                return;
            case IToken token:
                writer.Write(token.Channel);
                writer.Write(token.CharPositionInLine);
                writer.Write(token.Line);
                writer.Write(token.StartIndex);
                writer.Write(token.StopIndex);
                writer.Write(token.Text);
                writer.Write(token.TokenIndex);
                writer.Write(token.Type);
                return;
            case CompiledTemplate compiledTemplate: {
                writer.Write(compiledTemplate.Name);
                writer.Write(compiledTemplate.Prefix);
                writer.Write(compiledTemplate.Template);
                WriteGroupObjectReference(writer, compiledTemplate.TemplateDefStartToken);
                writer.Write(compiledTemplate.HasFormalArgs);
                WriteGroupObjectReference(writer, compiledTemplate.NativeGroup);
                writer.Write(compiledTemplate.IsRegion);
                writer.Write((int)compiledTemplate.RegionDefType);
                writer.Write(compiledTemplate.IsAnonSubtemplate);
                if (compiledTemplate.FormalArguments == null) {
                    writer.Write(-1);
                } else {
                    writer.Write(compiledTemplate.FormalArguments.Count);
                    foreach (var arg in compiledTemplate.FormalArguments) {
                        WriteGroupObjectReference(writer, arg);
                    }
                }
                if (compiledTemplate.strings == null) {
                    writer.Write(-1);
                } else {
                    writer.Write(compiledTemplate.strings.Length);
                    foreach (var s in compiledTemplate.strings) {
                        writer.Write(s);
                    }
                }
                if (compiledTemplate.instrs == null) {
                    writer.Write(-1);
                } else {
                    writer.Write(compiledTemplate.instrs.Length);
                    writer.Write(compiledTemplate.instrs);
                }
                writer.Write(compiledTemplate.codeSize);
                if (compiledTemplate.sourceMap == null) {
                    writer.Write(-1);
                } else {
                    writer.Write(compiledTemplate.sourceMap.Length);
                    foreach (var interval in compiledTemplate.sourceMap) {
                        if (interval == null) {
                            writer.Write(-1);
                            writer.Write(-1);
                        }
                        else {
                            writer.Write(interval.Start);
                            writer.Write(interval.Length);
                        }
                    }
                }
                return;
            }
            case FormalArgument formalArgument:
                writer.Write(formalArgument.Name);
                writer.Write(formalArgument.Index);
                WriteGroupObjectReference(writer, formalArgument.DefaultValueToken);
                WriteGroupObjectReference(writer, formalArgument.DefaultValue);
                WriteGroupObjectReference(writer, formalArgument.CompiledDefaultValue);
                return;
            case Template template: {
                WriteGroupObjectReference(writer, template.impl);
                if (template.locals == null) {
                    writer.Write(-1);
                } else {
                    writer.Write(template.locals.Length);
                    foreach (var local in template.locals) {
                        WriteGroupObjectReference(writer, local);
                    }
                }
                WriteGroupObjectReference(writer, template.Group);
                return;
            }
            case TemplateGroupFile:
                writer.Write(false);
                return;
        }

        if (obj == DefaultGroup) {
            writer.Write(true);
            return;
        }
        throw new NotImplementedException();
    }

    private HashSet<object> CalculateReachableSerializedObjects(ICollection<object> rootSet) {
        var reachableObjects = new HashSet<object>(ObjectReferenceEqualityComparer<object>.Default);
        foreach (var obj in rootSet) {
            CalculateReachableSerializedObjects(obj, reachableObjects);
        }
        return reachableObjects;
    }

    private void CalculateReachableSerializedObjects(object obj, HashSet<object> reachableObjects) {
        if (obj == null || !reachableObjects.Add(obj)) {
            return;
        }
        CalculateReachableSerializedObjects(obj.GetType(), reachableObjects);
        switch (obj) {
            case bool:
            case string:
            case IToken:
            case Type:
                // nothing more to do
                return;
            case CompiledTemplate compiledTemplate: {
                CalculateReachableSerializedObjects(compiledTemplate.NativeGroup, reachableObjects);
                CalculateReachableSerializedObjects(compiledTemplate.TemplateDefStartToken, reachableObjects);
                if (compiledTemplate.FormalArguments != null) {
                    foreach (var argument in compiledTemplate.FormalArguments) {
                        CalculateReachableSerializedObjects(argument, reachableObjects);
                    }
                }

                if (compiledTemplate.ImplicitlyDefinedTemplates != null) {
                    foreach (var t in compiledTemplate.ImplicitlyDefinedTemplates) {
                        CalculateReachableSerializedObjects(t, reachableObjects);
                    }
                }
                return;
            }
            case FormalArgument formalArgument:
                CalculateReachableSerializedObjects(formalArgument.DefaultValueToken, reachableObjects);
                CalculateReachableSerializedObjects(formalArgument.DefaultValue, reachableObjects);
                CalculateReachableSerializedObjects(formalArgument.CompiledDefaultValue, reachableObjects);
                return;
            case Template template: {
                CalculateReachableSerializedObjects(template.impl, reachableObjects);
                if (template.locals != null) {
                    foreach (var local in template.locals) {
                        CalculateReachableSerializedObjects(local, reachableObjects);
                    }
                }

                CalculateReachableSerializedObjects(template.Group, reachableObjects);
                return;
            }
            case TemplateGroupFile: {
                if (obj != this) {
                    throw new NotSupportedException();
                }
                return;
            }
            case TemplateGroup: {
                if (obj != DefaultGroup) {
                    throw new NotSupportedException();
                }
                return;
            }
        }
        throw new NotImplementedException();
    }

    /** Load template file into this group using absolute filename */
    public CompiledTemplate LoadAbsoluteTemplateFile(string fileName) {
        ANTLRReaderStream fs;
        try {
            fs = new ANTLRReaderStream(new StreamReader(File.OpenRead(fileName), Encoding)) {
                name = fileName
            };
        } catch (IOException) {
            // doesn't exist
            //errMgr.IOError(null, ErrorType.NO_SUCH_TEMPLATE, ioe, fileName);
            return null;
        }
        return LoadTemplateFile("", fileName, fs);
    }

    /** Load template stream into this group. unqualifiedFileName is "a.st".
     *  The prefix is path from group root to unqualifiedFileName like /subdir
     *  if file is in /subdir/a.st
     */
    protected virtual CompiledTemplate LoadTemplateFile(string prefix, string unqualifiedFileName, ICharStream templateStream) {
        var lexer = new GroupLexer(templateStream);
        var tokens = new CommonTokenStream(lexer);
        var parser = new GroupParser(tokens) {
            Group = this
        };
        lexer.group = this;
        try {
            parser.templateDef(prefix);
        } catch (RecognitionException re) {
            ErrorManager.GroupSyntaxError(ErrorType.SYNTAX_ERROR, unqualifiedFileName, re, re.Message);
        }
        var templateName = Path.GetFileNameWithoutExtension(unqualifiedFileName);
        if (!string.IsNullOrEmpty(prefix)) {
            templateName = prefix + templateName;
        }
        var impl = RawGetTemplate(templateName);
        if (impl == null) {
            return null;
        }
        impl.Prefix = prefix;
        return impl;
    }

    /** Add an adaptor for a kind of object so Template knows how to pull properties
     *  from them. Add adaptors in increasing order of specificity.  Template adds Object,
     *  Map, and Template model adaptors for you first. Adaptors you Add have
     *  priority over default adaptors.
     *
     *  If an adaptor for type T already exists, it is replaced by the adaptor arg.
     *
     *  This must invalidate cache entries, so set your adaptors up before
     *  Render()ing your templates for efficiency.
     */
    public void RegisterModelAdaptor(Type attributeType, IModelAdaptor adaptor) {
        adaptors[attributeType] = adaptor;
    }

    public IModelAdaptor GetModelAdaptor(Type attributeType) {
        return adaptors.TryGetValue(attributeType, out var adaptor) ? adaptor : null;
    }

    /** Register a renderer for all objects of a particular "kind" for all
     *  templates evaluated relative to this group.  Use r to Render if
     *  object in question is instanceof(attributeType).
     */
    public void RegisterRenderer(Type attributeType, IAttributeRenderer renderer, bool recursive = true) {
        renderers ??= new TypeRegistry<IAttributeRenderer>();
        renderers[attributeType] = renderer;
        if (!recursive) {
            return;
        }
        Load();
        foreach (var group in ImportedGroups) {
            group.RegisterRenderer(attributeType, renderer, recursive:true);
        }
    }

    public IAttributeRenderer GetAttributeRenderer(Type attributeType) {
        if (renderers == null) {
            return null;
        }
        renderers.TryGetValue(attributeType, out var renderer);
        return renderer;
    }

    public void RegisterTypeProxyFactory(Type targetType, ITypeProxyFactory factory) {
        _proxyFactories ??= new TypeRegistry<ITypeProxyFactory>();
        _proxyFactories[targetType] = factory;
    }

    public ITypeProxyFactory GetTypeProxyFactory(Type targetType) {
        if (_proxyFactories == null) {
            return null;
        }
        _proxyFactories.TryGetValue(targetType, out var factory);
        return factory;
    }

    private Template CreateStringTemplate(CompiledTemplate impl) {
        var st = new Template(this) {
            impl = impl
        };
        if (impl.FormalArguments != null) {
            st.locals = new object[impl.FormalArguments.Count];
            for (var i = 0; i < st.locals.Length; i++) {
                st.locals[i] = Template.EmptyAttribute;
            }
        }
        return st;
    }

    /** differentiate so we can avoid having creation events for regions,
     *  map operations, and other "new ST" events used during interp.
     */
    public Template CreateStringTemplateInternally(CompiledTemplate impl) {
        var template = CreateStringTemplate(impl);
        if (TrackCreationEvents && template.DebugState != null) {
            // toss it out
            template.DebugState.NewTemplateEvent = null;
        }
        return template;
    }

    public Template CreateStringTemplateInternally(Template prototype) {
        // no need to wack debugState; not set in ST(proto).
        return new Template(prototype);
    }

    public virtual string Name => "<no name>;";

    public virtual string FileName => null;

    /** Return root dir if this is group dir; return dir containing group file
     *  if this is group file.  This is derived from original incoming
     *  dir or filename.  If it was absolute, this should come back
     *  as full absolute path.  If only a URL is available, return URL of
     *  one dir up.
     */
    protected virtual Uri RootDirUri => null;

    public override string ToString() => Name;

    public virtual string Description {
        get {
            var buf = new StringBuilder();
            if (_imports is { Count: > 0 }) {
                buf.Append(" : " + _imports);
            }
            foreach (var n in templates.Keys) {
                var name = n;
                var c = templates[name];
                if (c.IsAnonSubtemplate || c == NotFoundTemplate) {
                    continue;
                }
                var slash = name.LastIndexOf('/');
                name = name.Substring(slash + 1, name.Length - slash - 1);
                buf.Append(name);
                buf.Append('(');
                if (c.FormalArguments != null) {
                    buf.Append(string.Join(",", c.FormalArguments.Select(i => i.ToString()).ToArray()));
                }
                buf.Append(')');
                buf.Append(" ::= <<" + Environment.NewLine);
                buf.Append(c.Template + Environment.NewLine);
                buf.Append(">>" + Environment.NewLine);
            }
            return buf.ToString();
        }
    }

    public ITemplateErrorListener Listener {
        get => ErrorManager?.Listener;
        set => ErrorManager = new ErrorManager(value);
    }

    public ISet<string> TemplateNames {
        get {
            Load();
            var result = new HashSet<string>();
            foreach (var e in templates) {
                if (e.Value != NotFoundTemplate) {
                    result.Add(e.Key);
                }
            }
            return result;
        }
    }

    protected bool TryOpenStream(Uri uri, out Stream inputStream, out DateTime lastModified) {
        if (uri.IsFile) {
            if (File.Exists(uri.LocalPath)) {
                inputStream = File.OpenRead(uri.LocalPath);
                lastModified = File.GetLastWriteTimeUtc(uri.LocalPath);
                return inputStream != null;
            }
        } else {
            var asm = ResourceAssembly ?? Assembly.GetExecutingAssembly();
            var uriName = Regex.Replace(uri.Host + uri.LocalPath, "/+", ".").TrimEnd('.');
            var resName = asm.GetManifestResourceNames().FirstOrDefault(
                str => str.EndsWith(uriName, StringComparison.OrdinalIgnoreCase));
            if (resName != null) {
                inputStream = asm.GetManifestResourceStream(resName);
                lastModified = DateTime.MinValue;
                return inputStream != null;
            }
        }
        inputStream = null;
        lastModified = DateTime.MinValue;
        return  false;
    }
        
    public static Assembly ResourceAssembly { get; set; }

    public static string ResourceRoot { get; set; } = "Resources";

    protected string ToResourceNameInAssembly(string name, Assembly asm) {
        var res = new StringBuilder(asm.GetName().Name);
        res.Append('.');
        if (!string.IsNullOrEmpty(ResourceRoot)) {
            res.Append(ResourceRoot);
            if (name.Length != 0) {
                res.Append('.');
            }
        }
        res.Append(name);
        return res.ToString().Replace("/", ".");
    }

}
