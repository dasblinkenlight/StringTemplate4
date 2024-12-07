using System;
using System.Collections.Generic;
using Antlr4.StringTemplate.Compiler;

namespace Antlr4.StringTemplate;

public interface ITemplateGroup {
    string Description { get; }
    ISet<string> TemplateNames { get; }
    Template GetInstanceOf(string name);
    CompiledTemplate LookupTemplate(string name);
    bool IsDefined(string name);
    void RegisterRenderer(Type attributeType, IAttributeRenderer renderer, bool recursive = true);
    void DefineTemplate(string name, string template);
    void DefineTemplate(string name, string template, string[] arguments);
    void ImportTemplates(ITemplateGroup otherGroup);
    void RegisterModelAdaptor(Type attributeType, IModelAdaptor adaptor);
    IModelAdaptor GetModelAdaptor(Type attributeType);
    void Load();
    void Unload();
}
