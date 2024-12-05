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
using System.Linq;
using System.Reflection;
using Antlr.Runtime;
using Microsoft.Extensions.Logging;
using ArgumentException = System.ArgumentException;
using ArgumentNullException = System.ArgumentNullException;
using Directory = System.IO.Directory;
using Encoding = System.Text.Encoding;
using Exception = System.Exception;
using File = System.IO.File;
using IOException = System.IO.IOException;
using NotSupportedException = System.NotSupportedException;
using Path = System.IO.Path;
using StreamReader = System.IO.StreamReader;
using Uri = System.Uri;
using UriFormatException = System.UriFormatException;

namespace Antlr4.StringTemplate;

using Compiler;
using Misc;

// TODO: caching?

/** A directory or directory tree full of templates and/or group files.
 *  We load files on-demand. If we fail to find a file, we look for it via
 *  the CLASSPATH as a resource.  I track everything with URLs not file names.
 */
public class TemplateGroupDirectory : TemplateGroup {
    private readonly string groupDirName;
    private readonly Uri root;

    public TemplateGroupDirectory(string dirName, char delimiterStartChar = '<', char delimiterStopChar =  '>')
    : base(delimiterStartChar, delimiterStopChar) {
        groupDirName = dirName;
        try {
            if (Directory.Exists(dirName)) {
                // we found the directory and it'll be file based
                root = new Uri(dirName);
            } else {
                var asm = ResourceAssembly ?? Assembly.GetExecutingAssembly();
                var res = ToResourceNameInAssembly(dirName, asm);
                var dirPrefix = res + ".";
                if (asm.GetManifestResourceNames()
                    .Any(n => n.StartsWith(dirPrefix, StringComparison.OrdinalIgnoreCase))) {
                    root = new Uri("resource://" + res);
                }
            }
            _logger.LogDebug("TemplateGroupDirectory({DirName}) found at {Root}", dirName, root);
        } catch (Exception e) {
            ErrorManager.InternalError(null, "can't Load group dir " + dirName, e);
        }
    }

    protected TemplateGroupDirectory(string dirName, Encoding encoding, char delimiterStartChar = '<', char delimiterStopChar = '>')
    : this(dirName, delimiterStartChar, delimiterStopChar) {
        Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
    }

    public TemplateGroupDirectory(Uri root, Encoding encoding, char delimiterStartChar, char delimiterStopChar)
        : base(delimiterStartChar, delimiterStopChar) {
        groupDirName = Path.GetFileName(root.AbsolutePath);
        this.root = root;
        Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
    }

    public override void ImportTemplates(IToken fileNameToken) {
        var msg =
            "import illegal in group files embedded in TemplateGroupDirectory; " +
            "import " + fileNameToken.Text + " in TemplateGroupDirectory " + Name;
        throw new NotSupportedException(msg);
    }

    /** <summary>
     * Load a template from dir or group file.  Group file is given
     * precedence over dir with same name. <paramref name="name"/> is
     * always fully qualified.
     * </summary>
     */
    protected override CompiledTemplate Load(string name) {
        _logger.LogDebug("STGroupDir.load({TemplateName})", name );
        var parent = Utility.GetParent(name); // must have parent; it's fully-qualified
        var prefix = Utility.GetPrefix(name);
        //    	if (parent.isEmpty()) {
        //    		// no need to check for a group file as name has no parent
        //            return loadTemplateFile("/", name+TemplateFileExtension); // load t.st file
        //    	}

        if (!Path.IsPathRooted(parent)) {
            throw new ArgumentException();
        }
        Uri groupFileURL;
        try {
            // see if parent of template name is a group file
            groupFileURL = new Uri(TemplateName.GetTemplatePath(root.LocalPath, parent) + GroupFileExtension);
        } catch (UriFormatException e) {
            ErrorManager.InternalError(null, "bad URL: " + TemplateName.GetTemplatePath(root.LocalPath, parent) + GroupFileExtension, e);
            return null;
        }
        if (!File.Exists(groupFileURL.LocalPath)) {
            var unqualifiedName = Path.GetFileName(name);
            return LoadTemplateFile(prefix, unqualifiedName + TemplateFileExtension); // load t.st file
        }
        LoadGroupFile(prefix, groupFileURL);
        return RawGetTemplate(name);
    }

    /** Load full path name .st file relative to root by prefix */
    private CompiledTemplate LoadTemplateFile(string prefix, string unqualifiedFileName) {
        if (Path.IsPathRooted(unqualifiedFileName)) {
            throw new ArgumentException();
        }
        _logger.LogDebug("loadTemplateFile({FileName}) in group dir from {Root} prefix={Prefix}",
            unqualifiedFileName, root, prefix);
        Uri f;
        try {
            var uriBuilder = new UriBuilder(root);
            uriBuilder.Path += prefix + unqualifiedFileName;
            f = uriBuilder.Uri;
        } catch (UriFormatException me) {
            ErrorManager.RuntimeError(null, ErrorType.INVALID_TEMPLATE_NAME, me, Path.Combine(root.LocalPath, unqualifiedFileName));
            return null;
        }
        ANTLRReaderStream fs;
        try {
            if (TryOpenStream(f, out var inputStream, out _)) {
                fs = new ANTLRReaderStream(new StreamReader(inputStream, Encoding)) {
                    name = unqualifiedFileName
                };
            } else {
                _logger.LogDebug("{Root}/{FileName} doesn't exist", root, unqualifiedFileName);
                return null;
            }
        } catch (IOException) {
            _logger.LogDebug("{Root}/{FileName} doesn't exist", root, unqualifiedFileName);
            //errMgr.IOError(null, ErrorType.NO_SUCH_TEMPLATE, ioe, unqualifiedFileName);
            return null;
        }
        return LoadTemplateFile(prefix, unqualifiedFileName, fs);
    }

    public override string Name => groupDirName;

    public override string FileName => Path.GetFileName(root.LocalPath);

    protected override Uri RootDirUri => root;
}
