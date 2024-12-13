/*
 * [The "BSD license"]
 * Copyright (c) 2011 Terence Parr
 * All rights reserved.
 *
 * Conversion to C#:
 * Copyright (c) 2024 Sergey Kalinichenko
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Antlr4.StringTemplate.Misc;

public static class BuiltinModelAdaptors {

    private static readonly Dictionary<Type, Dictionary<string, Func<object, object>>> _memberAccessors = new();

    public static object MapModelAdaptorDelegate(object o, object property, string propertyName) {
        var map = (IDictionary)o;
        object value;
        if (property == null) {
            value = map[TemplateGroup.DefaultKey];
        } else if (map.Contains(property)) {
            value = map[property];
        } else if (map.Contains(propertyName)) {
            value = map[propertyName]; // if we can't find the key, try ToString version
        } else if (property.Equals("keys")) {
            value = map.Keys;
        } else if (property.Equals("values")) {
            value = map.Values;
        } else {
            value = map[TemplateGroup.DefaultKey]; // not found, use default
        }
        if (ReferenceEquals(value, TemplateGroup.DictionaryKey)) {
            value = property;
        }
        return value;
    }

    public static object AggregateModelAdaptorDelegate(object o, object property, string propertyName) =>
        MapModelAdaptorDelegate(((Aggregate)o).Properties, property, propertyName);

    public static object TemplateModelAdaptorDelegate(object o, object _, string propertyName) =>
        ((Template)o).GetAttribute(propertyName);

    public static object ObjectModelAdaptorDelegate(object o, object property, string propertyName) {
        if (o == null) {
            throw new ArgumentNullException(nameof(o));
        }
        var c = o.GetType();
        if (o is DynamicXml xml) {
            xml.TryGetMember(new Goof(propertyName, true), out var x3);
            return x3;
        }
        if (property == null) {
            throw new TemplateNoSuchPropertyException(o, $"{c.FullName}.{propertyName ?? "null"}");
        }
        object value;
        var accessor = FindMember(c, propertyName);
        if (accessor != null) {
            value = accessor(o);
        } else {
            throw new TemplateNoSuchPropertyException(o, $"{c.FullName}.{propertyName}");
        }
        return value;
    }

    private static Func<object, object> FindMember(Type type, string name) {
        if (type == null) {
            throw new ArgumentNullException(nameof(type));
        }

        if (name == null) {
            throw new ArgumentNullException(nameof(name));
        }
        lock (_memberAccessors) {
            Func<object, object> accessor = null;

            if (_memberAccessors.TryGetValue(type, out var members)) {
                if (members.TryGetValue(name, out accessor)) {
                    return accessor;
                }
            } else {
                members = new Dictionary<string, Func<object, object>>();
                _memberAccessors[type] = members;
            }

            // must look up using reflection
            var methodSuffix = char.ToUpperInvariant(name[0]) + name.Substring(1);
            var checkOriginalName = !string.Equals(methodSuffix, name);

            MethodInfo method = null;
            if (method == null) {
                var p = type.GetProperty(methodSuffix);
                if (p == null && checkOriginalName)
                    p = type.GetProperty(name);

                if (p != null)
                    method = p.GetGetMethod();
            }

            if (method == null) {
                method = type.GetMethod("Get" + methodSuffix, Type.EmptyTypes);
                if (method == null && checkOriginalName)
                    method = type.GetMethod("Get" + name, Type.EmptyTypes);
            }

            if (method == null) {
                method = type.GetMethod("get_" + methodSuffix, Type.EmptyTypes);
                if (method == null && checkOriginalName)
                    method = type.GetMethod("get_" + name, Type.EmptyTypes);
            }

            if (method == null) {
                method = type.GetMethod(name, Type.EmptyTypes);
            }

            if (method != null) {
                accessor = BuildAccessor(method);
            } else {
                // try for an indexer
                method = type.GetMethod("get_Item", [typeof(string)]);
                if (method == null) {
                    var property = type.GetProperties().FirstOrDefault(IsIndexer);
                    if (property != null)
                        method = property.GetGetMethod();
                }

                if (method != null) {
                    accessor = BuildAccessor(method, name);
                } else {
                    // try for a visible field
                    var field = type.GetField(name);
                    // also check .NET naming convention for fields
                    if (field == null)
                        field = type.GetField("_" + name);

                    if (field != null)
                        accessor = BuildAccessor(field);
                }
            }

            members[name] = accessor;

            return accessor;
        }
    }

    private static bool IsIndexer(PropertyInfo propertyInfo) {
        if (propertyInfo == null) {
            throw new ArgumentNullException(nameof(propertyInfo));
        }
        var indexParameters = propertyInfo.GetIndexParameters();
        return indexParameters.Length > 0 && indexParameters[0].ParameterType == typeof(string);
    }

    private static Func<object, object> BuildAccessor(MethodInfo method) {
        var obj = Expression.Parameter(typeof(object), "obj");
        var instance = !method.IsStatic ? Expression.Convert(obj, method.DeclaringType!) : null;
        var expr = Expression.Lambda<Func<object, object>>(
            Expression.Convert(
                Expression.Call(instance, method),
                typeof(object)),
            obj);

        return expr.Compile();
    }

    /// <summary>
    /// Builds an accessor for an indexer property that returns a takes a string argument.
    /// </summary>
    private static Func<object, object> BuildAccessor(MethodInfo method, string argument) {
        var obj = Expression.Parameter(typeof(object), "obj");
        var instance = !method.IsStatic ? Expression.Convert(obj, method.DeclaringType!) : null;
        var expr = Expression.Lambda<Func<object, object>>(
            Expression.Convert(
                Expression.Call(instance, method, Expression.Constant(argument)),
                typeof(object)),
            obj);

        return expr.Compile();
    }

    private static Func<object, object> BuildAccessor(FieldInfo field) {
        var obj = Expression.Parameter(typeof(object), "obj");
        var instance = !field.IsStatic ? Expression.Convert(obj, field.DeclaringType!) : null;
        var expr = Expression.Lambda<Func<object, object>>(
            Expression.Convert(
                Expression.Field(instance, field),
                typeof(object)),
            obj);

        return expr.Compile();
    }

}
