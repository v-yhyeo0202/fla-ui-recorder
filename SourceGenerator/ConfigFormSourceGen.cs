using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SourceGenerator;

[Generator]
public class ConfigFormSourceGen : IIncrementalGenerator
{
    private static string className = null;
    private static ClassDeclarationSyntax classNode = null;
    private static SyntaxTree classSyntaxTree = null;

    private static void PostInit(IncrementalGeneratorPostInitializationContext context)
    {
        // Source codes to declare attribute for generator to identify the related classes for source generation
        StringBuilder sbSource = new();
        sbSource.Append(
            $$"""
            using System;
            using Microsoft.CodeAnalysis;
                
            namespace ConfigFormSourceGen;
            

            """);

        foreach (KeyValuePair<string, FormConfig> kvp in Global.dictFormConfig)
        {
            sbSource.Append(
                $$"""
                [AttributeUsage(AttributeTargets.Class), Embedded]
                public sealed class {{kvp.Key}}Attribute : Attribute {}


                """);
        }

        context.AddEmbeddedAttributeDefinition();
        context.AddSource("FormAttributeGen.cs", SourceText.From(sbSource.ToString(), Encoding.UTF8));

        return;
    }

    private static ClassDeclarationSyntax Transform(GeneratorAttributeSyntaxContext context, CancellationToken _)
    {
        string nodeName = ((ClassDeclarationSyntax)context.TargetNode).Identifier.Text;
        className = context.Attributes.First().AttributeClass.Name.Replace("Attribute", "");

        if (nodeName.Contains("Form"))
        {
            // Get Form class declaration syntax node
            return (ClassDeclarationSyntax)context.TargetNode;
        }
        else if (nodeName == className)
        {
            // Get item class declaration syntax node and syntax tree
            classNode = (ClassDeclarationSyntax)context.TargetNode;
            classSyntaxTree = context.TargetNode.SyntaxTree;
        }

        return null;
    }

    private static void ValidateConfigPropertyName()
    {
        HashSet<string> setPropertyName = new(classNode.Members.OfType<PropertyDeclarationSyntax>().Select(i => i.Identifier.Text));

        foreach (string k in Global.dictFormConfig[className].dictPropertyConfig.Keys)
        {
            if (!setPropertyName.Contains(k))
            {
                throw new Exception($"ValidateConfigPropertyName Global.dictFromConfig[\"{className}\"] contains incorrect property name");
            }
        }

        return;
    }

    private static void Execute(SourceProductionContext context, ClassDeclarationSyntax node)
    {
        if (node == null)
        {
            return;
        }

        try
        {
            ValidateConfigPropertyName();
            CSharpCompilation compile = CSharpCompilation.Create($"{className}FormCompile");
            SemanticModel classModel = compile.AddSyntaxTrees(classSyntaxTree).GetSemanticModel(classSyntaxTree);

            Dictionary<string, PropertyDeclarationSyntax> dictPropertyNode = classNode.Members.OfType<PropertyDeclarationSyntax>()
                .Where(i => Global.dictFormConfig[className].dictPropertyConfig.ContainsKey(i.Identifier.Text))
                .ToDictionary(j => j.Identifier.Text, j => j);
            List<string> listPropertyFullName = Global.dictFormConfig[className].dictPropertyConfig.Keys.ToList();
            List<string> listPropertyName = listPropertyFullName.Select(i => i.Split('.').Last()).ToList();

            string propertyName = null;
            FormType? formType = null;
            TypeSyntax typeNode = null;
            ITypeSymbol typeSymbol = null;
            Dictionary<string, FormType?> dictFormType = new();
            Dictionary<string, Type> dictPropertyType = new();

            for (int i = 0; i < listPropertyName.Count; i++)
            {
                propertyName = listPropertyFullName[i];
                formType = Global.dictFormConfig[className].dictPropertyConfig[propertyName].formType;

                if ((formType == null) && dictPropertyNode.ContainsKey(listPropertyName[i]))
                {
                    typeNode = dictPropertyNode[listPropertyName[i]].DescendantNodes()
                        .OfType<TypeSyntax>()
                        .First();
                    typeSymbol = classModel.GetTypeInfo(typeNode).Type;

                    if (typeSymbol != null)
                    {
                        if ((typeSymbol.SpecialType == SpecialType.System_String) ||
                            typeSymbol.ToDisplayString().Contains("string"))
                        {
                            formType = FormType.TextForm;
                        }
                        else if ((typeSymbol.SpecialType == SpecialType.System_Int16) ||
                            (typeSymbol.SpecialType == SpecialType.System_Int32) ||
                            (typeSymbol.SpecialType == SpecialType.System_Int64) ||
                            (typeSymbol.SpecialType == SpecialType.System_UInt16) ||
                            (typeSymbol.SpecialType == SpecialType.System_UInt32) ||
                            (typeSymbol.SpecialType == SpecialType.System_UInt64))
                        {
                            formType = FormType.IntForm;
                        }
                        else if ((typeSymbol.SpecialType == SpecialType.System_Decimal) ||
                            (typeSymbol.SpecialType == SpecialType.System_Double))
                        {
                            formType = FormType.DecimalForm;
                        }
                        else if ((typeSymbol.SpecialType == SpecialType.System_DateTime) ||
                            typeSymbol.ToDisplayString().Contains(nameof(DateTime)))
                        {
                            formType = FormType.DateForm;
                        }
                        else if (typeSymbol.SpecialType == SpecialType.System_Boolean)
                        {
                            formType = FormType.CheckBoxForm;
                        }
                    }
                }

                dictFormType[propertyName] = formType;
            }

            StringBuilder sbSource = new();
            sbSource.Append(
                $$"""
                using Microsoft.Win32;
                using Minerals.StringCases;
                using System.Collections.ObjectModel;
                using System.Diagnostics;
                using System.IO;
                using System.Text.Encodings.Web;
                using System.Text.Json;

                using {{Global.targetedProject}};
                using {{Global.targetedProject}}.Core;
                using Shared;

                namespace {{((FileScopedNamespaceDeclarationSyntax)node.Parent).Name}};

                public partial class {{className}}Form
                {
                    private Utility utility;
                    private string configPath;

                    public override void AddItem(object parameter)
                    {
                        listFormWrapper.Add(new FormWrapper());
                        {{className}} defaultInput = new();


                """);

            for (int i = 0; i < listPropertyName.Count; i++)
            {
                propertyName = listPropertyFullName[i];
                formType = dictFormType[propertyName];

                sbSource.Append(
                    $$"""
                            if(formConfig.dictPropertyConfig.Keys.Contains("{{propertyName}}"))
                            {
                                listFormWrapper.Last().listForm.Add(new {{formType.ToString()}}
                                {
                                    displayName = "{{Global.dictFormConfig[className].dictPropertyConfig[propertyName].displayName ?? listPropertyName[i]}}",
                                    labelWidth = {{(Global.dictFormConfig[className].dictPropertyConfig[propertyName].labelWidth != 0 ?
                                    Global.dictFormConfig[className].dictPropertyConfig[propertyName].labelWidth :
                                    Global.dictFormConfig[className].labelWidth)}},
                                    input = defaultInput.{{propertyName}},

                    """);

                if (formType == FormType.PathForm)
                {
                    sbSource.Append(
                        $$"""
                                        bFilePath = {{(Global.dictFormConfig[className].dictPropertyConfig[propertyName].bFilePath ? "true" : "false")}},
                                        bSave = {{(Global.dictFormConfig[className].dictPropertyConfig[propertyName].bSave ? "true" : "false")}},
                                        bPostSearchPath = {{(Global.dictFormConfig[className].dictPropertyConfig[propertyName].bPostSearchPath ? "true" : "false")}},
                                        formDependency = formDependency

                        """);
                }

                sbSource.Append(
                    $$"""
                                });


                    """);

                if ((formType == FormType.ComboForm) && (Global.dictFormConfig[className].dictPropertyConfig[propertyName].itemSelectCode != null))
                {
                    sbSource.Append(
                        $$"""
                                    if(formDependency != null)
                                    {
                                        formDependency.SetComboBoxItem((({{formType.ToString()}})listFormWrapper.Last().listForm.Last()).listItem, "{{propertyName}}");
                                    }

                        """);
                }

                sbSource.Append(
                    $$"""
                            }


                    """);
            }

            sbSource.Append(
                $$"""
                        return;
                    }

                    public List<{{className}}> GetInput()
                    {
                        List<{{className}}> listInput = Enumerable.Range(0, listFormWrapper.Count)
                            .Select(i => new {{className}}())
                            .ToList();
                
                        for(int i = 0; i < listFormWrapper.Count; i++)
                        {
                
                """);

            for (int i = 0; i < listPropertyFullName.Count; i++)
            {
                propertyName = listPropertyFullName[i];
                formType = dictFormType[propertyName];

                if (formType != null)
                {
                    sbSource.Append(
                        $$"""
                                    if(formConfig.dictPropertyConfig.Keys.Contains("{{propertyName}}"))
                                    {
                                        listInput[i].{{propertyName}} = (({{formType.ToString()}})listFormWrapper[i].listForm[{{i}}]).input;
                                    }


                        """);
                }
            }

            sbSource.Append(
                $$"""
                        }
                
                        return listInput;
                    }

                    public void SetInput(List<{{className}}> listInput)
                    {
                        listFormWrapper = new ObservableCollection<FormWrapper>();

                        for(int i = 0; i < listInput.Count; i++)
                        {
                            AddItem(null);


                """);

            for (int i = 0; i < listPropertyFullName.Count; i++)
            {
                propertyName = listPropertyFullName[i];
                formType = dictFormType[propertyName];

                if (formType != null)
                {
                    sbSource.Append(
                        $$"""
                                    (({{formType.ToString()}})listFormWrapper[i].listForm[{{i}}]).input = listInput[i].{{propertyName}};

                        """);
                }
            }

            sbSource.Append(
                $$"""
                        }

                        return;
                    }

                    public override async Task InsertAsync(object parameter) {}

                    public override async Task ApplyAsync(object parameter)
                    {
                        List<{{className}}> listInput = GetInput();


                """);

            if (Global.dictFormConfig[className].bSpecifyConfigPath)
            {
                sbSource.Append(
                    $$"""
                            SaveFileDialog saveFileDialog = new();
                            bool? status = saveFileDialog.ShowDialog();

                            if ((status != null) && (bool)status)
                            {
                                configPath = saveFileDialog.FileName;
                            }
                            else
                            {
                                return;
                            }
                    """);
            }

            sbSource.Append(
                $$"""
                        await File.WriteAllTextAsync(
                            configPath,
                            JsonSerializer.Serialize(
                                listInput,
                                new JsonSerializerOptions
                                {
                                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                                    WriteIndented = true
                                }
                            )
                        );

                        utility.ShowSuccessMessage();

                        if(formDependency != null)
                        {
                            formDependency.PostApply(listInput);
                        }
                        
                        return;
                    }

                    public override void Reset(object parameter)
                    {
                        SetInput(new List<{{className}}> { new {{className}}() });

                        return;
                    }

                """);

            List<string> listAsyncText = new() { "", "async" };
            List<string> listAwaitText = new() { "", "await" };
            List<string> listReturnTypeText = new() { "void", "Task" };

            string asyncText;
            string capitalizedAsyncText;

            for (int i = 0; i < listAsyncText.Count; i++)
            {
                asyncText = listAsyncText[i];
                capitalizedAsyncText = string.IsNullOrEmpty(asyncText) ? null : char.ToUpper(asyncText.First()) + asyncText.Substring(1);

                sbSource.Append(
                    $$"""
                        public {{asyncText}} {{listReturnTypeText[i]}} LoadInput{{capitalizedAsyncText}}(string _configPath)
                        {
                            configPath = _configPath;
                            List<{{className}}> listInput = JsonSerializer.Deserialize<List<{{className}}>>({{listAwaitText[i]}} File.ReadAllText{{capitalizedAsyncText}}(configPath));
                            SetInput(listInput);
                
                            return;
                        }
                    """);
            }

            sbSource.Append(
                $$"""
                    public {{className}}Form(FormConfig localFormConfig = null, IFormDependency _formDependency = null) :
                        base(Global.dictFormConfig["{{className}}"], localFormConfig, _formDependency)
                    {
                        utility = new Utility();
                        configPath = Path.Join(Directory.GetCurrentDirectory(), "{{char.ToLower(className[0]) + className.Substring(1)}}.json");
                        
                        if(File.Exists(configPath))
                        {
                            LoadInput(configPath);
                        }
                        else
                        {
                            AddItem(null);
                        }
                        
                        return;
                    }

                    public string GetConfigPath()
                    {
                        return configPath;
                    }
                }
                """);

            context.AddSource($"{className}Form.cs", SourceText.From(sbSource.ToString(), Encoding.UTF8));
        }
        catch (Exception e)
        {
            SourceGenLogger.Log($"Exception caught in ConfigFormSourceGen.Execute {e.TargetSite}\n{e.Message}\n");
        }

        return;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        try
        {
            context.RegisterPostInitializationOutput(PostInit);

            foreach (KeyValuePair<string, FormConfig> kvp in Global.dictFormConfig)
            {
                IncrementalValuesProvider<ClassDeclarationSyntax> provider = context.SyntaxProvider.ForAttributeWithMetadataName(
                    fullyQualifiedMetadataName: $"ConfigFormSourceGen.{kvp.Key}Attribute",
                    predicate: static (node, _) => node is ClassDeclarationSyntax,
                    transform: Transform);
                context.RegisterSourceOutput(provider, Execute);
            }
        }
        catch (Exception e)
        {
            SourceGenLogger.Log($"Exception caught in ConfigFormSourceGen.Initialize {e.TargetSite}\n{e.Message}\n");
        }

        return;
    }
}