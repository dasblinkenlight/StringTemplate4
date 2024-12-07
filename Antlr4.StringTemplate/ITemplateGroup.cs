using System;
using System.Collections.Generic;

namespace Antlr4.StringTemplate;

public interface ITemplateGroup {
    string Name { get; }
    string Description { get; }
    ISet<string> TemplateNames { get; }
    ITemplate FindTemplate(string name);
    bool IsDefined(string name);
    void RegisterRenderer(Type attributeType, IAttributeRenderer renderer, bool recursive = true);
    void ImportTemplates(ITemplateGroup otherGroup);
    void RegisterModelAdaptor(Type attributeType, IModelAdaptor adaptor);
    IModelAdaptor GetModelAdaptor(Type attributeType);
    void Load();
    void Unload();
}
