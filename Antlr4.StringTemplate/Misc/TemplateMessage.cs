/*
 * [The "BSD licence"]
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

namespace Antlr4.StringTemplate.Misc;

using Exception = System.Exception;
using StringWriter = System.IO.StringWriter;

/** Upon error, Template creates a TemplateMessage or subclass instance and notifies
 *  the listener.  This root class is used for IO and internal errors.
 *
 *  @see TemplateRuntimeMessage
 *  @see TemplateCompileTimeMessage
 */
public class TemplateMessage
{
    private TemplateMessage(ErrorType error) {
        Error = error;
    }

    private TemplateMessage(ErrorType error, Template self)
    : this(error) {
        Self = self;
    }

    public TemplateMessage(ErrorType error, Template self, Exception cause)
    : this(error, self) {
        Cause = cause;
    }

    public TemplateMessage(ErrorType error, Template self, Exception cause, object arg)
    : this(error, self, cause) {
        Arg = arg;
    }

    protected TemplateMessage(ErrorType error, Template self, Exception cause, object arg, object arg2)
    : this(error, self, cause, arg) {
        Arg2 = arg2;
    }

    protected TemplateMessage(ErrorType error, Template self, Exception cause, object arg, object arg2, object arg3)
    : this(error, self, cause, arg, arg2) {
        Arg3 = arg3;
    }

    /** if in debug mode, has created instance, Add attr events and eval
     *  template events.
     */
    protected Template Self { get; }

    public ErrorType Error { get; }

    protected object Arg { get; }

    protected object Arg2 { get; }

    private object Arg3 { get; }

    public Exception Cause { get; }

    public override string ToString() {
        var sw = new StringWriter();
        var msg = string.Format(Error.Message, Arg, Arg2, Arg3);
        sw.Write(msg);
        if (Cause == null) {
            return sw.ToString();
        }
        sw.WriteLine();
        sw.Write("Caused by: ");
        sw.WriteLine(Cause.Message);
        sw.Write(Cause.StackTrace);
        return sw.ToString();
    }

}
