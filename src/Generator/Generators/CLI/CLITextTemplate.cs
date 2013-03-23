﻿using System;
using System.Collections.Generic;
using Cxxi.Types;

namespace Cxxi.Generators.CLI
{
    public struct Include
    {
        public enum IncludeKind
        {
            Angled,
            Quoted
        }

        public string File;
        public IncludeKind Kind;

        public override string ToString()
        {
            return string.Format(Kind == IncludeKind.Angled ?
                "#include <{0}>" : "#include \"{0}\"", File);
        }
    }

    public abstract class CLITextTemplate : TextTemplate
    {
        protected const string DefaultIndent = "    ";
        protected const uint MaxIndent = 80;

        public ITypePrinter TypePrinter { get; set; }

        public ISet<Include> Includes;

        protected CLITextTemplate(Driver driver, TranslationUnit unit)
            : base(driver, unit)
        {
            TypePrinter = new CLITypePrinter(driver);
            Includes = new HashSet<Include>();
        }

        public static string SafeIdentifier(string proposedName)
        {
            return proposedName;
        }

        public string QualifiedIdentifier(Declaration decl)
        {
            if (Options.GenerateLibraryNamespace)
                return string.Format("{0}::{1}", Options.OutputNamespace, decl.QualifiedName);
            return string.Format("{0}", decl.QualifiedName);
        }

        public void GenerateStart()
        {
            if (Transform == null)
            {
                WriteLine("//----------------------------------------------------------------------------");
                WriteLine("// This is autogenerated code by cxxi-generator.");
                WriteLine("// Do not edit this file or all your changes will be lost after re-generation.");
                WriteLine("//----------------------------------------------------------------------------");

                if (FileExtension == "cpp")
                    WriteLine(@"#include ""../interop.h""          // marshalString");
            }
            else
            {
                Transform.GenerateStart(this);
            }
        }

        public void GenerateAfterNamespaces()
        {
            if (Transform != null)
                Transform.GenerateAfterNamespaces(this);
        }

        public void GenerateSummary(string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
                return;

            // Wrap the comment to the line width.
            var maxSize = (int)(MaxIndent - CurrentIndent.Count - "/// ".Length);
            var lines = StringHelpers.WordWrapLines(comment, maxSize);

            WriteLine("/// <summary>");
            foreach (string line in lines)
                WriteLine(string.Format("/// {0}", line.TrimEnd()));
            WriteLine("/// </summary>");
        }

        public void GenerateInlineSummary(string comment)
        {
            if (String.IsNullOrWhiteSpace(comment))
                return;
            WriteLine("/// <summary> {0} </summary>", comment);
        }

        public void GenerateMethodParameters(Method method)
        {
            for (var i = 0; i < method.Parameters.Count; ++i)
            {
                if (method.Conversion == MethodConversionKind.FunctionToInstanceMethod
                    && i == 0)
                    continue;

                var param = method.Parameters[i];
                Write("{0}", TypePrinter.VisitParameter(param));
                if (i < method.Parameters.Count - 1)
                    Write(", ");
            }
        }

        public string GenerateParametersList(List<Parameter> parameters)
        {
            var types = new List<string>();
            foreach (var param in parameters)
                types.Add(TypePrinter.VisitParameter(param));
            return string.Join(", ", types);
        }

        public static bool CheckIgnoreMethod(Class @class, Method method)
        {
            if (method.Ignore) return true;

            bool isEmptyCtor = method.IsConstructor && method.Parameters.Count == 0;

            if (@class.IsValueType && isEmptyCtor)
                return true;

            if (method.IsCopyConstructor || method.IsMoveConstructor)
                return true;

            if (method.IsDestructor)
                return true;

            if (method.OperatorKind == CXXOperatorKind.Equal)
                return true;

            if (method.Kind == CXXMethodKind.Conversion)
                return true;

            if (method.Access != AccessSpecifier.Public)
                return true;

            return false;
        }

        public static bool CheckIgnoreField(Class @class, Field field)
        {
            if (field.Ignore) return true;

            if (field.Access != AccessSpecifier.Public)
                return true;

            return false;
        }

        public static List<Parameter> GetEventParameters(Event @event)
        {
            var i = 0;
            var @params = new List<Parameter>();
            foreach (var type in @event.Parameters)
            {
                @params.Add(new Parameter()
                {
                    Name = string.Format("_{0}", i++),
                    QualifiedType = type
                });
            }
            return @params;
        }

        public abstract override string FileExtension { get; }

        public abstract override void Generate();
    }
}