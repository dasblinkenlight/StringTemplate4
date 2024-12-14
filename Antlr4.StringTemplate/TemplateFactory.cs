namespace Antlr4.StringTemplate;

public class TemplateFactory : ITemplateFactory {

    public ITemplate CreateTemplate(string content, char delimiterStartChar = '<', char delimiterStopChar = '>') =>
        new Template(content, delimiterStartChar, delimiterStopChar);

    public ITemplate CreateTemplateImplicit(string content, ITemplateGroup group) =>
        new Template(content, group as TemplateGroup);

    public TemplateGroupBuilder CreateTemplateGroup() => TemplateGroupBuilder.ForGroup();

    public TemplateGroupBuilder CreateTemplateGroupDirectory(string dirName) =>
        TemplateGroupBuilder.ForGroupDirectory(dirName);

    public TemplateGroupBuilder CreateRawGroupDirectory(string dirName) =>
        TemplateGroupBuilder.ForRawGroupDirectory(dirName);

    public TemplateGroupBuilder CreateTemplateGroupFile(string fileName) =>
        TemplateGroupBuilder.ForGroupFile(fileName);

    public TemplateGroupBuilder CreateTemplateGroupString(string text, string sourceName) =>
        TemplateGroupBuilder.ForString(text, sourceName);

}
