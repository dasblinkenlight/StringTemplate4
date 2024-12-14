using System.Collections.Generic;
using Antlr4.StringTemplate.Compiler;

namespace Antlr4.StringTemplate.Debug;

internal static class TemplateDebugExtensions {

    public static CompiledTemplate GetCompiledTemplate(this ITemplate template) => (template as Template)?.impl;

    public static List<InterpEvent> GetEvents(this ITemplate template) =>
        (template as Template)?.GetEvents();

    public static List<InterpEvent> GetEvents(this ITemplate template, int lineWidth) =>
        (template as Template)?.GetEvents(lineWidth);

    public static List<InterpEvent> GetEvents(this ITemplate template, ITemplateWriter writer) =>
        (template as Template)?.GetEvents(writer);

    public static void DefineTemplate(this ITemplateGroup group, string name, string template) =>
        (group as TemplateGroup)?.DefineTemplate(name, template);

    public static void DefineTemplate(this ITemplateGroup group, string name, string template, string[] arguments) =>
        (group as TemplateGroup)?.DefineTemplate(name, template, arguments);

    public static string GetTemplateGroupName(this ITemplate template) => ((Template)template).Group.Name;

    public static void SetTemplateGroup(this ITemplate template, ITemplateGroup group) =>
        (template as Template)!.Group = group as TemplateGroup;

}
