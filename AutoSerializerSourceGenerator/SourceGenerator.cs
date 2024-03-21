﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SourceGenerator
{
    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        public SourceGenerator()
        {
//#if DEBUG
//            if (!Debugger.IsAttached)
//            {
//                Debugger.Launch();
//            }
//#endif
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var compilation = context.Compilation;

            var types = GetAllSerializableTypes(compilation).Where(t => !t.IsGenericType).ToList();
            var src = new StringBuilder();
            AddAutoGeneratedHeader(src);

            foreach (var type in types)
            {
                AddNamespaceImport(src, type.ContainingNamespace.ToDisplayString());
            }

            AddSerializerClass(src, types,compilation);

            context.AddSource("SerializerProvider.g.cs", src.ToString());
        }

        private static IEnumerable<INamedTypeSymbol> GetAllSerializableTypes(Compilation compilation)
        {
            return GetAllTypesByMetadataName(compilation)
                .Where(t => t.TypeKind == TypeKind.Class && t.AllInterfaces.Any(i => i.Name == "ISerializable"));
        }

        private static void AddAutoGeneratedHeader(StringBuilder src)
        {
            src.AppendLine("// <auto-generated/> ");
            src.AppendLine("using System;");
            src.AppendLine("using AutoSerializer;");
        }

        private static void AddNamespaceImport(StringBuilder src, string namespaceName)
        {
            src.AppendLine($"using {namespaceName};");
        }

        private static void AddSerializerClass(StringBuilder src, List<INamedTypeSymbol> types,Compilation compilation)
        {
            src.AppendLine($"namespace {compilation.AssemblyName}");
            src.AppendLine("{");
            src.AppendLine("    public class SerializerProvider:ISerializer");
            src.AppendLine("    {");
            src.AppendLine("        public void SerializePartial<T>(ISerializable<T> obj, BinaryWriter writer) where T : new()");
            src.AppendLine("        {");
            src.AppendLine("            switch (obj)");
            src.AppendLine("            {");

            AddSerializeCases(src, types);

            src.AppendLine("            }");
            src.AppendLine("        }");
            src.AppendLine();
            src.AppendLine("        public void DeserializePartial(BinaryReader reader, ref object result)");
            src.AppendLine("        {");
            src.AppendLine("            switch (result)");
            src.AppendLine("            {");

            AddDeserializeCases(src, types);

            src.AppendLine("            }");
            src.AppendLine("        }");
            src.AppendLine("    }");
            src.AppendLine("}");
        }

        private static void AddSerializeCases(StringBuilder src, List<INamedTypeSymbol> types)
        {
            foreach (var type in types)
            {
                src.AppendLine($@"case {type.Name}:");
                var publicProperties = GetPublicProperties(type);
                foreach (var prop in publicProperties)
                {
                    AddSerializeProperty(src, type, prop);
                }
                src.AppendLine($@"break;");
            }
        }

        private static void AddDeserializeCases(StringBuilder src, List<INamedTypeSymbol> types)
        {
            foreach (var type in types)
            {
                src.AppendLine($@"case {type.Name}:");
                var publicProperties = GetPublicProperties(type);
                foreach (var prop in publicProperties)
                {
                    AddDeserializeProperty(src, type, prop);
                }
                src.AppendLine($@"break;");
            }
        }

        private static IEnumerable<IPropertySymbol> GetPublicProperties(INamedTypeSymbol type)
        {
            return type.GetMembers()
                .Where(m => m.Kind == SymbolKind.Property && m.DeclaredAccessibility == Accessibility.Public)
                .OfType<IPropertySymbol>()
                .ToList();
        }

        private static void AddSerializeProperty(StringBuilder src, INamedTypeSymbol type, IPropertySymbol prop)
        {
            if (prop.Type.AllInterfaces.Any(i => i.Name == "ISerializable") && prop.Type.IsReferenceType)
            {
                src.AppendLine($@"(({prop.Type.ToDisplayString()})(({type.Name})obj).{prop.Name}).Serialize(writer);");
            }
            else if (prop.Type.TypeKind == TypeKind.Enum)
            {
                src.AppendLine($@"writer.Write((int)(({type.Name})obj).{prop.Name});");
            }
            else
            {
                src.AppendLine($@"writer.Write((({type.Name})obj).{prop.Name});");
            }
        }

        private static void AddDeserializeProperty(StringBuilder src, INamedTypeSymbol type, IPropertySymbol prop)
        {
            if (prop.Type.TypeKind == TypeKind.Array && ((IArrayTypeSymbol)prop.Type).ElementType.AllInterfaces.Any(i => i.Name == "ISerializable"))
            {
                src.AppendLine($@"(({type.Name})result).{prop.Name} = ({prop.Type.ToDisplayString()})reader.Read<{((IArrayTypeSymbol)prop.Type).ElementType.ToDisplayString()}>();");
            }

            else if (prop.Type.AllInterfaces.Any(i => i.Name == "ISerializable") && prop.Type.IsReferenceType)
            {
                src.AppendLine($@"(({type.Name})result).{prop.Name} = {prop.Type.Name}.Deserialize(reader);");
            }
            else if (prop.Type.TypeKind == TypeKind.Enum)
            {
                src.AppendLine($@"(({type.Name})result).{prop.Name} = ({prop.Type.ToDisplayString()})reader.Read(typeof(int));");
            }
            else
            {
                src.AppendLine($@"(({type.Name})result).{prop.Name} = ({prop.Type.ToDisplayString()})reader.Read(typeof({prop.Type.ToDisplayString()}));");
            }
        }

        private static IEnumerable<INamedTypeSymbol> GetAllTypesByMetadataName(Compilation compilation)
        {
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                foreach (var typeDeclaration in syntaxTree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    if (semanticModel.GetDeclaredSymbol(typeDeclaration) is INamedTypeSymbol typeSymbol)
                    {
                        yield return typeSymbol;
                    }
                }
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // context.RegisterForSyntaxNotifications(() => new RecordSyntaxReceiver());

            // Initialization logic can be added here if needed.
        }
    }
}
