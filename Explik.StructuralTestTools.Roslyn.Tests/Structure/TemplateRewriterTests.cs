﻿using Explik.StructuralTestTools;
using Explik.StructuralTestTools.TypeSystem;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using static TestTools_Tests.TestHelper;

namespace TestTools_Tests.Structure
{
    [TestClass]
    public class TemplateRewriterTests
    {
        // This feature has been disabled
        //[TestMethod("Visit replaces using directive")]
        public void Visit_ReplaceUsingDirective()
        {
            var root = SyntaxFactory.ParseSyntaxTree("using OriginalNamespace").GetRoot();
            var node = root.AllDescendantNodes<UsingDirectiveSyntax>().First();
            var syntaxResolver = Substitute.For<ICompileTimeDescriptionResolver>();
            var statementRewriter = Substitute.For<CSharpSyntaxRewriter>(true);
            statementRewriter.Visit(node).Returns(SyntaxFactory.ParseStatement("using TranslatedNamespace"));
            var rewriter = new TemplateRewriter(syntaxResolver, statementRewriter);

            var translatedNode = rewriter.Visit(node);

            var expectedSource = "using TranslatedNamespace";
            AssertAreEqualSource(expectedSource, translatedNode.ToString());
        }

        [TestMethod("Visit removes _Template from class name")]
        public void Visit_RemovesUnderscoreTemplateFromClassName()
        { 
            var root = SyntaxFactory.ParseSyntaxTree("class ClassName_Template { }").GetRoot();
            var node = root.AllDescendantNodes<ClassDeclarationSyntax>().First();
            var syntaxResolver = Substitute.For<ICompileTimeDescriptionResolver>();
            var statementRewriter = Substitute.For<CSharpSyntaxRewriter>(true);
            var rewriter = new TemplateRewriter(syntaxResolver, statementRewriter);

            var translatedNode = rewriter.Visit(node);

            var expectedSource = "class ClassName { }";
            AssertAreEqualSource(expectedSource, translatedNode.ToString());
        }

        [TestMethod("Visit removes _Template from constructor")]
        public void Visit_RemovesUnderscoreTemplateFromConstructor()
        {
            var source = "class ClassName_Template { ClassName_Template() { } }";
            var root = SyntaxFactory.ParseSyntaxTree(source).GetRoot();
            var node = root.AllDescendantNodes<ClassDeclarationSyntax>().First();
            var syntaxResolver = Substitute.For<ICompileTimeDescriptionResolver>();
            var statementRewriter = Substitute.For<CSharpSyntaxRewriter>(true);
            var rewriter = new TemplateRewriter(syntaxResolver, statementRewriter);

            var translatedNode = rewriter.Visit(node);

            var expectedSource = "class ClassName { ClassName() { } }";
            AssertAreEqualSource(expectedSource, translatedNode.ToString());
        }

        [TestMethod("Visit does not modify non-templated attributes")]
        public void Visit_DoesNotModifyNonTemplatedAttributes()
        {
            var source = "[TestClass] class ClassName { }";
            var root = SyntaxFactory.ParseSyntaxTree(source).GetRoot();
            var node1 = root.AllDescendantNodes<ClassDeclarationSyntax>().First();
            var node2 = root.AllDescendantNodes<AttributeSyntax>().First();
            var syntaxResolver = Substitute.For<ICompileTimeDescriptionResolver>();
            syntaxResolver.IsTemplatedAttribute(node2).Returns(false);
            var statementRewriter = Substitute.For<CSharpSyntaxRewriter>(true);
            var rewriter = new TemplateRewriter(syntaxResolver, statementRewriter);

            var translatedNode = rewriter.Visit(node1);

            AssertAreEqualSource(source, translatedNode.ToString());
        }

        [TestMethod("Visit replace name of templated attributes on class")]
        public void Visit_ReplacesNameOfTemplatedAttributesOnClass()
        {
            var source = "[Namespace.TemplatedTestClass] class ClassName { }";
            var root = SyntaxFactory.ParseSyntaxTree(source).GetRoot();
            var node1 = root.AllDescendantNodes<ClassDeclarationSyntax>().First();
            var node2 = root.AllDescendantNodes<AttributeSyntax>().First();
            
            var syntaxResolver = Substitute.For<ICompileTimeDescriptionResolver>();
            syntaxResolver.IsTemplatedAttribute(node2).Returns(true);
            syntaxResolver.GetAssociatedAttributeType(node2).Returns("Namespace.TestClass");
            var statementRewriter = Substitute.For<CSharpSyntaxRewriter>(true);
            var rewriter = new TemplateRewriter(syntaxResolver, statementRewriter);

            var translatedNode = rewriter.Visit(root);

            var expectedSource = "[Namespace.TestClass] class ClassName { }";
            AssertAreEqualSource(expectedSource, translatedNode.ToString());
        }

        [TestMethod("Visit replace name of templated attributes on class with using directive")]
        public void Visit_ReplacesNameOfTemplatedAttributesOnClassWithUsingDirective()
        {
            var source = "using Namespace; [TemplatedTestClass] class ClassName { }";
            var root = SyntaxFactory.ParseSyntaxTree(source).GetRoot();
            var node1 = root.AllDescendantNodes<UsingDirectiveSyntax>().First();
            var node2 = root.AllDescendantNodes<AttributeSyntax>().First();

            var syntaxResolver = Substitute.For<ICompileTimeDescriptionResolver>();
            syntaxResolver.GetNamespaceDescription(node1).Returns(new RuntimeNamespaceDescription("Namespace"));
            syntaxResolver.IsTemplatedAttribute(node2).Returns(true);
            syntaxResolver.GetAssociatedAttributeType(node2).Returns("Namespace.TestClass");
            var statementRewriter = Substitute.For<CSharpSyntaxRewriter>(true);
            var rewriter = new TemplateRewriter(syntaxResolver, statementRewriter);

            var translatedNode = rewriter.Visit(root);

            var expectedSource = "using Namespace; [TestClass] class ClassName { }";
            AssertAreEqualSource(expectedSource, translatedNode.ToString());
        }

        [TestMethod("Visit replaces name of templated attributes on method")]
        public void Visit_ReplacesNameOfTemplatedAttributeOnMethod()
        {
            var source = "[Namespace.TemplatedTestClass] class ClassName { [Namespace.TemplatedTestMethod] void MethodName() {} }";
            var root = SyntaxFactory.ParseSyntaxTree(source).GetRoot();
            var node1 = root.AllDescendantNodes<AttributeSyntax>().ElementAt(0);
            var node2 = root.AllDescendantNodes<AttributeSyntax>().ElementAt(1);
            var node3 = root.AllDescendantNodes<MethodDeclarationSyntax>().First();

            var syntaxResolver = Substitute.For<ICompileTimeDescriptionResolver>();
            syntaxResolver.IsTemplatedAttribute(node1).Returns(true);
            syntaxResolver.GetAssociatedAttributeType(node1).Returns("Namespace.TestClass");
            syntaxResolver.IsTemplatedAttribute(node2).Returns(true);
            syntaxResolver.GetAssociatedAttributeType(node2).Returns("Namespace.TestMethod");
            syntaxResolver.HasTemplatedAttribute(node3).Returns(true);
            var statementRewriter = Substitute.For<CSharpSyntaxRewriter>(true);
            statementRewriter.Visit(node3.Body).Returns(node3.Body);
            var rewriter = new TemplateRewriter(syntaxResolver, statementRewriter);

            var translatedNode = rewriter.Visit(root);

            var expectedSource = "[Namespace.TestClass] class ClassName { [Namespace.TestMethod] void MethodName() {} }";
            AssertAreEqualSource(expectedSource, translatedNode.ToString());
        }

        [TestMethod("Visit replaces name of templated attributes on method with using directive")]
        public void Visit_ReplacesNameOfTemplatedAttributeOnMethodWithUsingDirective()
        {
            var source = "using Namespace; [TemplatedTestClass] class ClassName { [TemplatedTestMethod] void MethodName() {} }";
            var root = SyntaxFactory.ParseSyntaxTree(source).GetRoot();
            var node1 = root.AllDescendantNodes<UsingDirectiveSyntax>().First();
            var node2 = root.AllDescendantNodes<AttributeSyntax>().ElementAt(0);
            var node3 = root.AllDescendantNodes<AttributeSyntax>().ElementAt(1);
            var node4 = root.AllDescendantNodes<MethodDeclarationSyntax>().First();

            var syntaxResolver = Substitute.For<ICompileTimeDescriptionResolver>();
            syntaxResolver.GetNamespaceDescription(node1).Returns(new RuntimeNamespaceDescription("Namespace"));
            syntaxResolver.IsTemplatedAttribute(node2).Returns(true);
            syntaxResolver.GetAssociatedAttributeType(node2).Returns("Namespace.TestClass");
            syntaxResolver.IsTemplatedAttribute(node3).Returns(true);
            syntaxResolver.GetAssociatedAttributeType(node3).Returns("Namespace.TestMethod");
            syntaxResolver.HasTemplatedAttribute(node4).Returns(true);
            var statementRewriter = Substitute.For<CSharpSyntaxRewriter>(true);
            statementRewriter.Visit(node4.Body).Returns(node4.Body);
            var rewriter = new TemplateRewriter(syntaxResolver, statementRewriter);

            var translatedNode = rewriter.Visit(root);

            var expectedSource = "using Namespace; [TestClass] class ClassName { [TestMethod] void MethodName() {} }";
            AssertAreEqualSource(expectedSource, translatedNode.ToString());
        }


        [TestMethod("Visit does not modify method body without templated attribute")]
        public void Visit_DoesNotModifyMethodBodyWithoutTemplatedAttribute()
        {
            var source = "class ClassName { [TestMethod] void MethodName() {} }";
            var root = SyntaxFactory.ParseSyntaxTree(source).GetRoot();
            var node1 = root.AllDescendantNodes<MethodDeclarationSyntax>().First();
            var node2 = root.AllDescendantNodes<AttributeSyntax>().First();
            var syntaxResolver = Substitute.For<ICompileTimeDescriptionResolver>();
            syntaxResolver.IsTemplatedAttribute(node2).Returns(false);
            var statementRewriter = Substitute.For<CSharpSyntaxRewriter>(true);
            var rewriter = new TemplateRewriter(syntaxResolver, statementRewriter);

            var translatedNode = rewriter.Visit(node1);

            var expectedSource = "[TestMethod] void MethodName() {}";
            AssertAreEqualSource(expectedSource, translatedNode.ToString());
        }

        [TestMethod("Visit modifies body of method with templated attributes")]
        public void Visit_ModifiesBodyOfMethodWithTemplatedAttributes()
        {
            var source = string.Join(
                Environment.NewLine,
                "class ClassName {",
                "  [TemplatedTestMethod]",
                "  void MethodName() {",
                "    Console.WriteLine(\"original line 1\");",
                "    ",
                "    Console.WriteLine(\"original line 2\");",
                "  }",
                "}");
            var root = SyntaxFactory.ParseSyntaxTree(source).GetRoot();
            var node1 = root.AllDescendantNodes<MethodDeclarationSyntax>().First();
            var node2 = root.AllDescendantNodes<AttributeSyntax>().First();
            var node3 = root.AllDescendantNodes<BlockSyntax>().First();

            var replacedSource = string.Join(
                Environment.NewLine,
                "  {",
                "    Console.WriteLine(\"translated line 1\");",
                "    ",
                "    Console.WriteLine(\"translated line 2\");",
                "  }");
            var syntaxResolver = Substitute.For<ICompileTimeDescriptionResolver>();
            syntaxResolver.HasTemplatedAttribute(node1).Returns(true);
            syntaxResolver.IsTemplatedAttribute(node2).Returns(true);
            syntaxResolver.GetAssociatedAttributeType(node2).Returns("TestMethod");
            var statementRewriter = Substitute.For<CSharpSyntaxRewriter>(true);
            statementRewriter.Visit(node3).Returns(SyntaxFactory.ParseStatement(replacedSource));
            var rewriter = new TemplateRewriter(syntaxResolver, statementRewriter);

            var translatedNode = rewriter.Visit(node1);

            var expectedSource = string.Join(
                Environment.NewLine,
                "[TestMethod]",
                "  void MethodName() {",
                "    Console.WriteLine(\"translated line 1\");",
                "    ",
                "    Console.WriteLine(\"translated line 2\");",
                "  }");
            AssertAreEqualSource(expectedSource, translatedNode.ToString());
        }

        [TestMethod("Visit replace body of method with templated attributes on VerifierServiceException (single line 1)")]
        public void VisitReplaceBodyOfMethodWithTemplatedAttributesOnVerifyServiceExceptionSingleLine1()
        {
            var source = string.Join(
                Environment.NewLine,
                "class ClassName {",
                "  [TemplatedTestMethod]",
                "  void MethodName() { Assert.IsNull(Person.FirstName); }",
                "}");
            var root = SyntaxFactory.ParseSyntaxTree(source).GetRoot();
            var node1 = root.AllDescendantNodes<MethodDeclarationSyntax>().First();
            var node2 = root.AllDescendantNodes<AttributeSyntax>().First();
            var node3 = root.AllDescendantNodes<BlockSyntax>().First();

            var syntaxResolver = Substitute.For<ICompileTimeDescriptionResolver>();
            syntaxResolver.HasTemplatedAttribute(node1).Returns(true);
            syntaxResolver.IsTemplatedAttribute(node2).Returns(true);
            syntaxResolver.GetAssociatedAttributeType(node2).Returns("TestMethod");
            syntaxResolver.GetAssociatedExceptionType(node2).Returns("AssertFailedException");
            var statementRewriter = Substitute.For<CSharpSyntaxRewriter>(true);
            statementRewriter.Visit(node3).Returns(x => throw new VerifierServiceException("message for user"));
            var rewriter = new TemplateRewriter(syntaxResolver, statementRewriter);

            var translatedNode = rewriter.Visit(node1);

            var expectedSource = string.Join(
                Environment.NewLine,
                "[TestMethod]",
                "  void MethodName() {",
                "    // == Failed To Compile ==",
                "    // Assert.IsNull(Person.FirstName);",
                "    throw new AssertFailedException(\"message for user\");",
                "  }");
            AssertAreEqualSource(expectedSource, translatedNode.ToString());
        }

        [TestMethod("Visit replace body of method with templated attributes on VerifierServiceException (single line 2)")]
        public void VisitReplaceBodyOfMethodWithTemplatedAttributesOnVerifyServiceExceptionSingleLine2()
        {
            var source = string.Join(
                Environment.NewLine,
                "class ClassName {",
                "  [TemplatedTestMethod]",
                "  public void MethodName() { Assert.IsNull(Person.FirstName); }",
                "}");
            var root = SyntaxFactory.ParseSyntaxTree(source).GetRoot();
            var node1 = root.AllDescendantNodes<MethodDeclarationSyntax>().First();
            var node2 = root.AllDescendantNodes<AttributeSyntax>().First();
            var node3 = root.AllDescendantNodes<BlockSyntax>().First();

            var syntaxResolver = Substitute.For<ICompileTimeDescriptionResolver>();
            syntaxResolver.HasTemplatedAttribute(node1).Returns(true);
            syntaxResolver.IsTemplatedAttribute(node2).Returns(true);
            syntaxResolver.GetAssociatedAttributeType(node2).Returns("TestMethod");
            syntaxResolver.GetAssociatedExceptionType(node2).Returns("AssertFailedException");
            var statementRewriter = Substitute.For<CSharpSyntaxRewriter>(true);
            statementRewriter.Visit(node3).Returns(x => throw new VerifierServiceException("message for user"));
            var rewriter = new TemplateRewriter(syntaxResolver, statementRewriter);

            var translatedNode = rewriter.Visit(node1);

            var expectedSource = string.Join(
                Environment.NewLine,
                "[TestMethod]",
                "  public void MethodName() {",
                "    // == Failed To Compile ==",
                "    // Assert.IsNull(Person.FirstName);",
                "    throw new AssertFailedException(\"message for user\");",
                "  }");
            AssertAreEqualSource(expectedSource, translatedNode.ToString());
        }

        [TestMethod("Visit replace body of method with templated attributes on VerifierServiceException (multi-line statement 1)")]
        public void VisitReplaceBodyOfMethodWithTemplatedAttributesOnVerifyServiceExceptionMultilLineStatement1()
        {
            var source = string.Join(
                Environment.NewLine,
                "class ClassName {",
                "  [TemplatedTestMethod]",
                "  void MethodName() {",
                "    Console.WriteLine(",
                "      \"A long string\");",
                "  }",
                "}");
            var root = SyntaxFactory.ParseSyntaxTree(source).GetRoot();
            var node1 = root.AllDescendantNodes<MethodDeclarationSyntax>().First();
            var node2 = root.AllDescendantNodes<AttributeSyntax>().First();
            var node3 = root.AllDescendantNodes<BlockSyntax>().First();

            var syntaxResolver = Substitute.For<ICompileTimeDescriptionResolver>();
            syntaxResolver.HasTemplatedAttribute(node1).Returns(true);
            syntaxResolver.IsTemplatedAttribute(node2).Returns(true);
            syntaxResolver.GetAssociatedAttributeType(node2).Returns("TestMethod");
            syntaxResolver.GetAssociatedExceptionType(node2).Returns("AssertFailedException");
            var statementRewriter = Substitute.For<CSharpSyntaxRewriter>(true);
            statementRewriter.Visit(node3).Returns(x => throw new VerifierServiceException("message for user"));
            var rewriter = new TemplateRewriter(syntaxResolver, statementRewriter);

            var translatedNode = rewriter.Visit(node1);

            var expectedSource = string.Join(
                Environment.NewLine,
                "[TestMethod]",
                "  void MethodName() {",
                "    // == Failed To Compile ==",
                "    // Console.WriteLine(",
                "    //   \"A long string\");",
                "    throw new AssertFailedException(\"message for user\")",
                "  }");
            AssertAreEqualSource(expectedSource, translatedNode.ToString());
        }

        [TestMethod("Visit replace body of method with templated attributes on VerifierServiceException (multi-line statement 2)")]
        public void VisitReplaceBodyOfMethodWithTemplatedAttributesOnVerifyServiceExceptionMultilLineStatement2()
        {
            var source = string.Join(
                Environment.NewLine,
                "class ClassName {",
                "  [TemplatedTestMethod]",
                "  void MethodName() {",
                "    new Person() {",
                "      Age = 38",
                "    }",
                "  }",
                "}");
            var root = SyntaxFactory.ParseSyntaxTree(source).GetRoot();
            var node1 = root.AllDescendantNodes<MethodDeclarationSyntax>().First();
            var node2 = root.AllDescendantNodes<AttributeSyntax>().First();
            var node3 = root.AllDescendantNodes<BlockSyntax>().First();

            var syntaxResolver = Substitute.For<ICompileTimeDescriptionResolver>();
            syntaxResolver.HasTemplatedAttribute(node1).Returns(true);
            syntaxResolver.IsTemplatedAttribute(node2).Returns(true);
            syntaxResolver.GetAssociatedAttributeType(node2).Returns("TestMethod");
            syntaxResolver.GetAssociatedExceptionType(node2).Returns("AssertFailedException");
            var statementRewriter = Substitute.For<CSharpSyntaxRewriter>(true);
            statementRewriter.Visit(node3).Returns(x => throw new VerifierServiceException("message for user"));
            var rewriter = new TemplateRewriter(syntaxResolver, statementRewriter);

            var translatedNode = rewriter.Visit(node1);

            var expectedSource = string.Join(
                Environment.NewLine,
                "[TestMethod]",
                "  void MethodName() {",
                "    // == Failed To Compile ==",
                "    // new Person() {",
                "    //   Age = 38",
                "    // }",
                "    throw new AssertFailedException(\"message for user\");",
                "  }");
            AssertAreEqualSource(expectedSource, translatedNode.ToString());
        }

        [TestMethod("Visit replaces body of method with templated attributes on VerifierServiceException (multiple statements 1)")]
        public void Visit_ReplacesBodyOfMethodWithTemplatedAttributesOnVerifierServiceExceptionMultipleStatements1()
        {
            var source = string.Join(
                Environment.NewLine,
                "class ClassName {",
                "  [TemplatedTestMethod]",
                "  void MethodName() {",
                "    Console.WriteLine(\"original line 1\");",
                "    Console.WriteLine(\"original line 2\");",
                "  }",
                "}");
            var root = SyntaxFactory.ParseSyntaxTree(source).GetRoot();
            var node1 = root.AllDescendantNodes<MethodDeclarationSyntax>().First();
            var node2 = root.AllDescendantNodes<AttributeSyntax>().First();
            var node3 = root.AllDescendantNodes<BlockSyntax>().First();

            var syntaxResolver = Substitute.For<ICompileTimeDescriptionResolver>();
            syntaxResolver.HasTemplatedAttribute(node1).Returns(true);
            syntaxResolver.IsTemplatedAttribute(node2).Returns(true);
            syntaxResolver.GetAssociatedAttributeType(node2).Returns("TestMethod");
            syntaxResolver.GetAssociatedExceptionType(node2).Returns("AssertFailedException");
            var statementRewriter = Substitute.For<CSharpSyntaxRewriter>(true);
            statementRewriter.Visit(node3).Returns(x => throw new VerifierServiceException("message for user"));
            var rewriter = new TemplateRewriter(syntaxResolver, statementRewriter);

            var translatedNode = rewriter.Visit(node1);

            var expectedSource = string.Join(
                Environment.NewLine,
                "[TestMethod]",
                "  void MethodName() {",
                "    // == Failed To Compile ==",
                "    // Console.WriteLine(\"original line 1\");",
                "    // Console.WriteLine(\"original line 2\");",
                "    throw new AssertFailedException(\"message for user\");",
                "  }");
            AssertAreEqualSource(expectedSource, translatedNode.ToString());
        }

        [TestMethod("Visit replaces body of method with templated attributes on VerifierServiceException (multiple statements 2)")]
        public void Visit_ReplacesBodyOfMethodWithTemplatedAttributesOnVerifierServiceExceptionMultipleStatements2()
        {
            var source = string.Join(
                Environment.NewLine,
                "class ClassName {",
                "  [TemplatedTestMethod]",
                "  void MethodName() {",
                "    Console.WriteLine(\"original line 1\");",
                "    ",
                "    Console.WriteLine(\"original line 2\");",
                "  }",
                "}");
            var root = SyntaxFactory.ParseSyntaxTree(source).GetRoot();
            var node1 = root.AllDescendantNodes<MethodDeclarationSyntax>().First();
            var node2 = root.AllDescendantNodes<AttributeSyntax>().First();
            var node3 = root.AllDescendantNodes<BlockSyntax>().First();

            var syntaxResolver = Substitute.For<ICompileTimeDescriptionResolver>();
            syntaxResolver.HasTemplatedAttribute(node1).Returns(true);
            syntaxResolver.IsTemplatedAttribute(node2).Returns(true);
            syntaxResolver.GetAssociatedAttributeType(node2).Returns("TestMethod");
            syntaxResolver.GetAssociatedExceptionType(node2).Returns("AssertFailedException");
            var statementRewriter = Substitute.For<CSharpSyntaxRewriter>(true);
            statementRewriter.Visit(node3).Returns(x => throw new VerifierServiceException("message for user"));
            var rewriter = new TemplateRewriter(syntaxResolver, statementRewriter);

            var translatedNode = rewriter.Visit(node1);

            var expectedSource = string.Join(
                Environment.NewLine,
                "[TestMethod]",
                "  void MethodName() {",
                "    // == Failed To Compile ==",
                "    // Console.WriteLine(\"original line 1\");",
                "    ",
                "    // Console.WriteLine(\"original line 2\");",
                "    throw new AssertFailedException(\"message for user\");",
                "  }");
            AssertAreEqualSource(expectedSource, translatedNode.ToString());
        }

        [TestMethod("Visit replaces body of method with templated attributes on VerifierServiceException (multiple statements 3)")]
        public void Visit_ReplacesBodyOfMethodWithTemplatedAttributesOnVerifierServiceExceptionMultipleStatements3()
        {
            var source = string.Join(
                Environment.NewLine,
                "class ClassName {",
                "  [TemplatedTestMethod]",
                "  void MethodName() {",
                "    Console.WriteLine(\"original line 1\");",
                "Console.WriteLine(\"original line 2\");",
                "  }",
                "}");
            var root = SyntaxFactory.ParseSyntaxTree(source).GetRoot();
            var node1 = root.AllDescendantNodes<MethodDeclarationSyntax>().First();
            var node2 = root.AllDescendantNodes<AttributeSyntax>().First();
            var node3 = root.AllDescendantNodes<BlockSyntax>().First();

            var syntaxResolver = Substitute.For<ICompileTimeDescriptionResolver>();
            syntaxResolver.HasTemplatedAttribute(node1).Returns(true);
            syntaxResolver.IsTemplatedAttribute(node2).Returns(true);
            syntaxResolver.GetAssociatedAttributeType(node2).Returns("TestMethod");
            syntaxResolver.GetAssociatedExceptionType(node2).Returns("AssertFailedException");
            var statementRewriter = Substitute.For<CSharpSyntaxRewriter>(true);
            statementRewriter.Visit(node3).Returns(x => throw new VerifierServiceException("message for user"));
            var rewriter = new TemplateRewriter(syntaxResolver, statementRewriter);

            var translatedNode = rewriter.Visit(node1);

            var expectedSource = string.Join(
                Environment.NewLine,
                "[TestMethod]",
                "  void MethodName() {",
                "    // == Failed To Compile ==",
                "    // Console.WriteLine(\"original line 1\");",
                "    // Console.WriteLine(\"original line 2\");",
                "    throw new AssertFailedException(\"message for user\");",
                "  }");
            AssertAreEqualSource(expectedSource, translatedNode.ToString());
        }

        [TestMethod("Visit replace body of method with templated attributes on VerifierServiceException with using statement")]
        public void VisitReplaceBodyOfMethodWithTemplatedAttributesOnVerifyServiceExceptionWithUsingStatement()
        {
            var source = string.Join(
                Environment.NewLine,
                "using Namespace;",
                "class ClassName {",
                "  [TemplatedTestMethod]",
                "  void MethodName() { Assert.IsNull(Person.FirstName); }",
                "}");
            var root = SyntaxFactory.ParseSyntaxTree(source).GetRoot();
            var node1 = root.AllDescendantNodes<UsingDirectiveSyntax>().First();
            var node2 = root.AllDescendantNodes<MethodDeclarationSyntax>().First();
            var node3 = root.AllDescendantNodes<AttributeSyntax>().First();
            var node4 = root.AllDescendantNodes<BlockSyntax>().First();

            var syntaxResolver = Substitute.For<ICompileTimeDescriptionResolver>();
            syntaxResolver.GetNamespaceDescription(node1).Returns(new RuntimeNamespaceDescription("Namespace"));
            syntaxResolver.HasTemplatedAttribute(node2).Returns(true);
            syntaxResolver.IsTemplatedAttribute(node3).Returns(true);
            syntaxResolver.GetAssociatedAttributeType(node3).Returns("TestMethod");
            syntaxResolver.GetAssociatedExceptionType(node3).Returns("Namespace.AssertFailedException");
            var statementRewriter = Substitute.For<CSharpSyntaxRewriter>(true);
            statementRewriter.Visit(node4).Returns(x => throw new VerifierServiceException("message for user"));
            var rewriter = new TemplateRewriter(syntaxResolver, statementRewriter);

            var translatedNode = rewriter.Visit(node2);

            var expectedSource = string.Join(
                Environment.NewLine,
                "[TestMethod]",
                "  void MethodName() {",
                "    // == Failed To Compile ==",
                "    // Assert.IsNull(Person.FirstName);",
                "    throw new AssertFailedException(\"message for user\");",
                "  }");
            AssertAreEqualSource(expectedSource, translatedNode.ToString());
        }

    }
}