using System;
using System.Text;

namespace Antlr4.StringTemplate;

public interface ITemplateFactory {
    ITemplate CreateTemplate(string content, char delimiterStartChar = '<', char delimiterStopChar = '>');
    ITemplate CopyTemplate(ITemplate template);
    ITemplate CreateTemplateImplicit(string content, ITemplateGroup group);
    TemplateGroupBuilder CreateTemplateGroup();
    TemplateGroupBuilder CreateTemplateGroupDirectory(string dirName);
    TemplateGroupBuilder CreateRawGroupDirectory(string dirName);
    TemplateGroupBuilder CreateTemplateGroupFile(string fileName);
    TemplateGroupBuilder CreateTemplateGroupString(string text, string sourceName = null);
}
