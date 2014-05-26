﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class DeclarationExpressionsTests : CompilingTestBase
    {
        [Fact]
        public void DisabledByDefault()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        System.Console.WriteLine(int i = 3);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,34): error CS1525: Invalid expression term 'int'
    //         System.Console.WriteLine(int i = 3);
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(6, 34),
    // (6,38): error CS1003: Syntax error, ',' expected
    //         System.Console.WriteLine(int i = 3);
    Diagnostic(ErrorCode.ERR_SyntaxError, "i").WithArguments(",", "").WithLocation(6, 38),
    // (6,38): error CS0103: The name 'i' does not exist in the current context
    //         System.Console.WriteLine(int i = 3);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "i").WithArguments("i").WithLocation(6, 38)
               );
        }

        [Fact]
        public void Simple_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(int y = 123);
        System.Console.WriteLine(y);
    }

    static void Test(int x)
    {
        System.Console.WriteLine(x);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));
            
            CompileAndVerify(compilation, expectedOutput: @"123
123").VerifyDiagnostics();
        }

        [Fact]
        public void Simple_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        i = 1;
        System.Console.WriteLine(int i = 3);

        System.Console.WriteLine(int j = 3);
        System.Console.WriteLine(int j = 4);

        System.Console.WriteLine(int k = 3);
        int k = 4;

        int l = 4;
        System.Console.WriteLine(int l = 3);

        int m = 5;
        {
            System.Console.WriteLine(int m = 4);
        }

        System.Console.WriteLine(m);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,9): error CS0841: Cannot use local variable 'i' before it is declared
    //         i = 1;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "i").WithArguments("i").WithLocation(6, 9),
    // (10,38): error CS0128: A local variable named 'j' is already defined in this scope
    //         System.Console.WriteLine(int j = 4);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "j").WithArguments("j").WithLocation(10, 38),
    // (13,13): error CS0128: A local variable named 'k' is already defined in this scope
    //         int k = 4;
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "k").WithArguments("k").WithLocation(13, 13),
    // (16,38): error CS0128: A local variable named 'l' is already defined in this scope
    //         System.Console.WriteLine(int l = 3);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "l").WithArguments("l").WithLocation(16, 38),
    // (20,42): error CS0136: A local or parameter named 'm' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int m = 4);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "m").WithArguments("m").WithLocation(20, 42),
    // (13,13): warning CS0219: The variable 'k' is assigned but its value is never used
    //         int k = 4;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "k").WithArguments("k").WithLocation(13, 13),
    // (15,13): warning CS0219: The variable 'l' is assigned but its value is never used
    //         int l = 4;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "l").WithArguments("l").WithLocation(15, 13)
                );
        }

        [Fact]
        public void Simple_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        int x = (int y = 123);
        System.Console.WriteLine(x);
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"123
123").VerifyDiagnostics();
        }

        [Fact]
        public void Simple_04()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        int x = (int y) = 123;
        System.Console.WriteLine(x);
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"123
123").VerifyDiagnostics();
        }

        [Fact]
        public void ERR_DeclarationExpressionOutsideOfAMethodBody_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }

    static void Test1(int p = (int a = 3) + a)
    {
    }

    static void Test2(int p1 = int b = 3, int p2 = b)
    {
    }

    static void Test3(int p1 = c, int p2 = int c = 3)
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (8,32): error CS8047: A declaration expression is not permitted in this context.
    //     static void Test1(int p = (int a = 3) + a)
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "int a = 3").WithLocation(8, 32),
    // (8,31): error CS1736: Default parameter value for 'p' must be a compile-time constant
    //     static void Test1(int p = (int a = 3) + a)
    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "(int a = 3) + a").WithArguments("p").WithLocation(8, 31),
    // (12,32): error CS8047: A declaration expression is not permitted in this context.
    //     static void Test2(int p1 = int b = 3, int p2 = b)
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "int b = 3").WithLocation(12, 32),
    // (12,32): error CS1736: Default parameter value for 'p1' must be a compile-time constant
    //     static void Test2(int p1 = int b = 3, int p2 = b)
    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "int b = 3").WithArguments("p1").WithLocation(12, 32),
    // (12,52): error CS0103: The name 'b' does not exist in the current context
    //     static void Test2(int p1 = int b = 3, int p2 = b)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(12, 52),
    // (12,47): error CS1750: A value of type '?' cannot be used as a default parameter because there are no standard conversions to type 'int'
    //     static void Test2(int p1 = int b = 3, int p2 = b)
    Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "p2").WithArguments("?", "int").WithLocation(12, 47),
    // (16,32): error CS0103: The name 'c' does not exist in the current context
    //     static void Test3(int p1 = c, int p2 = int c = 3)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "c").WithArguments("c").WithLocation(16, 32),
    // (16,27): error CS1750: A value of type '?' cannot be used as a default parameter because there are no standard conversions to type 'int'
    //     static void Test3(int p1 = c, int p2 = int c = 3)
    Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "p1").WithArguments("?", "int").WithLocation(16, 27),
    // (16,44): error CS8047: A declaration expression is not permitted in this context.
    //     static void Test3(int p1 = c, int p2 = int c = 3)
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "int c = 3").WithLocation(16, 44),
    // (16,44): error CS1736: Default parameter value for 'p2' must be a compile-time constant
    //     static void Test3(int p1 = c, int p2 = int c = 3)
    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "int c = 3").WithArguments("p2").WithLocation(16, 44)
                );
        }

        [Fact]
        public void ERR_DeclarationExpressionOutsideOfAMethodBody_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }

    [TestAttribute((int a = 3) + a)]
    void Test1(){}

    [TestAttribute(int b = 3, Y = b)]
    void Test2(){}

    [TestAttribute(Y = (int c = 3) + c)]
    void Test3(){}

    [TestAttribute(Y = int d = 3, Z = d)]
    void Test4(){}

    [TestAttribute(e, Y = int e = 3)]
    void Test5(){}

    [TestAttribute(Y = f, Z = int f = 3)]
    void Test6(){}
}

class TestAttribute : System.Attribute
{
    public TestAttribute() { }
    public TestAttribute(int a) { }
    public TestAttribute(int a, int b) { }

    public int Y { get; set; }
    public int Z { get; set; }
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (8,21): error CS8047: A declaration expression is not permitted in this context.
    //     [TestAttribute((int a = 3) + a)]
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "int a = 3").WithLocation(8, 21),
    // (8,20): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
    //     [TestAttribute((int a = 3) + a)]
    Diagnostic(ErrorCode.ERR_BadAttributeArgument, "(int a = 3) + a").WithLocation(8, 20),
    // (11,20): error CS8047: A declaration expression is not permitted in this context.
    //     [TestAttribute(int b = 3, Y = b)]
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "int b = 3").WithLocation(11, 20),
    // (11,35): error CS0103: The name 'b' does not exist in the current context
    //     [TestAttribute(int b = 3, Y = b)]
    Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(11, 35),
    // (11,20): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
    //     [TestAttribute(int b = 3, Y = b)]
    Diagnostic(ErrorCode.ERR_BadAttributeArgument, "int b = 3").WithLocation(11, 20),
    // (14,25): error CS8047: A declaration expression is not permitted in this context.
    //     [TestAttribute(Y = (int c = 3) + c)]
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "int c = 3").WithLocation(14, 25),
    // (14,24): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
    //     [TestAttribute(Y = (int c = 3) + c)]
    Diagnostic(ErrorCode.ERR_BadAttributeArgument, "(int c = 3) + c").WithLocation(14, 24),
    // (17,24): error CS8047: A declaration expression is not permitted in this context.
    //     [TestAttribute(Y = int d = 3, Z = d)]
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "int d = 3").WithLocation(17, 24),
    // (17,39): error CS0103: The name 'd' does not exist in the current context
    //     [TestAttribute(Y = int d = 3, Z = d)]
    Diagnostic(ErrorCode.ERR_NameNotInContext, "d").WithArguments("d").WithLocation(17, 39),
    // (17,24): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
    //     [TestAttribute(Y = int d = 3, Z = d)]
    Diagnostic(ErrorCode.ERR_BadAttributeArgument, "int d = 3").WithLocation(17, 24),
    // (20,20): error CS0103: The name 'e' does not exist in the current context
    //     [TestAttribute(e, Y = int e = 3)]
    Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(20, 20),
    // (20,27): error CS8047: A declaration expression is not permitted in this context.
    //     [TestAttribute(e, Y = int e = 3)]
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "int e = 3").WithLocation(20, 27),
    // (23,24): error CS0103: The name 'f' does not exist in the current context
    //     [TestAttribute(Y = f, Z = int f = 3)]
    Diagnostic(ErrorCode.ERR_NameNotInContext, "f").WithArguments("f").WithLocation(23, 24),
    // (23,31): error CS8047: A declaration expression is not permitted in this context.
    //     [TestAttribute(Y = f, Z = int f = 3)]
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "int f = 3").WithLocation(23, 31),
    // (23,31): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
    //     [TestAttribute(Y = f, Z = int f = 3)]
    Diagnostic(ErrorCode.ERR_BadAttributeArgument, "int f = 3").WithLocation(23, 31)
                );
        }

        [Fact]
        public void SimpleVar_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(var y = 123);
        System.Console.WriteLine(y);
        PrintType(y);
    }

    static void Test(int x)
    {
        System.Console.WriteLine(x);
    }

    static void PrintType<T>(T x)
    {
        System.Console.WriteLine(typeof(T));
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"123
123
System.Int32").VerifyDiagnostics();
        }

        [Fact]
        public void SimpleVar_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(var y);
    }

    static void Test(int x)
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,18): error CS0818: Implicitly-typed variables must be initialized
    //         Test(var y);
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 18),
    // (6,14): error CS0165: Use of unassigned local variable 'y'
    //         Test(var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 14)
                );
        }

        [Fact]
        public void SimpleVar_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var x = 1 + x;
        Test(var y = 1 + y);
    }

    static void Test(int x)
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,21): error CS0841: Cannot use local variable 'x' before it is declared
    //         var x = 1 + x;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x").WithArguments("x").WithLocation(6, 21),
    // (7,26): error CS0841: Cannot use local variable 'y' before it is declared
    //         Test(var y = 1 + y);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y").WithArguments("y").WithLocation(7, 26),
    // (6,21): error CS0165: Use of unassigned local variable 'x'
    //         var x = 1 + x;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(6, 21),
    // (7,26): error CS0165: Use of unassigned local variable 'y'
    //         Test(var y = 1 + y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(7, 26)
                );
        }

        [Fact]
        public void For_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        for (int i = (int)(double j = 0); (j = i) < 2; i=(int)j+1)
        {
            System.Console.WriteLine(j);
        }

        for (int i = (int)(double j = 10); (j = i) < 12; i=(int)j+1)
            System.Console.WriteLine(j + (int k = 5 + i) + k);

        int ii;
        for (ii = (int)(double j = 10); (j = ii) < 12; ii=(int)j+1)
            System.Console.WriteLine(j + (int k = 5 + ii) + k);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"0
1
40
43
40
43").VerifyDiagnostics();
        }

        [Fact]
        public void For_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        for (int i = 0; i < 2; i = (int j = i + 1) + j)
        {
            System.Console.WriteLine(j);
        }

        for (int i = (int)((double j = 10) + j); (j = i) < 12; i++)
            System.Console.WriteLine(j + (int k = 5 + i));

        j = 3;
        k = 4;

        for (int i = l; i < (int l = 12); i++) {}

        for (int i = 0; i < m; i = (int m = 12) + m) {}

        for (int i = n; i < 1; i++) 
            System.Console.WriteLine((int n = 5) + n);

        for (int i = 0; i < o; i++) 
            System.Console.WriteLine(int o = 5);

        for (int i = 0; i < 1; i = p) 
            System.Console.WriteLine(int p = 5);

        for (int i = 0; i < q; i++)
        { 
            System.Console.WriteLine((int q = 5) + q);
        }

        for (int i = 0; i < 1; i = r) 
        {
            System.Console.WriteLine(int r = 5);
        }


        int a1 = 1;
        System.Console.WriteLine((int b1 = 5) + a1);
            
        for (int i = (int a1 = 0); i < 0 ;);
        for (int i = (int b1 = 0); i < 0 ;);

        int a2 = 1;
        System.Console.WriteLine(a2);
        for (int i = 0; i < (int a2 = 0) ;);

        int a3 = 1;
        System.Console.WriteLine(a3);
        for (int i = 0; i < 0; i +=(int a3 = 0));

        int a4 = 1;
        System.Console.WriteLine(a4);
        for (int i = 0; i < 0; i ++) 
            System.Console.WriteLine(int a4 = 0);

        int a5 = 1;
        System.Console.WriteLine(a5);
        for (int i = 0; i < 0; i ++) 
        {
            System.Console.WriteLine(int a5 = 0);
        }


        for (int i = (int c1 = 0) + (int c1 = 1); i < 0 ;);

        for (int i = (int c2 = 0); i < (int c2 = 0) ;);

        for (int i = (int c3 = 0); i < 0; i +=(int c3 = 0));

        for (int i = (int c4 = 0); i < 0; i ++) 
            System.Console.WriteLine(int c4 = 0);

        for (int i = (int c5 = 0); i < 0; i ++) 
        {
            System.Console.WriteLine(int c5 = 0);
        }

        for (int i = (int c6 = 0); i < 0; i ++) 
        {
            int c6 = 0;
            System.Console.WriteLine(c6);
        }


        for (int i = 0; (int d1 = 0) < (int d1 = 1); i++);

        for (int i = 0; (int d2 = 0) < 0; i += (int d2 = 0));

        for (int i = 0; (int d3 = 0) < 0; i ++) 
            System.Console.WriteLine(int d3 = 0);

        for (int i = 0; (int d4 = 0) < 0; i ++) 
        {
            System.Console.WriteLine(int d4 = 0);
        }

        for (int i = 0; (int d5 = 0) < 0; i ++) 
        {
            int d5 = 0;
            System.Console.WriteLine(d5);
        }


        for (int i = 0; i < 0; i += (int e1 = 0) + (int e1 = 1));

        for (int i = 0; i < 0; i += (int e2 = 0)) 
            System.Console.WriteLine(int e2 = 0);

        for (int i = 0; i < 0; i += (int e3 = 0)) 
        {
            System.Console.WriteLine(int e3 = 0);
        }

        for (int i = 0; i < 0; i += (int e4 = 0)) 
        {
            int e4 = 0;
            System.Console.WriteLine(e4);
        }


        for (int i = 0; i < 0; i ++) 
            System.Console.WriteLine((int f1 = 0)+(int f1 = 1));
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (14,9): error CS0103: The name 'j' does not exist in the current context
    //         j = 3;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "j").WithArguments("j").WithLocation(14, 9),
    // (15,9): error CS0103: The name 'k' does not exist in the current context
    //         k = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "k").WithArguments("k").WithLocation(15, 9),
    // (17,22): error CS0103: The name 'l' does not exist in the current context
    //         for (int i = l; i < int l = 12; i++) {}
    Diagnostic(ErrorCode.ERR_NameNotInContext, "l").WithArguments("l").WithLocation(17, 22),
    // (19,29): error CS0841: Cannot use local variable 'm' before it is declared
    //         for (int i = 0; i < m; i = (int m = 12)) {}
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "m").WithArguments("m").WithLocation(19, 29),
    // (21,22): error CS0103: The name 'n' does not exist in the current context
    //         for (int i = n; i < 1; i++) 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "n").WithArguments("n").WithLocation(21, 22),
    // (24,29): error CS0103: The name 'o' does not exist in the current context
    //         for (int i = 0; i < o; i++) 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(24, 29),
    // (27,36): error CS0103: The name 'p' does not exist in the current context
    //         for (int i = 0; i < 1; i = p) 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p").WithArguments("p").WithLocation(27, 36),
    // (30,29): error CS0103: The name 'q' does not exist in the current context
    //         for (int i = 0; i < q; i++)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "q").WithArguments("q").WithLocation(30, 29),
    // (35,36): error CS0103: The name 'r' does not exist in the current context
    //         for (int i = 0; i < 1; i = r) 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "r").WithArguments("r").WithLocation(35, 36),
    // (44,27): error CS0136: A local or parameter named 'a1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         for (int i = (int a1 = 0); i < 0 ;);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a1").WithArguments("a1").WithLocation(44, 27),
    // (45,27): error CS0136: A local or parameter named 'b1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         for (int i = (int b1 = 0); i < 0 ;);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "b1").WithArguments("b1").WithLocation(45, 27),
    // (49,34): error CS0136: A local or parameter named 'a2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         for (int i = 0; i < (int a2 = 0) ;);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a2").WithArguments("a2").WithLocation(49, 34),
    // (53,41): error CS0136: A local or parameter named 'a3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         for (int i = 0; i < 0; i +=(int a3 = 0));
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a3").WithArguments("a3").WithLocation(53, 41),
    // (58,42): error CS0136: A local or parameter named 'a4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a4 = 0);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a4").WithArguments("a4").WithLocation(58, 42),
    // (64,42): error CS0136: A local or parameter named 'a5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a5 = 0);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a5").WithArguments("a5").WithLocation(64, 42),
    // (68,42): error CS0128: A local variable named 'c1' is already defined in this scope
    //         for (int i = (int c1 = 0) + (int c1 = 1); i < 0 ;);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c1").WithArguments("c1").WithLocation(68, 42),
    // (70,45): error CS0136: A local or parameter named 'c2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         for (int i = (int c2 = 0); i < (int c2 = 0) ;);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c2").WithArguments("c2").WithLocation(70, 45),
    // (72,52): error CS0136: A local or parameter named 'c3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         for (int i = (int c3 = 0); i < 0; i +=(int c3 = 0));
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c3").WithArguments("c3").WithLocation(72, 52),
    // (75,42): error CS0136: A local or parameter named 'c4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c4 = 0);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c4").WithArguments("c4").WithLocation(75, 42),
    // (79,42): error CS0136: A local or parameter named 'c5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c5 = 0);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c5").WithArguments("c5").WithLocation(79, 42),
    // (84,17): error CS0136: A local or parameter named 'c6' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             int c6 = 0;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c6").WithArguments("c6").WithLocation(84, 17),
    // (89,45): error CS0128: A local variable named 'd1' is already defined in this scope
    //         for (int i = 0; (int d1 = 0) < (int d1 = 1); );
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "d1").WithArguments("d1").WithLocation(89, 45),
    // (91,53): error CS0128: A local variable named 'd2' is already defined in this scope
    //         for (int i = 0; (int d2 = 0) < 0; i += (int d2 = 0));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "d2").WithArguments("d2").WithLocation(91, 53),
    // (94,42): error CS0136: A local or parameter named 'd3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int d3 = 0);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "d3").WithArguments("d3").WithLocation(94, 42),
    // (98,42): error CS0136: A local or parameter named 'd4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int d4 = 0);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "d4").WithArguments("d4").WithLocation(98, 42),
    // (103,17): error CS0136: A local or parameter named 'd5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             int d5 = 0;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "d5").WithArguments("d5").WithLocation(103, 17),
    // (108,57): error CS0128: A local variable named 'e1' is already defined in this scope
    //         for (int i = 0; i < 0; i += (int e1 = 0) + (int e1 = 1));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "e1").WithArguments("e1").WithLocation(108, 57),
    // (111,42): error CS0136: A local or parameter named 'e2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int e2 = 0);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "e2").WithArguments("e2").WithLocation(111, 42),
    // (115,42): error CS0136: A local or parameter named 'e3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int e3 = 0);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "e3").WithArguments("e3").WithLocation(115, 42),
    // (120,17): error CS0136: A local or parameter named 'e4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             int e4 = 0;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "e4").WithArguments("e4").WithLocation(120, 17),
    // (126,56): error CS0128: A local variable named 'f1' is already defined in this scope
    //             System.Console.WriteLine((int f1 = 0)+(int f1 = 1));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "f1").WithArguments("f1").WithLocation(126, 56),
    // (8,38): error CS0165: Use of unassigned local variable 'j'
    //             System.Console.WriteLine(j);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "j").WithArguments("j").WithLocation(8, 38)
                );
        }

        [Fact]
        public void For_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var lambdasJ = new System.Func<int>[3];
        var lambdasK = new System.Func<int>[2];
        var lambdasL = new System.Func<int>[2];

        for (int i = 0; 
                i < Dummy(2, int j = (i+1)*10, lambdasJ[i] = () => j);
                i = Dummy(i+1, int k = (i+1)*100, lambdasK[i] = () => k))
            Dummy(i, int l = (i+1)*1000, lambdasL[i] = () => l);

        foreach (var i in lambdasJ)
            System.Console.WriteLine(i());

        foreach (var i in lambdasK)
            System.Console.WriteLine(i());

        foreach (var i in lambdasL)
            System.Console.WriteLine(i());
    }

    static int Dummy(int val, object p1, object p2)
    {
        return val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"10
20
30
100
200
1000
2000").VerifyDiagnostics();
        }

        [Fact]
        public void For_04()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var lambdasJ = new System.Func<int>[3];

        for (int i = 0; 
                i < Dummy(2, int j = (i+1)*10, lambdasJ[i] = () => j);
                )
            i++;

        foreach (var i in lambdasJ)
            System.Console.WriteLine(i());
    }

    static int Dummy(int val, object p1, object p2)
    {
        return val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"10
20
30").VerifyDiagnostics();
        }

        [Fact]
        public void For_05()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var lambdasK = new System.Func<int>[2];

        for (int i = 0; 
                ;
                i = Dummy(i+1, int k = (i+1)*100, lambdasK[i] = () => k))
            if (i >= 2) break;

        foreach (var i in lambdasK)
            System.Console.WriteLine(i());
    }

    static int Dummy(int val, object p1, object p2)
    {
        return val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"100
200").VerifyDiagnostics();
        }

        [Fact]
        public void For_06()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var lambdasJ = new System.Func<int>[3];

        for (int i = 0; 
                i < Dummy(2, int j = (i+1)*10, lambdasJ[i] = () => j);
                i++)
            ;

        foreach (var i in lambdasJ)
            System.Console.WriteLine(i());
    }

    static int Dummy(int val, object p1, object p2)
    {
        return val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"10
20
30").VerifyDiagnostics();
        }

        [Fact]
        public void For_07()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var lambdasL = new System.Func<int>[2];

        for (int i = 0; 
                i < 2;
                i++)
            Dummy(i, int l = (i+1)*1000, lambdasL[i] = () => l);

        foreach (var i in lambdasL)
            System.Console.WriteLine(i());
    }

    static int Dummy(int val, object p1, object p2)
    {
        return val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"1000
2000").VerifyDiagnostics();
        }

        [Fact]
        public void Using_01()
        {
            var text = @"
using System.Collections.Generic;

public class Cls
{
    public static void Main()
    {
        using (var e = ((IEnumerable<int>)(new [] { int j = 0, 1})).GetEnumerator())
        {
            while(e.MoveNext())
            {
                System.Console.WriteLine(j);
                j++;
            }
        }

        using (var e = ((IEnumerable<int>)(new [] { int j = 3, 1})).GetEnumerator())
            System.Console.WriteLine(j + (int k = 5) + k);

        using (((IEnumerable<int>)(new [] { int j = 5, 1})).GetEnumerator())
            System.Console.WriteLine(j + (int k = 10) + k);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"0
1
13
25").VerifyDiagnostics();
        }

        [Fact]
        public void Using_02()
        {
            var text = @"
using System.Collections.Generic;

public class Cls
{
    public static void Main()
    {
        using (var e = ((IEnumerable<int>)(new [] { (int j = 0) + j, 1})).GetEnumerator())
        {
            while(e.MoveNext())
            {
                System.Console.WriteLine(j);
                j++;
            }
        }

        using (var e = ((IEnumerable<int>)(new [] { int j = 3, 1})).GetEnumerator())
            System.Console.WriteLine(j + (int k = 5) + k);

        j = 3;
        k = 4;

        using (var e = l)
        {
            System.Console.WriteLine(IEnumerator<int> l = null);
        }

        using (var e = m)
            System.Console.WriteLine(IEnumerator<int> m = null);

        int a1 = 0;
        System.Console.WriteLine(a1 + (int b1 = 1));

        using (var e = ((IEnumerable<int>)(new [] { int a1 = 3, 1})).GetEnumerator()) System.Console.WriteLine();
        using (var e = ((IEnumerable<int>)(new [] { int b1 = 3, 1})).GetEnumerator()) System.Console.WriteLine();

        int a2 = 0;
        System.Console.WriteLine(a2);
        using (var e = ((IEnumerable<int>)(new [] { 0, 1})).GetEnumerator()) 
            System.Console.WriteLine(int a2 = 1);

        int a3 = 0;
        System.Console.WriteLine(a3);
        using (var e = ((IEnumerable<int>)(new [] { 0, 1})).GetEnumerator()) 
        {
            System.Console.WriteLine(int a3 = 1);
        }

        using (var c1 = ((IEnumerable<int>)(new [] { int c1 = 3, 1})).GetEnumerator()) System.Console.WriteLine();

        using (var c2 = ((IEnumerable<int>)(new [] { 0, 1})).GetEnumerator()) 
        {
            System.Console.WriteLine(int c2 = 1);
        }

        using (var e = ((IEnumerable<int>)(new [] { int d1 = 3, int d1 = 4})).GetEnumerator()) System.Console.WriteLine();

        using (var e = ((IEnumerable<int>)(new [] { int d2 = 3, 1})).GetEnumerator()) 
            System.Console.WriteLine(int d2 = 1);

        using (var e = ((IEnumerable<int>)(new [] { int d3 = 3, 1})).GetEnumerator()) 
        {
            System.Console.WriteLine(int d3 = 1);
        }

        using (var e = ((IEnumerable<int>)(new [] { int d4 = 3, 1})).GetEnumerator()) 
        {
            int d4 = 0;
            System.Console.WriteLine(d4);
        }

        using (var c3 = ((IEnumerable<int>)(new [] { 0, 1})).GetEnumerator()) 
            System.Console.WriteLine(int c3 = 1);

        using (var e = ((IEnumerable<int>)(new [] { 0, 1})).GetEnumerator()) 
            System.Console.WriteLine((int e1 = 1) + (int e1 = 1));
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (20,9): error CS0103: The name 'j' does not exist in the current context
    //         j = 3;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "j").WithArguments("j").WithLocation(20, 9),
    // (21,9): error CS0103: The name 'k' does not exist in the current context
    //         k = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "k").WithArguments("k").WithLocation(21, 9),
    // (23,24): error CS0103: The name 'l' does not exist in the current context
    //         using (var e = l)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "l").WithArguments("l").WithLocation(23, 24),
    // (28,24): error CS0103: The name 'm' does not exist in the current context
    //         using (var e = m)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "m").WithArguments("m").WithLocation(28, 24),
    // (34,57): error CS0136: A local or parameter named 'a1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         using (var e = ((IEnumerable<int>)(new [] { int a1 = 3, 1})).GetEnumerator()) ;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a1").WithArguments("a1").WithLocation(34, 57),
    // (35,57): error CS0136: A local or parameter named 'b1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         using (var e = ((IEnumerable<int>)(new [] { int b1 = 3, 1})).GetEnumerator()) ;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "b1").WithArguments("b1").WithLocation(35, 57),
    // (40,42): error CS0136: A local or parameter named 'a2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a2").WithArguments("a2").WithLocation(40, 42),
    // (46,42): error CS0136: A local or parameter named 'a3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a3 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a3").WithArguments("a3").WithLocation(46, 42),
    // (49,58): error CS0128: A local variable named 'c1' is already defined in this scope
    //         using (var c1 = ((IEnumerable<int>)(new [] { int c1 = 3, 1})).GetEnumerator()) ;
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c1").WithArguments("c1").WithLocation(49, 58),
    // (53,42): error CS0136: A local or parameter named 'c2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c2").WithArguments("c2").WithLocation(53, 42),
    // (56,69): error CS0128: A local variable named 'd1' is already defined in this scope
    //         using (var e = ((IEnumerable<int>)(new [] { int d1 = 3, int d1 = 4})).GetEnumerator()) ;
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "d1").WithArguments("d1").WithLocation(56, 69),
    // (59,42): error CS0136: A local or parameter named 'd2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int d2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "d2").WithArguments("d2").WithLocation(59, 42),
    // (63,42): error CS0136: A local or parameter named 'd3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int d3 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "d3").WithArguments("d3").WithLocation(63, 42),
    // (68,17): error CS0136: A local or parameter named 'd4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             int d4 = 0;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "d4").WithArguments("d4").WithLocation(68, 17),
    // (73,42): error CS0136: A local or parameter named 'c3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c3 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c3").WithArguments("c3").WithLocation(73, 42),
    // (76,58): error CS0128: A local variable named 'e1' is already defined in this scope
    //             System.Console.WriteLine((int e1 = 1) + (int e1 = 1));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "e1").WithArguments("e1").WithLocation(76, 58)
                );
        }

        [Fact]
        public void Fixed_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        unsafe
        {
            fixed (int* p = new[] { 1, int j = 2 })
            {
                System.Console.WriteLine(j);
            }

            fixed (int* p = new[] { 1, int j = -20 })
                System.Console.WriteLine(j + (int k = 5) + k);
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe.WithAllowUnsafe(true), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"2
-10").VerifyDiagnostics();
        }

        [Fact]
        public void Fixed_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        unsafe
        {
            fixed (int* p = new[] { 1, (int j = 2) + j })
            {
                System.Console.WriteLine(j);
            }

            fixed (int* p = new[] { 1, int j = -20 })
                System.Console.WriteLine(j + (int k = 5) + k);

            j = 3;
            k = 4;

            fixed (int* p = l)
            {
                System.Console.WriteLine(int[] l = null);
            }

            fixed (int* p = m)
                System.Console.WriteLine(int[] m = null);

            int a1 = 1;
            System.Console.WriteLine(a1);
            fixed (int* p = new[] { int a1 = 1, 2 })
                System.Console.WriteLine();

            int a2 = 1;
            System.Console.WriteLine(a2);
            fixed (int* p = new[] { 1, 2 })
                System.Console.WriteLine(int a2 = 2);

            int a3 = 1;
            System.Console.WriteLine(a3);
            fixed (int* p = new[] { 1, 2 })
            {
                System.Console.WriteLine(int a3 = 3);
            }

            fixed (int* c1 = new[] { int c1 = 1, 2 })
                System.Console.WriteLine();

            fixed (int* c2 = new[] { 1, 2 })
                System.Console.WriteLine(int c2 = 2);

            fixed (int* c3 = new[] { 1, 2 })
            {
                System.Console.WriteLine(int c3 = 3);
            }

            fixed (int* p = new[] { int d1 = 1, int d1 = 2 })
                System.Console.WriteLine();

            fixed (int* p = new[] { int d2 = 1, 2 })
                System.Console.WriteLine(int d2 = 2);

            fixed (int* p = new[] { int d3 = 1, 2 })
            {
                System.Console.WriteLine(int d3 = 3);
            }

            fixed (int* p = new[] { 1, 2 })
                System.Console.WriteLine((int e1 = 2) + (int e1 = 2));
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe.WithAllowUnsafe(true), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (20,9): error CS0103: The name 'j' does not exist in the current context
    //         j = 3;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "j").WithArguments("j"),
    // (21,9): error CS0103: The name 'k' does not exist in the current context
    //         k = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "k").WithArguments("k"),
    // (19,29): error CS0103: The name 'l' does not exist in the current context
    //             fixed (int* p = l)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "l").WithArguments("l"),
    // (24,29): error CS0103: The name 'm' does not exist in the current context
    //             fixed (int* p = m)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "m").WithArguments("m"),
    // (29,41): error CS0136: A local or parameter named 'a1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             fixed (int* p = new[] { int a1 = 1, 2 })
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a1").WithArguments("a1").WithLocation(29, 41),
    // (35,46): error CS0136: A local or parameter named 'a2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 System.Console.WriteLine(int a2 = 2);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a2").WithArguments("a2").WithLocation(35, 46),
    // (41,46): error CS0136: A local or parameter named 'a3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 System.Console.WriteLine(int a3 = 3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a3").WithArguments("a3").WithLocation(41, 46),
    // (44,42): error CS0128: A local variable named 'c1' is already defined in this scope
    //             fixed (int* c1 = new[] { int c1 = 1, 2 })
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c1").WithArguments("c1").WithLocation(44, 42),
    // (48,46): error CS0136: A local or parameter named 'c2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 System.Console.WriteLine(int c2 = 2);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c2").WithArguments("c2").WithLocation(48, 46),
    // (52,46): error CS0136: A local or parameter named 'c3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 System.Console.WriteLine(int c3 = 3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c3").WithArguments("c3").WithLocation(52, 46),
    // (55,53): error CS0128: A local variable named 'd1' is already defined in this scope
    //             fixed (int* p = new[] { int d1 = 1, int d1 = 2 })
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "d1").WithArguments("d1").WithLocation(55, 53),
    // (59,46): error CS0136: A local or parameter named 'd2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 System.Console.WriteLine(int d2 = 2);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "d2").WithArguments("d2").WithLocation(59, 46),
    // (63,46): error CS0136: A local or parameter named 'd3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 System.Console.WriteLine(int d3 = 3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "d3").WithArguments("d3").WithLocation(63, 46),
    // (67,62): error CS0128: A local variable named 'e1' is already defined in this scope
    //                 System.Console.WriteLine((int e1 = 2) + (int e1 = 2));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "e1").WithArguments("e1").WithLocation(67, 62)
                );
        }

        [Fact]
        public void switch_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        switch ((int j = 2) - 1)
        {
            default:
                System.Console.WriteLine(j);
                break;
        }

        switch ((int j = 3) - 1)
        {
            default:
                System.Console.WriteLine(j + (int k = 5) + k);
                break;
            case 298980:
                k = 3;
                System.Console.WriteLine(k);
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"2
13").VerifyDiagnostics();
        }

        [Fact]
        public void switch_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        switch ((int j = 2) - j)
        {
            default:
                System.Console.WriteLine(j);
                break;
        }

        switch ((int j = 3) - 1)
        {
            default:
                System.Console.WriteLine(j + (int k = 5) + k);
                break;
        }

        j = 3;
        k = 4;

        switch (l)
        {
            default:
                System.Console.WriteLine(int l = 5);
                break;
        }

        switch ((int j = 3) - 1)
        {
            case 0:
                m=2;
                break;
            default:
                System.Console.WriteLine(int m = 5);
                break;
        }

        int a1 = 0;
        System.Console.WriteLine(a1);
        switch (int a1 = 0)
        {
            default:
                break;
        }
        
        int a2 = 0;
        int a3 = 0;
        System.Console.WriteLine(a2);
        switch (a3)
        {
            default:
                System.Console.WriteLine(int a2 = 5);
                break;
        }
        
        switch ((int c1 = 0) + (int c1 = 1))
        {
            default:
                break;
        }

        switch (int c2 = 0)
        {
            default:
                System.Console.WriteLine(int c2 = 5);
                break;
        }

        switch (int c3 = 0)
        {
            default:
                int c3 = 0;
                System.Console.WriteLine(c3);
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (14,9): error CS0103: The name 'j' does not exist in the current context
    //         j = 3;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "j").WithArguments("j"),
    // (15,9): error CS0103: The name 'k' does not exist in the current context
    //         k = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "k").WithArguments("k"),
    // (23,17): error CS0103: The name 'l' does not exist in the current context
    //         switch (l)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "l").WithArguments("l"),
    // (33,17): error CS0841: Cannot use local variable 'm' before it is declared
    //                 m=2;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "m").WithArguments("m"),
    // (42,21): error CS0136: A local or parameter named 'a1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         switch (int a1 = 0)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a1").WithArguments("a1").WithLocation(42, 21),
    // (54,46): error CS0136: A local or parameter named 'a2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 System.Console.WriteLine(int a2 = 5);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a2").WithArguments("a2").WithLocation(54, 46),
    // (58,37): error CS0128: A local variable named 'c1' is already defined in this scope
    //         switch ((int c1 = 0) + (int c1 = 1))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c1").WithArguments("c1").WithLocation(58, 37),
    // (67,46): error CS0136: A local or parameter named 'c2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 System.Console.WriteLine(int c2 = 5);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c2").WithArguments("c2").WithLocation(67, 46),
    // (74,21): error CS0136: A local or parameter named 'c3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 int c3 = 0;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c3").WithArguments("c3").WithLocation(74, 21)
                );
        }

        [Fact]
        public void ForEach_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        foreach (var i in new [] { int j = 0, 1})
        {
            System.Console.WriteLine(j);
            j+=2;
        }

        foreach (var i in new [] { int j = 0, 1})
            System.Console.WriteLine(j = j + (int k = 5) + k);

        var lambdas = new System.Func<int>[2];
        foreach (var i in new [] { 0, 1})
            Dummy(int k = i+30, lambdas[i] = () => k);

        foreach (var i in lambdas)
            System.Console.WriteLine(i());

        foreach (var i in (System.Collections.Generic.IEnumerable<int>)(new [] { int j = 10, 1}))
            System.Console.WriteLine(j = j + i);
    }

    static void Dummy(object p1, object p2){}
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"0
2
10
20
30
31
20
21").VerifyDiagnostics();
        }

        [Fact]
        public void ForEach_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        foreach (var i in new [] { (int j = 0) + j, 1})
        {
            System.Console.WriteLine(j);
            j+=2;
        }

        foreach (var i in new [] { int j = 0, 1})
            System.Console.WriteLine(j = j + (int k = 5) + k);

        j = 3;
        k = 4;

        foreach (var i in new [] { (int)l})
            System.Console.WriteLine(int l = 5 + i);

        int a1 = 0;
        System.Console.WriteLine(a1);
        foreach (var i in new [] { int a1 = 2 })
            ;

        int a2 = 0;
        System.Console.WriteLine(a2);
        foreach (var i in new [] { 2 })
            System.Console.WriteLine(int a2 = 3);

        int a3 = 0;
        System.Console.WriteLine(a3);
        foreach (var i in new [] { 2 })
        {
            System.Console.WriteLine(int a3 = 3);
        }

        foreach (var i in new [] { int c1 = 2, int c1 = 2 })
            ;

        foreach (var c2 in new [] { int c2 = 2 })
            ;

        foreach (var i in new [] { int c3 = 2 })
            System.Console.WriteLine(int c3 = 3);

        foreach (var i in new [] { int c4 = 2 })
        {
            System.Console.WriteLine(int c4 = 3);
        }

        foreach (var i in new [] { int c5 = 2 })
        {
            int c5 = 0;
            System.Console.WriteLine(c5);
        }

        foreach (var d1 in new [] { 2 })
            System.Console.WriteLine(int d1 = 3);

        foreach (var d2 in new [] { 2 })
        {
            System.Console.WriteLine(int d2 = 3);
        }

        foreach (var i in new [] { 2 })
            System.Console.WriteLine((int e1 = 3) + (int e1 = 3));
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (20,9): error CS0103: The name 'j' does not exist in the current context
    //         j = 3;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "j").WithArguments("j"),
    // (21,9): error CS0103: The name 'k' does not exist in the current context
    //         k = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "k").WithArguments("k"),
    // (18,41): error CS0103: The name 'l' does not exist in the current context
    //         foreach (var i in new [] { (int)l})
    Diagnostic(ErrorCode.ERR_NameNotInContext, "l").WithArguments("l"),
    // (23,40): error CS0136: A local or parameter named 'a1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         foreach (var i in new [] { int a1 = 2 })
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a1").WithArguments("a1").WithLocation(23, 40),
    // (29,42): error CS0136: A local or parameter named 'a2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a2 = 3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a2").WithArguments("a2").WithLocation(29, 42),
    // (35,42): error CS0136: A local or parameter named 'a3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a3 = 3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a3").WithArguments("a3").WithLocation(35, 42),
    // (38,52): error CS0128: A local variable named 'c1' is already defined in this scope
    //         foreach (var i in new [] { int c1 = 2, int c1 = 2 })
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c1").WithArguments("c1").WithLocation(38, 52),
    // (41,22): error CS0136: A local or parameter named 'c2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         foreach (var c2 in new [] { int c2 = 2 })
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c2").WithArguments("c2").WithLocation(41, 22),
    // (45,42): error CS0136: A local or parameter named 'c3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c3 = 3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c3").WithArguments("c3").WithLocation(45, 42),
    // (49,42): error CS0136: A local or parameter named 'c4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c4 = 3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c4").WithArguments("c4").WithLocation(49, 42),
    // (54,17): error CS0136: A local or parameter named 'c5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             int c5 = 0;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c5").WithArguments("c5").WithLocation(54, 17),
    // (59,42): error CS0136: A local or parameter named 'd1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int d1 = 3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "d1").WithArguments("d1").WithLocation(59, 42),
    // (63,42): error CS0136: A local or parameter named 'd2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int d2 = 3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "d2").WithArguments("d2").WithLocation(63, 42),
    // (67,58): error CS0128: A local variable named 'e1' is already defined in this scope
    //             System.Console.WriteLine((int e1 = 3) + (int e1 = 3));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "e1").WithArguments("e1").WithLocation(67, 58)
                );
        }

        [Fact]
        public void While_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        int i;

        i = 0;
        while((int j = i + 1) < 3)
        {
            System.Console.WriteLine(j + i++);
        }

        i = 10;
        while ((int j = i) < 12)
            System.Console.WriteLine(j + (int k = 5 + i) + k + i++);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"1
3
50
54").VerifyDiagnostics();
        }

        [Fact]
        public void While_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        while ((int j = 10) < j)
            System.Console.WriteLine(j + (int k = 5) + k);

        j = 3;
        k = 4;

        while (n < 1) 
            System.Console.WriteLine(int n = 5);

        while (0 < q)
        { 
            System.Console.WriteLine(int q = 5);
        }

        int a1 = 0;
        System.Console.WriteLine(a1);
        while (bool a1 = true)
            ;

        int a2 = 0;
        System.Console.WriteLine(a2);
        while (a2 > 0)
            System.Console.WriteLine(int a2 = 1);

        int a3 = 0;
        System.Console.WriteLine(a3);
        while (a2 > 0)
        {
            System.Console.WriteLine(int a3 = 1);
        }

        while ((bool c1 = true) && (bool c1 = true))
            ;

        while (bool c2 = true)
            System.Console.WriteLine(int c2 = 1);

        while (bool c3 = true)
        {
            System.Console.WriteLine(int c3 = 1);
        }

        while (bool c4 = true)
        {
            int c4 = 0;
            System.Console.WriteLine(c4);
        }

        while (a2 > 0)
            System.Console.WriteLine((int d1 = 1) + (int d1 = 1));
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (9,9): error CS0103: The name 'j' does not exist in the current context
    //         j = 3;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "j").WithArguments("j").WithLocation(9, 9),
    // (10,9): error CS0103: The name 'k' does not exist in the current context
    //         k = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "k").WithArguments("k").WithLocation(10, 9),
    // (12,16): error CS0103: The name 'n' does not exist in the current context
    //         while (n < 1) 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "n").WithArguments("n").WithLocation(12, 16),
    // (15,20): error CS0103: The name 'q' does not exist in the current context
    //         while (0 < q)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "q").WithArguments("q").WithLocation(15, 20),
    // (22,21): error CS0136: A local or parameter named 'a1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         while (bool a1 = true)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a1").WithArguments("a1").WithLocation(22, 21),
    // (28,42): error CS0136: A local or parameter named 'a2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a2").WithArguments("a2").WithLocation(28, 42),
    // (34,42): error CS0136: A local or parameter named 'a3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a3 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a3").WithArguments("a3").WithLocation(34, 42),
    // (37,42): error CS0128: A local variable named 'c1' is already defined in this scope
    //         while ((bool c1 = true) && (bool c1 = true))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c1").WithArguments("c1").WithLocation(37, 42),
    // (41,42): error CS0136: A local or parameter named 'c2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c2").WithArguments("c2").WithLocation(41, 42),
    // (45,42): error CS0136: A local or parameter named 'c3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c3 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c3").WithArguments("c3").WithLocation(45, 42),
    // (50,17): error CS0136: A local or parameter named 'c4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             int c4 = 0;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c4").WithArguments("c4").WithLocation(50, 17),
    // (55,58): error CS0128: A local variable named 'd1' is already defined in this scope
    //             System.Console.WriteLine((int d1 = 1) + (int d1 = 1));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "d1").WithArguments("d1").WithLocation(55, 58)

                );
        }

        [Fact]
        public void While_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var lambdasJ = new System.Func<int>[3];
        var lambdasK = new System.Func<int>[2];

        int i = 0; 

        while (i < Dummy(2, int j = (i+1)*10, lambdasJ[i] = () => j))
            i = Dummy(i+1, int k = (i+1)*100, lambdasK[i] = () => k);

        foreach (var l in lambdasJ)
            System.Console.WriteLine(l());

        foreach (var l in lambdasK)
            System.Console.WriteLine(l());
    }

    static int Dummy(int val, object p1, object p2)
    {
        return val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"10
20
30
100
200").VerifyDiagnostics();
        }

        [Fact]
        public void While_04()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var lambdasJ = new System.Func<int>[3];

        int i = 0; 

        while (i < Dummy(2, int j = (i+1)*10, lambdasJ[i] = () => j))
            i++;

        foreach (var l in lambdasJ)
            System.Console.WriteLine(l());
    }

    static int Dummy(int val, object p1, object p2)
    {
        return val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"10
20
30").VerifyDiagnostics();
        }

        [Fact]
        public void While_05()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var lambdasK = new System.Func<int>[2];

        int i = 0; 

        while (i < 2)
            i = Dummy(i+1, int k = (i+1)*100, lambdasK[i] = () => k);

        foreach (var l in lambdasK)
            System.Console.WriteLine(l());
    }

    static int Dummy(int val, object p1, object p2)
    {
        return val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"100
200").VerifyDiagnostics();
        }

        [Fact]
        public void Do_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        int i;

        i = 0;

        do
        {
            System.Console.WriteLine((int j = i + 1) + j * 2);
        }
        while((int k = Dummy(2, i++)) + k > i * 2);
 
        do
            System.Console.WriteLine((int j = 1) + j * 3);
        while(i < 0);
    }

    private static int Dummy(int val, int p1)
    {
        return val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"3
6
4").VerifyDiagnostics();
        }

        [Fact]
        public void Do_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        do
            System.Console.WriteLine((int k = 5) + k);
        while ((int j = 10) < j);

        j = 3;
        k = 4;

        do
            System.Console.WriteLine(n);
        while ((int n = 5) < 1);

        do
        {
            System.Console.WriteLine(q);
        }
        while ((int q = 5) < 1);

        do
        {
            System.Console.WriteLine(int r = 2);
        }
        while (r < 1);

        do
            System.Console.WriteLine(int s = 1);
        while(s < 3);

        int a1 = 0;
        System.Console.WriteLine(a1);
        do
            System.Console.WriteLine();
        while (bool a1 = true);

        int a2 = 0;
        System.Console.WriteLine(a2);
        do
            System.Console.WriteLine(int a2 = 1);
        while (a2 > 0);

        int a3 = 0;
        System.Console.WriteLine(a3);
        do
        {
            System.Console.WriteLine(int a3 = 1);
        }
        while (a2 > 0);

        do
            System.Console.WriteLine();
        while ((bool c1 = true) && (bool c1 = true));

        do
            System.Console.WriteLine(int c2 = 1);
        while (bool c2 = true);

        do
        {
            System.Console.WriteLine(int c3 = 1);
        }
        while (bool c3 = true);

        do
        {
            int c4 = 0;
            System.Console.WriteLine(c4);
        }
        while (bool c4 = true);

        do
            System.Console.WriteLine((int d1 = 1) + (int d1 = 1));
        while (a2 > 0);

    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (10,9): error CS0103: The name 'j' does not exist in the current context
    //         j = 3;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "j").WithArguments("j").WithLocation(10, 9),
    // (11,9): error CS0103: The name 'k' does not exist in the current context
    //         k = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "k").WithArguments("k").WithLocation(11, 9),
    // (14,38): error CS0841: Cannot use local variable 'n' before it is declared
    //             System.Console.WriteLine(n);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "n").WithArguments("n").WithLocation(14, 38),
    // (19,38): error CS0841: Cannot use local variable 'q' before it is declared
    //             System.Console.WriteLine(q);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "q").WithArguments("q").WithLocation(19, 38),
    // (27,16): error CS0103: The name 'r' does not exist in the current context
    //         while (r < 1);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "r").WithArguments("r").WithLocation(27, 16),
    // (31,15): error CS0103: The name 's' does not exist in the current context
    //         while(s < 3);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "s").WithArguments("s").WithLocation(31, 15),
    // (37,21): error CS0136: A local or parameter named 'a1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         while (bool a1 = true);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a1").WithArguments("a1").WithLocation(37, 21),
    // (42,42): error CS0136: A local or parameter named 'a2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a2").WithArguments("a2").WithLocation(42, 42),
    // (49,42): error CS0136: A local or parameter named 'a3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a3 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a3").WithArguments("a3").WithLocation(49, 42),
    // (55,42): error CS0128: A local variable named 'c1' is already defined in this scope
    //         while ((bool c1 = true) && (bool c1 = true));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c1").WithArguments("c1").WithLocation(55, 42),
    // (58,42): error CS0136: A local or parameter named 'c2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c2").WithArguments("c2").WithLocation(58, 42),
    // (63,42): error CS0136: A local or parameter named 'c3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c3 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c3").WithArguments("c3").WithLocation(63, 42),
    // (69,17): error CS0136: A local or parameter named 'c4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             int c4 = 0;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c4").WithArguments("c4").WithLocation(69, 17),
    // (75,58): error CS0128: A local variable named 'd1' is already defined in this scope
    //             System.Console.WriteLine((int d1 = 1) + (int d1 = 1));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "d1").WithArguments("d1").WithLocation(75, 58)
                );
        }

        [Fact]
        public void Do_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var lambdasJ = new System.Func<int>[2];
        var lambdasK = new System.Func<int>[2];

        int i = 0; 

        do
            i = Dummy(i+1, int k = (i+1)*100, lambdasK[i] = () => k);
        while (i < Dummy(2, int j = (i+1)*10, lambdasJ[i - 1] = () => j));

        foreach (var l in lambdasK)
            System.Console.WriteLine(l());

        foreach (var l in lambdasJ)
            System.Console.WriteLine(l());
    }

    static int Dummy(int val, object p1, object p2)
    {
        return val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"100
200
20
30").VerifyDiagnostics();
        }

        [Fact]
        public void if_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        if ((int j = 2) - 1 > int.MinValue)
        {
            System.Console.WriteLine(j);
        }

        if ((int j = 3) - 1 > int.MinValue)
            System.Console.WriteLine(j);

        if ((int j = 3) - 1 < int.MinValue)
            System.Console.WriteLine(int k = 5);
        else
        {
            System.Console.WriteLine(j + (int k = 100));
        }

        if ((int j = 3) - 1 < int.MinValue)
            System.Console.WriteLine(int k = 5);
        else
            System.Console.WriteLine(j + (int k = 1000));
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"2
3
103
1003").VerifyDiagnostics();
        }

        [Fact]
        public void if_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        if ((int j = 2) > j)
            System.Console.WriteLine(j + (int k = 5) + k);
        else 
            System.Console.WriteLine(j + (int l = 5) + l);

        if ((int j = 2) > j)
        {
            System.Console.WriteLine(j + (int m = 5) + m);
        }
        else 
            System.Console.WriteLine(j + (int n = 5) + n + m);

        if ((int j = 2) > j)
            System.Console.WriteLine(j + (int k = 5) + k);
        else 
        {
            System.Console.WriteLine(j + (int o = 5) + o);
        }

        j = 3;
        k = 4;
        l = 4;
        m = 4;
        n = 4;
        o = 4;

        if (p < q)
        {
            System.Console.WriteLine(int p = 5);
        }
        else
        {
            System.Console.WriteLine(int q = 5);
        }

        if (r < s)
            System.Console.WriteLine(int r = 5);
        else
            System.Console.WriteLine(int s = 5);

        if ((int x = 3) > 0)
            System.Console.WriteLine((int t = 5) + u);
        else
            System.Console.WriteLine(int u = 5);

        if ((int x = 3) > 0)
        {
            System.Console.WriteLine((int v = 5) + w);
        }
        else
        {
            System.Console.WriteLine((int w = 5) + v);
        }

        if ((int a = 2) > b)
            System.Console.WriteLine(a + c + e);
        else if ((int b = 2) + (int c = 3) > a + d)
            System.Console.WriteLine(int d = 4);
        else 
            System.Console.WriteLine(int e = 5);

        if ((int x = 3) > 0)
            System.Console.WriteLine(int f = 1);
        else
            System.Console.WriteLine(f);

        if ((int x = 3) > g)
            System.Console.WriteLine();
        else 
            System.Console.WriteLine(int g = 5);

        int a1 = 0;
        System.Console.WriteLine(a1);
        if (bool a1 = true)
            System.Console.WriteLine();

        int a2 = 0;
        System.Console.WriteLine(a2);
        int a3 = 0;
        System.Console.WriteLine(a3);

        if (a1 > 0)
            System.Console.WriteLine(int a2 = 1);
        else
            System.Console.WriteLine(int a3 = 1);

        int a4 = 0;
        System.Console.WriteLine(a4);
        int a5 = 0;
        System.Console.WriteLine(a5);

        if (a1 > 0)
        {
            System.Console.WriteLine(int a4 = 1);
        }       
        else
        {
            System.Console.WriteLine(int a5 = 1);
        }

        if ((bool b1 = true) && (bool b1 = true))
            System.Console.WriteLine();

        if ((bool b2 = true) && (bool b3 = true))
            System.Console.WriteLine(int b2 = 1);
        else
            System.Console.WriteLine(int b3 = 1);

        if ((bool b4 = true) && (bool b5 = true))
        {
            System.Console.WriteLine(int b4 = 1);
        }
        else
        {
            System.Console.WriteLine(int b5 = 1);
        }

        if ((bool b6 = true) && (bool b7 = true))
        {
            int b6 = 1;
            System.Console.WriteLine(b6);
        }
        else
        {
            int b7 = 1;
            System.Console.WriteLine(b7);
        }

        if (a2 > 0)
            System.Console.WriteLine((int c1 = 1) + (int c1 = 1));
        else
            System.Console.WriteLine((int c2 = 1) + (int c2 = 1));
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (16,60): error CS0103: The name 'm' does not exist in the current context
    //             System.Console.WriteLine(j + (int n = 5) + n + m);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "m").WithArguments("m").WithLocation(16, 60),
    // (25,9): error CS0103: The name 'j' does not exist in the current context
    //         j = 3;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "j").WithArguments("j").WithLocation(25, 9),
    // (26,9): error CS0103: The name 'k' does not exist in the current context
    //         k = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "k").WithArguments("k").WithLocation(26, 9),
    // (27,9): error CS0103: The name 'l' does not exist in the current context
    //         l = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "l").WithArguments("l").WithLocation(27, 9),
    // (28,9): error CS0103: The name 'm' does not exist in the current context
    //         m = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "m").WithArguments("m").WithLocation(28, 9),
    // (29,9): error CS0103: The name 'n' does not exist in the current context
    //         n = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "n").WithArguments("n").WithLocation(29, 9),
    // (30,9): error CS0103: The name 'o' does not exist in the current context
    //         o = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(30, 9),
    // (32,17): error CS0103: The name 'q' does not exist in the current context
    //         if (p < q)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "q").WithArguments("q").WithLocation(32, 17),
    // (32,13): error CS0103: The name 'p' does not exist in the current context
    //         if (p < q)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p").WithArguments("p").WithLocation(32, 13),
    // (41,17): error CS0103: The name 's' does not exist in the current context
    //         if (r < s)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "s").WithArguments("s").WithLocation(41, 17),
    // (41,13): error CS0103: The name 'r' does not exist in the current context
    //         if (r < s)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "r").WithArguments("r").WithLocation(41, 13),
    // (47,52): error CS0103: The name 'u' does not exist in the current context
    //             System.Console.WriteLine((int t = 5) + u);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u").WithArguments("u").WithLocation(47, 52),
    // (53,52): error CS0103: The name 'w' does not exist in the current context
    //             System.Console.WriteLine((int v = 5) + w);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "w").WithArguments("w").WithLocation(53, 52),
    // (57,52): error CS0103: The name 'v' does not exist in the current context
    //             System.Console.WriteLine((int w = 5) + v);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v").WithArguments("v").WithLocation(57, 52),
    // (60,27): error CS0103: The name 'b' does not exist in the current context
    //         if ((int a = 2) > b)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(60, 27),
    // (61,46): error CS0103: The name 'e' does not exist in the current context
    //             System.Console.WriteLine(a + c + e);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(61, 46),
    // (61,42): error CS0103: The name 'c' does not exist in the current context
    //             System.Console.WriteLine(a + c + e);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "c").WithArguments("c").WithLocation(61, 42),
    // (62,50): error CS0103: The name 'd' does not exist in the current context
    //         else if ((int b = 2) + (int c = 3) > a + d)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "d").WithArguments("d").WithLocation(62, 50),
    // (70,38): error CS0103: The name 'f' does not exist in the current context
    //             System.Console.WriteLine(f);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "f").WithArguments("f").WithLocation(70, 38),
    // (72,27): error CS0103: The name 'g' does not exist in the current context
    //         if ((int x = 3) > g)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "g").WithArguments("g").WithLocation(72, 27),
    // (79,18): error CS0136: A local or parameter named 'a1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         if (bool a1 = true)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a1").WithArguments("a1").WithLocation(79, 18),
    // (88,42): error CS0136: A local or parameter named 'a2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a2").WithArguments("a2").WithLocation(88, 42),
    // (90,42): error CS0136: A local or parameter named 'a3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a3 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a3").WithArguments("a3").WithLocation(90, 42),
    // (99,42): error CS0136: A local or parameter named 'a4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a4 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a4").WithArguments("a4").WithLocation(99, 42),
    // (103,42): error CS0136: A local or parameter named 'a5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a5 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a5").WithArguments("a5").WithLocation(103, 42),
    // (106,39): error CS0128: A local variable named 'b1' is already defined in this scope
    //         if ((bool b1 = true) && (bool b1 = true))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "b1").WithArguments("b1").WithLocation(106, 39),
    // (110,42): error CS0136: A local or parameter named 'b2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int b2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "b2").WithArguments("b2").WithLocation(110, 42),
    // (112,42): error CS0136: A local or parameter named 'b3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int b3 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "b3").WithArguments("b3").WithLocation(112, 42),
    // (116,42): error CS0136: A local or parameter named 'b4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int b4 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "b4").WithArguments("b4").WithLocation(116, 42),
    // (120,42): error CS0136: A local or parameter named 'b5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int b5 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "b5").WithArguments("b5").WithLocation(120, 42),
    // (125,17): error CS0136: A local or parameter named 'b6' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             int b6 = 1;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "b6").WithArguments("b6").WithLocation(125, 17),
    // (130,17): error CS0136: A local or parameter named 'b7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             int b7 = 1;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "b7").WithArguments("b7").WithLocation(130, 17),
    // (135,58): error CS0128: A local variable named 'c1' is already defined in this scope
    //             System.Console.WriteLine((int c1 = 1) + (int c1 = 1));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c1").WithArguments("c1").WithLocation(135, 58),
    // (137,58): error CS0128: A local variable named 'c2' is already defined in this scope
    //             System.Console.WriteLine((int c2 = 1) + (int c2 = 1));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c2").WithArguments("c2").WithLocation(137, 58)
            );
        }

        [Fact]
        public void DataFlow_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out int y);
        System.Console.WriteLine(y);
    }

    static void Test(out int x)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"123").VerifyDiagnostics();
        }

        [Fact]
        public void DataFlow_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test((int y) = 123);
        System.Console.WriteLine(y);
    }

    static void Test(int x)
    {
        System.Console.WriteLine(x);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"123
123").VerifyDiagnostics();
        }

        [Fact]
        public void DataFlow_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test1(ref int x = 1);
        Test1(ref int y);
        Test1(ref (int z) = 2);
        Test1(ref (int u));
        Test2(int v);
        Test2((int w));
    }

    static void Test1(ref int x)
    {
    }
    static void Test2(int x)
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (8,19): error CS1510: A ref or out argument must be an assignable variable
    //         Test1(ref (int z) = 2);
    Diagnostic(ErrorCode.ERR_RefLvalueExpected, "(int z) = 2").WithLocation(8, 19),
    // (7,19): error CS0165: Use of unassigned local variable 'y'
    //         Test1(ref int y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "int y").WithArguments("y").WithLocation(7, 19),
    // (9,20): error CS0165: Use of unassigned local variable 'u'
    //         Test1(ref (int u));
    Diagnostic(ErrorCode.ERR_UseDefViolation, "int u").WithArguments("u").WithLocation(9, 20),
    // (10,15): error CS0165: Use of unassigned local variable 'v'
    //         Test2(int v);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "int v").WithArguments("v").WithLocation(10, 15),
    // (11,16): error CS0165: Use of unassigned local variable 'w'
    //         Test2((int w));
    Diagnostic(ErrorCode.ERR_UseDefViolation, "int w").WithArguments("w").WithLocation(11, 16),
    // (8,24): warning CS0219: The variable 'z' is assigned but its value is never used
    //         Test1(ref (int z) = 2);
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "z").WithArguments("z").WithLocation(8, 24)
                );
        }

        [Fact]
        public void OutVar_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out var y);
        Print(y);
    }

    static void Test(out int x)
    {
        x = 123;
    }

    static void Print<T>(T val)
    {
        System.Console.WriteLine(val);
        System.Console.WriteLine(typeof(T));
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"123
System.Int32").VerifyDiagnostics();
        }

        [Fact]
        public void OutVar_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        int x = (var y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,22): error CS0818: Implicitly-typed variables must be initialized
    //         int x = (var y);
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 22),
    // (6,18): error CS0165: Use of unassigned local variable 'y'
    //         int x = (var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 18)
                );
        }

        [Fact]
        public void OutVar_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        int x = (var y) = 1;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,22): error CS0818: Implicitly-typed variables must be initialized
    //         int x = (var y) = 1;
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 22)
                );
        }

        [Fact]
        public void OutVar_04()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(var y);
        byte z = y;
    }

    static void Test(out int x)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,18): error CS0818: Implicitly-typed variables must be initialized
    //         Test(var y);
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 18),
    // (6,14): error CS0165: Use of unassigned local variable 'y'
    //         Test(var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 14)
                );
        }


        [Fact]
        public void OutVar_05()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(ref var y);
        byte z = y;
    }

    static void Test(out int x)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,18): error CS1620: Argument 1 must be passed with the 'out' keyword
    //         Test(ref var y);
    Diagnostic(ErrorCode.ERR_BadArgRef, "var y").WithArguments("1", "out").WithLocation(6, 18),
    // (7,18): error CS0266: Cannot implicitly convert type 'int' to 'byte'. An explicit conversion exists (are you missing a cast?)
    //         byte z = y;
    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y").WithArguments("int", "byte").WithLocation(7, 18),
    // (6,18): error CS0165: Use of unassigned local variable 'y'
    //         Test(ref var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 18)
                );
        }

        [Fact]
        public void OutVar_06()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out var y);
        byte z = y;
    }

    static void Test(int x)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,18): error CS1615: Argument 1 should not be passed with the 'out' keyword
    //         Test(out var y);
    Diagnostic(ErrorCode.ERR_BadArgExtraRef, "var y").WithArguments("1", "out").WithLocation(6, 18),
    // (7,18): error CS0266: Cannot implicitly convert type 'int' to 'byte'. An explicit conversion exists (are you missing a cast?)
    //         byte z = y;
    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y").WithArguments("int", "byte").WithLocation(7, 18)
                );
        }

        [Fact]
        public void OutVar_07()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out var y);
        byte z = y;
    }

    static void Test(ref int x)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,18): error CS1620: Argument 1 must be passed with the 'ref' keyword
    //         Test(out var y);
    Diagnostic(ErrorCode.ERR_BadArgRef, "var y").WithArguments("1", "ref").WithLocation(6, 18),
    // (7,18): error CS0266: Cannot implicitly convert type 'int' to 'byte'. An explicit conversion exists (are you missing a cast?)
    //         byte z = y;
    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y").WithArguments("int", "byte").WithLocation(7, 18)
                );
        }

        [Fact]
        public void OutVar_08()
        {
            var text = @"
public class C
{
    static void Main()
    {
        M(1, __arglist(var x));
        M(1, __arglist(out var y));
        M(1, __arglist(ref var z));
    }
    
    static void M(int x, __arglist)
    {    
    }
}";

            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,28): error CS0818: Implicitly-typed variables must be initialized
    //         M(1, __arglist(var x));
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "x").WithLocation(6, 28),
    // (7,32): error CS0818: Implicitly-typed variables must be initialized
    //         M(1, __arglist(out var y));
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(7, 32),
    // (8,32): error CS0818: Implicitly-typed variables must be initialized
    //         M(1, __arglist(ref var z));
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "z").WithLocation(8, 32),
    // (6,24): error CS0165: Use of unassigned local variable 'x'
    //         M(1, __arglist(var x));
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var x").WithArguments("x").WithLocation(6, 24),
    // (8,28): error CS0165: Use of unassigned local variable 'z'
    //         M(1, __arglist(ref var z));
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var z").WithArguments("z").WithLocation(8, 28));
        }

        [Fact]
        public void OutVar_09()
        {
            var text = @"
using System;
public struct C
{
    static void Main()
    {
        M(__makeref(var i));
        M(__makeref(out var j));
        M(__makeref(ref var k));
    }
    static Type M(TypedReference tr)
    {
        return __reftype(tr);
    }
}";

            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (8,21): error CS1525: Invalid expression term 'out'
    //         M(__makeref(out var j));
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "out").WithArguments("out").WithLocation(8, 21),
    // (8,21): error CS1026: ) expected
    //         M(__makeref(out var j));
    Diagnostic(ErrorCode.ERR_CloseParenExpected, "out").WithLocation(8, 21),
    // (8,21): error CS1003: Syntax error, ',' expected
    //         M(__makeref(out var j));
    Diagnostic(ErrorCode.ERR_SyntaxError, "out").WithArguments(",", "out").WithLocation(8, 21),
    // (8,31): error CS1002: ; expected
    //         M(__makeref(out var j));
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(8, 31),
    // (8,31): error CS1513: } expected
    //         M(__makeref(out var j));
    Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(8, 31),
    // (9,21): error CS1525: Invalid expression term 'ref'
    //         M(__makeref(ref var k));
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(9, 21),
    // (9,21): error CS1026: ) expected
    //         M(__makeref(ref var k));
    Diagnostic(ErrorCode.ERR_CloseParenExpected, "ref").WithLocation(9, 21),
    // (9,21): error CS1003: Syntax error, ',' expected
    //         M(__makeref(ref var k));
    Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",", "ref").WithLocation(9, 21),
    // (9,31): error CS1002: ; expected
    //         M(__makeref(ref var k));
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(9, 31),
    // (9,31): error CS1513: } expected
    //         M(__makeref(ref var k));
    Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(9, 31),
    // (7,25): error CS0818: Implicitly-typed variables must be initialized
    //         M(__makeref(var i));
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "i").WithLocation(7, 25),
    // (7,21): error CS0165: Use of unassigned local variable 'i'
    //         M(__makeref(var i));
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var i").WithArguments("i").WithLocation(7, 21),
    // (9,25): error CS0165: Use of unassigned local variable 'k'
    //         M(__makeref(ref var k));
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var k").WithArguments("k").WithLocation(9, 25)
                );
        }

        [Fact]
        public void OutVar_10()
        {
            var text = @"
using System;
public struct C
{
    static void Main()
    {
    }
}

[MyAttribute(out var a)] class Test1
{}

[MyAttribute(ref var b)] class Test2
{}

[MyAttribute(var c)] class Test3
{}

public class MyAttribute : Attribute
{
    public MyAttribute(out int x)
    {
        x = 0;
    }
}
";

            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (10,14): error CS1041: Identifier expected; 'out' is a keyword
    // [MyAttribute(out var a)] class Test1
    Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "out").WithArguments("", "out").WithLocation(10, 14),
    // (13,14): error CS1041: Identifier expected; 'ref' is a keyword
    // [MyAttribute(ref var b)] class Test2
    Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "ref").WithArguments("", "ref").WithLocation(13, 14),
    // (13,18): error CS8028: A declaration expression is not permitted in a variable-initializer of a field declaration, or in a class-base specification.
    // [MyAttribute(ref var b)] class Test2
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "var b").WithLocation(13, 18),
    // (13,22): error CS0818: Implicitly-typed variables must be initialized
    // [MyAttribute(ref var b)] class Test2
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "b").WithLocation(13, 22),
    // (10,18): error CS8028: A declaration expression is not permitted in a variable-initializer of a field declaration, or in a class-base specification.
    // [MyAttribute(out var a)] class Test1
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "var a").WithLocation(10, 18),
    // (10,22): error CS0818: Implicitly-typed variables must be initialized
    // [MyAttribute(out var a)] class Test1
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "a").WithLocation(10, 22),
    // (16,14): error CS8028: A declaration expression is not permitted in a variable-initializer of a field declaration, or in a class-base specification.
    // [MyAttribute(var c)] class Test3
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "var c").WithLocation(16, 14),
    // (16,18): error CS0818: Implicitly-typed variables must be initialized
    // [MyAttribute(var c)] class Test3
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "c").WithLocation(16, 18)
            );
        }

        [Fact]
        public void OutVar_11()
        {
            var text = @"
using System;
public struct C
{
    static void Main()
    {
    }
}

[MyAttribute(out var a)] class Test1
{}

[MyAttribute(ref var b)] class Test2
{}

[MyAttribute(var c)] class Test3
{}

public class MyAttribute : Attribute
{
    public MyAttribute(ref int x)
    {}
}
";

            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (10,14): error CS1041: Identifier expected; 'out' is a keyword
    // [MyAttribute(out var a)] class Test1
    Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "out").WithArguments("", "out").WithLocation(10, 14),
    // (13,14): error CS1041: Identifier expected; 'ref' is a keyword
    // [MyAttribute(ref var b)] class Test2
    Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "ref").WithArguments("", "ref").WithLocation(13, 14),
    // (13,18): error CS8028: A declaration expression is not permitted in a variable-initializer of a field declaration, or in a class-base specification.
    // [MyAttribute(ref var b)] class Test2
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "var b").WithLocation(13, 18),
    // (13,22): error CS0818: Implicitly-typed variables must be initialized
    // [MyAttribute(ref var b)] class Test2
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "b").WithLocation(13, 22),
    // (10,18): error CS8028: A declaration expression is not permitted in a variable-initializer of a field declaration, or in a class-base specification.
    // [MyAttribute(out var a)] class Test1
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "var a").WithLocation(10, 18),
    // (10,22): error CS0818: Implicitly-typed variables must be initialized
    // [MyAttribute(out var a)] class Test1
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "a").WithLocation(10, 22),
    // (16,14): error CS8028: A declaration expression is not permitted in a variable-initializer of a field declaration, or in a class-base specification.
    // [MyAttribute(var c)] class Test3
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "var c").WithLocation(16, 14),
    // (16,18): error CS0818: Implicitly-typed variables must be initialized
    // [MyAttribute(var c)] class Test3
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "c").WithLocation(16, 18)
                );
        }

        [Fact]
        public void OutVar_12()
        {
            var text = @"
using System;
public struct C
{
    static void Main()
    {
    }
}

[MyAttribute(out var a)] class Test1
{}

[MyAttribute(ref var b)] class Test2
{}

[MyAttribute(var c)] class Test3
{}

public class MyAttribute : Attribute
{
    public MyAttribute(int x)
    {}
}
";

            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (10,14): error CS1041: Identifier expected; 'out' is a keyword
    // [MyAttribute(out var a)] class Test1
    Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "out").WithArguments("", "out").WithLocation(10, 14),
    // (13,14): error CS1041: Identifier expected; 'ref' is a keyword
    // [MyAttribute(ref var b)] class Test2
    Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "ref").WithArguments("", "ref").WithLocation(13, 14),
    // (13,18): error CS8028: A declaration expression is not permitted in a variable-initializer of a field declaration, or in a class-base specification.
    // [MyAttribute(ref var b)] class Test2
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "var b").WithLocation(13, 18),
    // (13,22): error CS0818: Implicitly-typed variables must be initialized
    // [MyAttribute(ref var b)] class Test2
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "b").WithLocation(13, 22),
    // (10,18): error CS8028: A declaration expression is not permitted in a variable-initializer of a field declaration, or in a class-base specification.
    // [MyAttribute(out var a)] class Test1
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "var a").WithLocation(10, 18),
    // (10,22): error CS0818: Implicitly-typed variables must be initialized
    // [MyAttribute(out var a)] class Test1
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "a").WithLocation(10, 22),
    // (16,14): error CS8028: A declaration expression is not permitted in a variable-initializer of a field declaration, or in a class-base specification.
    // [MyAttribute(var c)] class Test3
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "var c").WithLocation(16, 14),
    // (16,18): error CS0818: Implicitly-typed variables must be initialized
    // [MyAttribute(var c)] class Test3
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "c").WithLocation(16, 18)
                );
        }

        [Fact]
        public void OutVar_13()
        {
            var text = @"
public class Cls
{
    public static void Main(dynamic target)
    {
        target.Test(out var y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, references: new[] { CSharpRef, SystemCoreRef }, compOptions: TestOptions.Dll, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,29): error CS0818: Implicitly-typed variables must be initialized
    //         target.Test(out var y);
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 29)
                );
        }

        [Fact]
        public void OutVar_14()
        {
            var text = @"
public class Cls
{
    public static void Main(dynamic target)
    {
        target.Test(ref var y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, references: new[] { CSharpRef, SystemCoreRef }, compOptions: TestOptions.Dll, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,29): error CS0818: Implicitly-typed variables must be initialized
    //         target.Test(ref var y);
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 29),
    // (6,25): error CS0165: Use of unassigned local variable 'y'
    //         target.Test(ref var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 25)
                );
        }

        [Fact]
        public void OutVar_15()
        {
            var text = @"
public class Cls
{
    public static void Main(dynamic target)
    {
        target.Test(var y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, references: new[] { CSharpRef, SystemCoreRef }, compOptions: TestOptions.Dll, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,25): error CS0818: Implicitly-typed variables must be initialized
    //         target.Test(var y);
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 25),
    // (6,21): error CS0165: Use of unassigned local variable 'y'
    //         target.Test(var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 21)
                );
        }

        [Fact]
        public void OutVar_16()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(ref var y);
        byte z = y;
    }

    static void Test(ref int x)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (7,18): error CS0266: Cannot implicitly convert type 'int' to 'byte'. An explicit conversion exists (are you missing a cast?)
    //         byte z = y;
    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y").WithArguments("int", "byte").WithLocation(7, 18),
    // (6,18): error CS0165: Use of unassigned local variable 'y'
    //         Test(ref var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 18)
                );
        }

        [Fact]
        public void OutVar_17()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(ref var y);
        byte z = y;
    }

    static void Test(int x)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,18): error CS1615: Argument 1 should not be passed with the 'ref' keyword
    //         Test(ref var y);
    Diagnostic(ErrorCode.ERR_BadArgExtraRef, "var y").WithArguments("1", "ref").WithLocation(6, 18),
    // (7,18): error CS0266: Cannot implicitly convert type 'int' to 'byte'. An explicit conversion exists (are you missing a cast?)
    //         byte z = y;
    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y").WithArguments("int", "byte").WithLocation(7, 18),
    // (6,18): error CS0165: Use of unassigned local variable 'y'
    //         Test(ref var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 18)
                );
        }

        [Fact]
        public void OutVar_18()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(var y);
        byte z = y;
    }

    static void Test(ref int x)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,18): error CS0818: Implicitly-typed variables must be initialized
    //         Test(var y);
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 18),
    // (6,14): error CS0165: Use of unassigned local variable 'y'
    //         Test(var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 14)
                );
        }

        [Fact]
        public void OutVar_19()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(var y);
        byte z = y;
    }

    static void Test(int x)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,18): error CS0818: Implicitly-typed variables must be initialized
    //         Test(var y);
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 18),
    // (6,14): error CS0165: Use of unassigned local variable 'y'
    //         Test(var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 14)
                );
        }

        [Fact]
        public void OutVar_20()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out var y);
        byte z = y;
    }

    static void Test(int x)
    {}

    static void Test(string x)
    {}
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,18): error CS1615: Argument 1 should not be passed with the 'out' keyword
    //         Test(out var y);
    Diagnostic(ErrorCode.ERR_BadArgExtraRef, "var y").WithArguments("1", "out").WithLocation(6, 18)
                );
        }

        [Fact]
        public void OutVar_21()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out var y);
        byte z = y;
    }

    static void Test(out int x)
    {
        x = 0;
    }

    static void Test(out string x)
    {
        x = null;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'Cls.Test(out int)' and 'Cls.Test(out string)'
    //         Test(out var y);
    Diagnostic(ErrorCode.ERR_AmbigCall, "Test").WithArguments("Cls.Test(out int)", "Cls.Test(out string)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void OutVar_22()
        {
            var text = @"
using System;

public class Cls
{
    public static void Main()
    {
        var x = new Action(out var y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (8,32): error CS0149: Method name expected
    //         var x = new Action(out var y);
    Diagnostic(ErrorCode.ERR_MethodNameExpected, "var y").WithLocation(8, 32),
    // (8,32): error CS0165: Use of unassigned local variable 'y'
    //         var x = new Action(out var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(8, 32)
                );
        }

        [Fact]
        public void OutVar_23()
        {
            var text = @"
using System;

public class Cls
{
    public static void Main()
    {
        var x = new Action(Main, out var y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (8,28): error CS0149: Method name expected
    //         var x = new Action(Main, out var y);
    Diagnostic(ErrorCode.ERR_MethodNameExpected, "Main, out var y").WithLocation(8, 28),
    // (8,38): error CS0165: Use of unassigned local variable 'y'
    //         var x = new Action(Main, out var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(8, 38)
                );
        }

        [Fact]
        public void OutVar_24()
        {
            var text = @"
public class Cls
{
    public static void Main(dynamic target)
    {
        var x = new Test(target, out var y);
    }
}

class Test
{
    public Test(int x, out int y)
    {
        y = 0;
    }

    public Test(uint x, out int y)
    {
        y = 1;
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text, references: new[] { CSharpRef, SystemCoreRef }, compOptions: TestOptions.Dll, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,42): error CS0818: Implicitly-typed variables must be initialized
    //         var x = new Test(target, out var y);
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 42)
                );
        }

        [Fact]
        public void OutVar_25()
        {
            var text = @"
public class Cls
{
    public static void Main(int [,] target)
    {
        target[var y] = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,20): error CS0818: Implicitly-typed variables must be initialized
    //         target[var y] = 0;
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 20),
    // (6,16): error CS0165: Use of unassigned local variable 'y'
    //         target[var y] = 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 16)
                );
        }

        [Fact]
        public void OutVar_26()
        {
            var text = @"
public class Cls
{
    public static void Main(int [] target)
    {
        target[var y] = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,20): error CS0818: Implicitly-typed variables must be initialized
    //         target[var y] = 0;
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 20),
    // (6,16): error CS0165: Use of unassigned local variable 'y'
    //         target[var y] = 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 16)
                );
        }

        [Fact]
        public void OutVar_27()
        {
            var text = @"
public class Cls
{
    public static void Main(int [] target)
    {
        target[out var y] = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,24): error CS0818: Implicitly-typed variables must be initialized
    //         target[out var y] = 0;
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 24),
    // (6,20): error CS0165: Use of unassigned local variable 'y'
    //         target[out var y] = 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 20)
                );
        }

        [Fact]
        public void OutVar_28()
        {
            var text = @"
public class Cls
{
    public static void Main(int [,] target)
    {
        target[out var y] = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,9): error CS0022: Wrong number of indices inside []; expected 2
    //         target[out var y] = 0;
    Diagnostic(ErrorCode.ERR_BadIndexCount, "target[out var y]").WithArguments("2").WithLocation(6, 9),
    // (6,20): error CS0165: Use of unassigned local variable 'y'
    //         target[out var y] = 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 20)
                );
        }


        [Fact]
        public void OutVar_29()
        {
            var text = @"
public class Cls
{
    public unsafe static void Main(int* target)
    {
        target[var y, 3] = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll.WithAllowUnsafe(true), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,20): error CS0818: Implicitly-typed variables must be initialized
    //         target[var y, 3] = 0;
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 20),
    // (6,16): error CS0165: Use of unassigned local variable 'y'
    //         target[var y, 3] = 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 16)
                );
        }

        [Fact]
        public void OutVar_30()
        {
            var text = @"
public class Cls
{
    public unsafe static void Main(int* target)
    {
        target[var y] = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll.WithAllowUnsafe(true), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,20): error CS0818: Implicitly-typed variables must be initialized
    //         target[var y] = 0;
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 20),
    // (6,16): error CS0165: Use of unassigned local variable 'y'
    //         target[var y] = 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 16)
                );
        }

        [Fact]
        public void OutVar_31()
        {
            var text = @"
public class Cls
{
    public unsafe static void Main(int * target)
    {
        target[out var y] = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll.WithAllowUnsafe(true), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,20): error CS1615: Argument 1 should not be passed with the 'out' keyword
    //         target[out var y] = 0;
    Diagnostic(ErrorCode.ERR_BadArgExtraRef, "var y").WithArguments("1", "out").WithLocation(6, 20),
    // (6,24): error CS0818: Implicitly-typed variables must be initialized
    //         target[out var y] = 0;
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 24),
    // (6,20): error CS0165: Use of unassigned local variable 'y'
    //         target[out var y] = 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 20)
                );
        }

        [Fact]
        public void OutVar_32()
        {
            var text = @"
public class Cls
{
    public unsafe static void Main(int * target)
    {
        target[out var y, 1] = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll.WithAllowUnsafe(true), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,20): error CS1615: Argument 1 should not be passed with the 'out' keyword
    //         target[out var y, 1] = 0;
    Diagnostic(ErrorCode.ERR_BadArgExtraRef, "var y").WithArguments("1", "out").WithLocation(6, 20),
    // (6,20): error CS0165: Use of unassigned local variable 'y'
    //         target[out var y, 1] = 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 20)
                );
        }

        [Fact]
        public void OutVar_33()
        {
            var text = @"
public class Cls
{
    public unsafe static void Main(int* target)
    {
        target[4, var y] = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll.WithAllowUnsafe(true), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,23): error CS0818: Implicitly-typed variables must be initialized
    //         target[4, var y] = 0;
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 23),
    // (6,19): error CS0165: Use of unassigned local variable 'y'
    //         target[4, var y] = 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 19)
                );
        }

        [Fact]
        public void OutVar_34()
        {
            var text = @"
public class Cls
{
    public unsafe static void Main(int * target)
    {
        target[5, out var y] = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll.WithAllowUnsafe(true), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,23): error CS1615: Argument 2 should not be passed with the 'out' keyword
    //         target[5, out var y] = 0;
    Diagnostic(ErrorCode.ERR_BadArgExtraRef, "var y").WithArguments("2", "out").WithLocation(6, 23),
    // (6,23): error CS0165: Use of unassigned local variable 'y'
    //         target[5, out var y] = 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 23)
                );
        }

        [Fact]
        public void OutVar_35()
        {
            var text = @"
public class Cls
{
    public static void Main(dynamic target)
    {
        var x = target[out var y];
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, references: new[] { CSharpRef, SystemCoreRef }, compOptions: TestOptions.Dll, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,32): error CS0818: Implicitly-typed variables must be initialized
    //         var x = target[out var y];
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 32)
                );
        }

        [Fact]
        public void OutVar_36()
        {
            var text = @"
public class Cls
{
    public static void Main(dynamic target)
    {
        var x = target[var y];
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, references: new[] { CSharpRef, SystemCoreRef }, compOptions: TestOptions.Dll, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,28): error CS0818: Implicitly-typed variables must be initialized
    //         var x = target[var y];
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 28),
    // (6,24): error CS0165: Use of unassigned local variable 'y'
    //         var x = target[var y];
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 24)
                );
        }

        [Fact]
        public void OutVar_37()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out var y);
    }

    static void Test(out int x)
    {
        x = 123;
    }

    static void Test(out uint x)
    {
        x = 456;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'Cls.Test(out int)' and 'Cls.Test(out uint)'
    //         Test(out var y);
    Diagnostic(ErrorCode.ERR_AmbigCall, "Test").WithArguments("Cls.Test(out int)", "Cls.Test(out uint)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void OutVar_38()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out var y);
    }

    static void Test<T>(out T x)
    {
        x = default(T);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,9): error CS0411: The type arguments for method 'Cls.Test<T>(out T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //         Test(out var y);
    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test").WithArguments("Cls.Test<T>(out T)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void OutVar_39()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out var y, 1);
        Print(y);
    }

    static void Test<T>(out T x, T y)
    {
        x = default(T);
    }

    static void Print<T>(T val)
    {
        System.Console.WriteLine(typeof(T));
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"System.Int32").VerifyDiagnostics();
        }

        [Fact]
        public void OutVar_40()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(null, out var y);
    }

    static void Test(A a, out int x)
    {
        x = 123;
    }

    static void Test(B b, out int x)
    {
        x = 456;
    }
}

class A{}
class B{}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'Cls.Test(A, out int)' and 'Cls.Test(B, out int)'
    //         Test(null, out var y);
    Diagnostic(ErrorCode.ERR_AmbigCall, "Test").WithArguments("Cls.Test(A, out int)", "Cls.Test(B, out int)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void OutVar_41()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out var y, y + 1);
    }

    static void Test(out int x, int y)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,25): error CS8029: Reference to variable 'y' is not permitted in this context.
    //         Test(out var y, y + 1);
    Diagnostic(ErrorCode.ERR_VariableUsedInTheSameArgumentList, "y").WithArguments("y").WithLocation(6, 25),
    // (6,25): error CS0165: Use of unassigned local variable 'y'
    //         Test(out var y, y + 1);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(6, 25)
                );
        }

        [Fact]
        public void OutVar_42()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(y + 1, out var y);
    }

    static void Test(int y, out int x)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,14): error CS0841: Cannot use local variable 'y' before it is declared
    //         Test(y + 1, out var y);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y").WithArguments("y").WithLocation(6, 14)
                );
        }

        [Fact]
        public void OutVar_43()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out var y), y);
    }

    static int Test1(out int x)
    {
        x = 123;
        return x + 1;
    }

    static void Test2(int x, int y)
    {
        System.Console.WriteLine(x);
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"124
123").VerifyDiagnostics();
        }

        [Fact]
        public void OutVar_44()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(y, Test1(out var y));
    }

    static int Test1(out int x)
    {
        x = 123;
        return x + 1;
    }

    static void Test2(int x, int y)
    {
        System.Console.WriteLine(x);
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,15): error CS0841: Cannot use local variable 'y' before it is declared
    //         Test2(y, Test1(out var y));
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y").WithArguments("y").WithLocation(6, 15)
                );
        }

        [Fact]
        public void CatchFilter_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.Exception e) if ((int j = e is System.NullReferenceException ? 1 : 2) + j == 2)
        {
            System.Console.WriteLine(j);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.Exception e) if ((int j = e is System.NullReferenceException ? 3 : 2) + j == 6)
        {
            System.Func<object> l = () => j;
            System.Console.WriteLine(l());
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.Exception e) if ((int j = e is System.NullReferenceException ? 5 : 2) + j == 10)
        {
            System.Func<object> l = () => e.GetType();
            System.Console.WriteLine(l());
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.Exception e) if ((int j = e is System.NullReferenceException ? 7 : 2) + j == 14)
        {
            System.Func<object> l1 = () => j;
            System.Func<object> l2 = () => e.GetType();
            System.Console.WriteLine(l1());
            System.Console.WriteLine(l2());
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException e) if ((int j = 9) == 9)
        {
            System.Console.WriteLine(j);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if ((int j = 11) == 11)
        {
            System.Console.WriteLine(j);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if ((int j = 13) == 13)
        {
            System.Func<object> l = () => j;
            System.Console.WriteLine(l());
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"1
3
System.NullReferenceException
7
System.NullReferenceException
9
11
13").VerifyDiagnostics(
    // (51,46): warning CS0168: The variable 'e' is declared but never used
    //         catch (System.NullReferenceException e) if ((int j = 9) == 9)
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e").WithLocation(51, 46)
                );
        }

        [Fact]
        public void CatchFilter_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException e) if (bool a = false)
        {
            System.Console.WriteLine(e);
        }

        System.Console.WriteLine(a);

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException e) if (bool b = false)
        {
            System.Console.WriteLine(e);
        }
        catch (System.Exception e) if (b)
        {
            System.Console.WriteLine(e);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException e) if (bool c = false)
        {
            System.Console.WriteLine(e);
        }
        catch (System.Exception e) 
        {
            System.Console.WriteLine(e);
            System.Console.WriteLine(c);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException e) if (bool d = false)
        {
            System.Console.WriteLine(e);
        }
        catch
        {
            System.Console.WriteLine(d);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException e)
        {
            System.Console.WriteLine(e);
            System.Console.WriteLine(f);
        }
        catch (System.Exception e) if (bool f = false)
        {
            System.Console.WriteLine(e);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException e) if (bool g = false)
        {
            System.Console.WriteLine(e);
        }
        finally
        {
            System.Console.WriteLine(g);
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (15,34): error CS0103: The name 'a' does not exist in the current context
    //         System.Console.WriteLine(a);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(15, 34),
    // (25,40): error CS0103: The name 'b' does not exist in the current context
    //         catch (System.Exception e) if (b)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(25, 40),
    // (41,38): error CS0103: The name 'c' does not exist in the current context
    //             System.Console.WriteLine(c);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "c").WithArguments("c").WithLocation(41, 38),
    // (54,38): error CS0103: The name 'd' does not exist in the current context
    //             System.Console.WriteLine(d);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "d").WithArguments("d").WithLocation(54, 38),
    // (64,38): error CS0103: The name 'f' does not exist in the current context
    //             System.Console.WriteLine(f);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "f").WithArguments("f").WithLocation(64, 38),
    // (81,38): error CS0103: The name 'g' does not exist in the current context
    //             System.Console.WriteLine(g);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "g").WithArguments("g").WithLocation(81, 38)
            );
        }

        [Fact]
        public void CatchFilter_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if (bool a = false)
        {
        }

        System.Console.WriteLine(a);

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if (bool b = false)
        {
        }
        catch (System.Exception) if (b)
        {
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if (bool c = false)
        {
        }
        catch (System.Exception) 
        {
            System.Console.WriteLine(c);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if (bool d = false)
        {
        }
        catch
        {
            System.Console.WriteLine(d);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException)
        {
            System.Console.WriteLine(f);
        }
        catch (System.Exception) if (bool f = false)
        {
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if (bool g = false)
        {
        }
        finally
        {
            System.Console.WriteLine(g);
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (14,34): error CS0103: The name 'a' does not exist in the current context
    //         System.Console.WriteLine(a);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(14, 34),
    // (23,38): error CS0103: The name 'b' does not exist in the current context
    //         catch (System.Exception) if (b)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(23, 38),
    // (36,38): error CS0103: The name 'c' does not exist in the current context
    //             System.Console.WriteLine(c);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "c").WithArguments("c").WithLocation(36, 38),
    // (48,38): error CS0103: The name 'd' does not exist in the current context
    //             System.Console.WriteLine(d);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "d").WithArguments("d").WithLocation(48, 38),
    // (57,38): error CS0103: The name 'f' does not exist in the current context
    //             System.Console.WriteLine(f);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "f").WithArguments("f").WithLocation(57, 38),
    // (72,38): error CS0103: The name 'g' does not exist in the current context
    //             System.Console.WriteLine(g);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "g").WithArguments("g").WithLocation(72, 38)
            );
        }

        [Fact]
        public void CatchFilter_04()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        int a1 = 0;
        System.Console.WriteLine(a1);
        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException e) if (bool a1 = false)
        {
            System.Console.WriteLine(e);
        }

        int a2 = 0;
        System.Console.WriteLine(a2);
        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException)
        {
            System.Console.WriteLine(int a2 = 1);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException b1) if (bool b1 = false)
        {
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException b2)
        {
            System.Console.WriteLine(int b2 = 0);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if ((bool c1 = false) && (bool c1 = false))
        {
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if (bool c2 = false) 
        {
            System.Console.WriteLine(int c2 = 1);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if (bool c3 = false) 
        {
            int c3 = 1;
            System.Console.WriteLine(c3);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException)
        {
            System.Console.WriteLine((bool d1 = false) && (bool d1 = false));
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (12,58): error CS0136: A local or parameter named 'a1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         catch (System.NullReferenceException e) if (bool a1 = false)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a1").WithArguments("a1").WithLocation(12, 58),
    // (25,42): error CS0136: A local or parameter named 'a2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a2").WithArguments("a2").WithLocation(25, 42),
    // (32,59): error CS0128: A local variable named 'b1' is already defined in this scope
    //         catch (System.NullReferenceException b1) if (bool b1 = false)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "b1").WithArguments("b1").WithLocation(32, 59),
    // (42,42): error CS0136: A local or parameter named 'b2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int b2 = 0);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "b2").WithArguments("b2").WithLocation(42, 42),
    // (49,78): error CS0128: A local variable named 'c1' is already defined in this scope
    //         catch (System.NullReferenceException) if ((bool c1 = false) && (bool c1 = false))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c1").WithArguments("c1").WithLocation(49, 78),
    // (59,42): error CS0136: A local or parameter named 'c2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c2").WithArguments("c2").WithLocation(59, 42),
    // (68,17): error CS0136: A local or parameter named 'c3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             int c3 = 1;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c3").WithArguments("c3").WithLocation(68, 17),
    // (78,65): error CS0128: A local variable named 'd1' is already defined in this scope
    //             System.Console.WriteLine((bool d1 = false) && (bool d1 = false));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "d1").WithArguments("d1").WithLocation(78, 65),
    // (32,46): warning CS0168: The variable 'b1' is declared but never used
    //         catch (System.NullReferenceException b1) if (bool b1 = false)
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "b1").WithArguments("b1").WithLocation(32, 46),
    // (40,46): warning CS0168: The variable 'b2' is declared but never used
    //         catch (System.NullReferenceException b2)
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "b2").WithArguments("b2").WithLocation(40, 46));
        }

        [Fact]
        public void CatchFilter_05()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException b1) if (bool b1 = false)
        {
            System.Console.WriteLine(b1);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if ((bool c1 = false) && (bool c1 = false))
        {
            System.Console.WriteLine(c1);
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (10,59): error CS0128: A local variable named 'b1' is already defined in this scope
    //         catch (System.NullReferenceException b1) if (bool b1 = false)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "b1").WithArguments("b1").WithLocation(10, 59),
    // (19,78): error CS0128: A local variable named 'c1' is already defined in this scope
    //         catch (System.NullReferenceException) if ((bool c1 = false) && (bool c1 = false))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c1").WithArguments("c1").WithLocation(19, 78)
                );
        }

        [Fact, WorkItem(42)]
        public void BugCodePlex_42_1()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        int.TryParse(""10"", out var n0);
        int.TryParse(""20"", out int n0); // Collision

        int.TryParse(""10"", out int n1);
        int.TryParse(""20"", out var n1); // Collision

        int.TryParse(""10"", out var n2);
        int.TryParse(""20"", out var n2); // Collision

        int n3 = 0;
        System.Console.WriteLine(n3);
        int.TryParse(""10"", out var n3); // Collision

        System.Console.WriteLine(int n4 = 0);
        int.TryParse(""10"", out var n4); // Collision

        int.TryParse(""10"", out var n5);
        System.Console.WriteLine(int n5 = 0); // Collision

        int.TryParse(""10"", out var n6);
        int n6 = 0; // Collision
        System.Console.WriteLine(n6);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (7,36): error CS0128: A local variable named 'n0' is already defined in this scope
    //         int.TryParse("20", out int n0); // Collision
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "n0").WithArguments("n0").WithLocation(7, 36),
    // (10,36): error CS0128: A local variable named 'n1' is already defined in this scope
    //         int.TryParse("20", out var n1); // Collision
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "n1").WithArguments("n1").WithLocation(10, 36),
    // (13,36): error CS0128: A local variable named 'n2' is already defined in this scope
    //         int.TryParse("20", out var n2); // Collision
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "n2").WithArguments("n2").WithLocation(13, 36),
    // (17,36): error CS0128: A local variable named 'n3' is already defined in this scope
    //         int.TryParse("10", out var n3); // Collision
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "n3").WithArguments("n3").WithLocation(17, 36),
    // (20,36): error CS0128: A local variable named 'n4' is already defined in this scope
    //         int.TryParse("10", out var n4); // Collision
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "n4").WithArguments("n4").WithLocation(20, 36),
    // (23,38): error CS0128: A local variable named 'n5' is already defined in this scope
    //         System.Console.WriteLine(int n5 = 0); // Collision
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "n5").WithArguments("n5").WithLocation(23, 38),
    // (26,13): error CS0128: A local variable named 'n6' is already defined in this scope
    //         int n6 = 0; // Collision
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "n6").WithArguments("n6").WithLocation(26, 13),
    // (26,13): warning CS0219: The variable 'n6' is assigned but its value is never used
    //         int n6 = 0; // Collision
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "n6").WithArguments("n6").WithLocation(26, 13)
                );
        }

        [Fact, WorkItem(42)]
        public void BugCodePlex_42_2()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var n7 = 10;
        if (int.TryParse(""20"", out var n7))
        {
            System.Console.WriteLine(n7); // <-- The variable 'n7' cannot be used in this local scope because that name has been used in an enclosing scope to refer to variable 'n2'
        }
        System.Console.WriteLine(n7);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (7,40): error CS0136: A local or parameter named 'n7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         if (int.TryParse("20", out var n7))
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "n7").WithArguments("n7").WithLocation(7, 40)
                );
        }

        [Fact]
        public void RestrictedType()
        {
            var source = @"
using System.Threading.Tasks;
using System;

class Test
{
    static void Main() {}

    async Task M1()
    {
        M1(out TypedReference x);
        M1(out var y);
        await Task.Factory.StartNew(() => { });
    }

    void M1(out TypedReference tr)
    {
        tr = default(TypedReference);
    }

    Task M2()
    {
        M1(out TypedReference x);
        M1(out var y);
        return null;
    }
}";
            CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental)).VerifyDiagnostics(
    // (16,13): error CS1601: Cannot make reference to variable of type 'System.TypedReference'
    //     void M1(out TypedReference tr)
    Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "out TypedReference tr").WithArguments("System.TypedReference").WithLocation(16, 13),
    // (11,16): error CS4012: Parameters or locals of type 'System.TypedReference' cannot be declared in async methods or lambda expressions.
    //         M1(out TypedReference x);
    Diagnostic(ErrorCode.ERR_BadSpecialByRefLocal, "TypedReference").WithArguments("System.TypedReference").WithLocation(11, 16),
    // (12,16): error CS4012: Parameters or locals of type 'System.TypedReference' cannot be declared in async methods or lambda expressions.
    //         M1(out var y);
    Diagnostic(ErrorCode.ERR_BadSpecialByRefLocal, "var").WithArguments("System.TypedReference").WithLocation(12, 16)
                );
        }

        [Fact]
        public void BugCodePlex_18_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var x = (int[] y = { }) = new [] { 1 };
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation).VerifyDiagnostics().VerifyIL("Cls.Main",
@"{
  // Code size       21 (0x15)
  .maxstack  4
  .locals init (int[] V_0, //x
  int[] V_1) //y
  IL_0000:  ldc.i4.0
  IL_0001:  newarr     ""int""
  IL_0006:  stloc.1
  IL_0007:  ldc.i4.1
  IL_0008:  newarr     ""int""
  IL_000d:  dup
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.1
  IL_0010:  stelem.i4
  IL_0011:  dup
  IL_0012:  stloc.1
  IL_0013:  stloc.0
  IL_0014:  ret
}");
        }

        [Fact]
        public void BugCodePlex_18_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var x = int[] y = { } = null;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,31): error CS1002: ; expected
    //         var x = int[] y = { } = null;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "=").WithLocation(6, 31),
    // (6,31): error CS1525: Invalid expression term '='
    //         var x = int[] y = { } = null;
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "=").WithArguments("=").WithLocation(6, 31)
                );
        }

        [Fact]
        public void BugCodePlex_18_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var x = var y = var z = 1;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation).VerifyDiagnostics().VerifyIL("Cls.Main",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0, //x
  int V_1, //y
  int V_2) //z
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.2
  IL_0002:  ldloc.2
  IL_0003:  stloc.1
  IL_0004:  ldloc.1
  IL_0005:  stloc.0
  IL_0006:  ret
}");
        }

        [Fact]
        public void BugCodePlex_18_04()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var x = (object) int[] y = { };
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,26): error CS1525: Invalid expression term 'int'
    //         var x = (object) int[] y = { };
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(6, 26),
    // (6,30): error CS0443: Syntax error; value expected
    //         var x = (object) int[] y = { };
    Diagnostic(ErrorCode.ERR_ValueExpected, "]").WithLocation(6, 30),
    // (6,32): error CS1002: ; expected
    //         var x = (object) int[] y = { };
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "y").WithLocation(6, 32),
    // (6,36): error CS1525: Invalid expression term '{'
    //         var x = (object) int[] y = { };
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(6, 36),
    // (6,36): error CS1002: ; expected
    //         var x = (object) int[] y = { };
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(6, 36),
    // (6,32): error CS0103: The name 'y' does not exist in the current context
    //         var x = (object) int[] y = { };
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(6, 32)
                );
        }

        [Fact]
        public void BugCodePlex_18_05()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var x = int[] y = { } ? 1 : 2;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,31): error CS1003: Syntax error, ',' expected
    //         var x = int[] y = { } ? 1 : 2;
    Diagnostic(ErrorCode.ERR_SyntaxError, "?").WithArguments(",", "?").WithLocation(6, 31),
    // (6,33): error CS1002: ; expected
    //         var x = int[] y = { } ? 1 : 2;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "1").WithLocation(6, 33),
    // (6,35): error CS1002: ; expected
    //         var x = int[] y = { } ? 1 : 2;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(6, 35),
    // (6,35): error CS1513: } expected
    //         var x = int[] y = { } ? 1 : 2;
    Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(6, 35),
    // (6,37): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //         var x = int[] y = { } ? 1 : 2;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "2").WithLocation(6, 37)
                );
        }

        [Fact]
        public void BugCodePlex_18_06()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var x = int[] y = { } ?? new int[] { 1 };
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,31): error CS1002: ; expected
    //         var x = int[] y = { } ?? new int[] { 1 };
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "??").WithLocation(6, 31),
    // (6,31): error CS1525: Invalid expression term '??'
    //         var x = int[] y = { } ?? new int[] { 1 };
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "??").WithArguments("??").WithLocation(6, 31)
                );
        }

        [Fact]
        public void BugCodePlex_18_07()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var x = int y++;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,22): error CS1002: ; expected
    //         var x = int y++;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "++").WithLocation(6, 22),
    // (6,24): error CS1525: Invalid expression term ';'
    //         var x = int y++;
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 24),
    // (6,17): error CS0165: Use of unassigned local variable 'y'
    //         var x = int y++;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "int y").WithArguments("y").WithLocation(6, 17)
                );
        }

        [Fact]
        public void BugCodePlex_18_08()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var x = ++ int y;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,20): error CS1525: Invalid expression term 'int'
    //         var x = ++ int y;
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(6, 20),
    // (6,24): error CS1002: ; expected
    //         var x = ++ int y;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "y").WithLocation(6, 24),
    // (6,24): error CS0103: The name 'y' does not exist in the current context
    //         var x = ++ int y;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(6, 24),
    // (6,24): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //         var x = ++ int y;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "y").WithLocation(6, 24)
                );
        }

        [Fact]
        public void BugCodePlex_18_09()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {}

    public async void Test()
    {
        var x = await int y = 2;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (9,23): error CS1525: Invalid expression term 'int'
    //         var x = await int y = 2;
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(9, 23),
    // (9,27): error CS1002: ; expected
    //         var x = await int y = 2;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "y").WithLocation(9, 27),
    // (9,27): error CS0103: The name 'y' does not exist in the current context
    //         var x = await int y = 2;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(9, 27)
                );
        }

        [Fact]
        public void BugCodePlex_18_10()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var x = 3 + int y = 2;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (6,21): error CS1525: Invalid expression term 'int'
    //         var x = 3 + int y = 2;
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(6, 21),
    // (6,25): error CS1002: ; expected
    //         var x = 3 + int y = 2;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "y").WithLocation(6, 25),
    // (6,25): error CS0103: The name 'y' does not exist in the current context
    //         var x = 3 + int y = 2;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(6, 25)
                );
        }

        [Fact]
        public void BugCodePlex_18_11()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        int[] y = { };
        var x = y ?? int[] z = { 1 };
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (7,22): error CS1525: Invalid expression term 'int'
    //         var x = y ?? int[] z = { 1 };
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(7, 22),
    // (7,26): error CS0443: Syntax error; value expected
    //         var x = y ?? int[] z = { 1 };
    Diagnostic(ErrorCode.ERR_ValueExpected, "]").WithLocation(7, 26),
    // (7,28): error CS1002: ; expected
    //         var x = y ?? int[] z = { 1 };
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "z").WithLocation(7, 28),
    // (7,32): error CS1525: Invalid expression term '{'
    //         var x = y ?? int[] z = { 1 };
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(7, 32),
    // (7,32): error CS1002: ; expected
    //         var x = y ?? int[] z = { 1 };
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(7, 32),
    // (7,36): error CS1002: ; expected
    //         var x = y ?? int[] z = { 1 };
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(7, 36),
    // (7,28): error CS0103: The name 'z' does not exist in the current context
    //         var x = y ?? int[] z = { 1 };
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z").WithArguments("z").WithLocation(7, 28)
                );
        }

        [Fact, WorkItem(2)]
        public void InLambda_01()
        {
            var text = @"
using System;
using System.Collections.Generic;

class C
{
    static void Main()
    {
        Action a = () => new Dictionary<int, int>().TryGetValue(0, out int value);
        Action b = () => new Dictionary<int, int>().TryGetValue(0, out int value);
        Action c = delegate  
                   {
                       new Dictionary<int, int>().TryGetValue(0, out int value);
                   };
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation).VerifyDiagnostics();
        }

        [Fact, WorkItem(2)]
        public void InLambda_02()
        {
            var text = @"
using System;
using System.Collections.Generic;

class C
{
    static void Main()
    {
        Func<int, bool> a = key => new Dictionary<int, int>().TryGetValue(key, out int value);
        Func<int, bool> b = key => new Dictionary<int, int>().TryGetValue(key, out int value);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation).VerifyDiagnostics();
        }

        [Fact, WorkItem(2)]
        public void InLambda_03()
        {
            var text = @"
using System;

class C
{
    static void Main()
    {
        Func<bool> f = () => int.TryParse(""42"", out int y);
        Console.WriteLine(y); 

        Func<bool> g = () => { return int.TryParse(""42"", out int z); };
        Console.WriteLine(z); 

        Func<bool> h = delegate { return int.TryParse(""42"", out int w); };
        Console.WriteLine(w); 
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (9,27): error CS0103: The name 'y' does not exist in the current context
    //         Console.WriteLine(y); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(9, 27),
    // (12,27): error CS0103: The name 'z' does not exist in the current context
    //         Console.WriteLine(z); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z").WithArguments("z").WithLocation(12, 27),
    // (15,27): error CS0103: The name 'w' does not exist in the current context
    //         Console.WriteLine(w); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "w").WithArguments("w").WithLocation(15, 27)
                );
        }

        [Fact, WorkItem(2)]
        public void InLambda_04()
        {
            var text = @"
using System;

class C
{
    static void Main()
    {
        Func<string, bool> f = str => int.TryParse(str, out int y);
        Console.WriteLine(y); 

        Func<string, bool> g = str => { return int.TryParse(str, out int z); };
        Console.WriteLine(z); 
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (9,27): error CS0103: The name 'y' does not exist in the current context
    //         Console.WriteLine(y); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(9, 27),
    // (12,27): error CS0103: The name 'z' does not exist in the current context
    //         Console.WriteLine(z); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z").WithArguments("z").WithLocation(12, 27)
                );
        }

        [Fact, WorkItem(2)]
        public void InFrom_01()
        {
            var text = @"
using System.Linq;

class C
{
    static void Main()
    {
        var res = from x in new[] { int.TryParse(""42"", out int y) }
                  select x;

        System.Console.WriteLine(y); 
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, new[] { SystemCoreRef }, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"42").VerifyDiagnostics();
        }

        [Fact, WorkItem(2)]
        public void InFrom_02()
        {
            var text = @"
using System.Linq;

class C
{
    static void Main()
    {
        var res = from x1 in new[] {""41"", ""42"", ""43""}
                  from x2 in new[] { int.TryParse(x1, out int y) && y == 42 }
                  select new { x1, x2 };

        foreach (var item in res)
        {
            System.Console.WriteLine(item); 
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, new[] { SystemCoreRef }, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"{ x1 = 41, x2 = False }
{ x1 = 42, x2 = True }
{ x1 = 43, x2 = False }").VerifyDiagnostics();
        }

        [Fact, WorkItem(2)]
        public void InFrom_03()
        {
            var text = @"
using System.Linq;

class C
{
    static void Main()
    {
        var res = from x1 in new[] {""41"", ""42"", ""43""}
                  from x2 in new[] { int.TryParse(x1, out int y) }
                  select y;

        System.Console.WriteLine(y); 
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, new[] { SystemCoreRef }, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (10,26): error CS0103: The name 'y' does not exist in the current context
    //                   select y;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(10, 26),
    // (12,34): error CS0103: The name 'y' does not exist in the current context
    //         System.Console.WriteLine(y); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(12, 34)
                );
        }

        [Fact, WorkItem(2)]
        public void InFrom_04()
        {
            var text = @"
using System.Linq;

class C
{
    static void Main()
    {
        var res = from bool x in new[] { int.TryParse(""42"", out int y) }
                  select x;

        System.Console.WriteLine(y); 
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, new[] { SystemCoreRef }, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"42").VerifyDiagnostics();
        }

        [Fact, WorkItem(2)]
        public void InFrom_05()
        {
            var text = @"
using System.Linq;

class C
{
    static void Main()
    {
        var res = from x1 in new[] {""41"", ""42"", ""43""}
                  from bool x2 in new[] { int.TryParse(x1, out int y) && y == 42 }
                  select new { x1, x2 };

        foreach (var item in res)
        {
            System.Console.WriteLine(item); 
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, new[] { SystemCoreRef }, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"{ x1 = 41, x2 = False }
{ x1 = 42, x2 = True }
{ x1 = 43, x2 = False }").VerifyDiagnostics();
        }

        [Fact, WorkItem(2)]
        public void InFrom_06()
        {
            var text = @"
using System.Linq;

class C
{
    static void Main()
    {
        var res = from x1 in new[] {""41"", ""42"", ""43""}
                  from bool x2 in new[] { int.TryParse(x1, out int y) }
                  select y;

        System.Console.WriteLine(y); 
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, new[] { SystemCoreRef }, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (10,26): error CS0103: The name 'y' does not exist in the current context
    //                   select y;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(10, 26),
    // (12,34): error CS0103: The name 'y' does not exist in the current context
    //         System.Console.WriteLine(y); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(12, 34)
                );
        }

        [Fact, WorkItem(2)]
        public void InLet_01()
        {
            var text = @"
using System.Linq;

class C
{
    static void Main()
    {
        var res = from x1 in new[] {""41"", ""42"", ""43""}
                  let x2 = int.TryParse(x1, out int y) && y == 42 
                  select new { x1, x2 };

        foreach (var item in res)
        {
            System.Console.WriteLine(item); 
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, new[] { SystemCoreRef }, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"{ x1 = 41, x2 = False }
{ x1 = 42, x2 = True }
{ x1 = 43, x2 = False }").VerifyDiagnostics();
        }

        [Fact, WorkItem(2)]
        public void InLet_02()
        {
            var text = @"
using System.Linq;

class C
{
    static void Main()
    {
        var res = from x1 in new[] {""41"", ""42"", ""43""}
                  let x2 = int.TryParse(x1, out int y) && y == 42 
                  select y;

        System.Console.WriteLine(y); 
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, new[] { SystemCoreRef }, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (10,26): error CS0103: The name 'y' does not exist in the current context
    //                   select y;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(10, 26),
    // (12,34): error CS0103: The name 'y' does not exist in the current context
    //         System.Console.WriteLine(y); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(12, 34)
                );
        }

        [Fact, WorkItem(2)]
        public void InJoin_01()
        {
            var text = @"
using System.Linq;

class C
{
    static void Main()
    {
        var res = from x1 in new[] { 1 }
                  join x2 in new[] { int.TryParse(""42"", out int y) }
                            on x1 equals x2 ? 1 : 0 
                  select new { x1, x2, y };

        System.Console.WriteLine(y); 
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, new[] { SystemCoreRef }, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"42").VerifyDiagnostics();
        }

        public void InJoin_02()
        {
            var text = @"
using System.Linq;

class C
{
    static void Main()
    {
        var res = from x1 in new[] {""41"", ""42"", ""43""}
                  join x2 in new[] {42} 
                            on int.TryParse(x1, out int y) ? y : 0 equals x2
                  select new { x1, x2 };

        foreach (var item in res)
        {
            System.Console.WriteLine(item); 
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, new[] { SystemCoreRef }, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"{ x1 = 42, x2 = 42 }").VerifyDiagnostics();
        }

        public void InJoin_03()
        {
            var text = @"
using System.Linq;

class C
{
    static void Main()
    {
        var res = from x1 in new[] {""41"", ""42"", ""43""}
                  join x2 in new[] {42} 
                            on int.TryParse(x1, out int y) ? y : 0 equals x2 + y
                  select y;

        System.Console.WriteLine(y); 
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, new[] { SystemCoreRef }, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (10,80): error CS1938: The name 'y' is not in scope on the right side of 'equals'.  Consider swapping the expressions on either side of 'equals'.
    //                             on int.TryParse(x1, out int y) ? y : 0 equals x2 + y
    Diagnostic(ErrorCode.ERR_QueryInnerKey, "y").WithArguments("y").WithLocation(10, 80),
    // (11,26): error CS0103: The name 'y' does not exist in the current context
    //                   select y;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(11, 26),
    // (13,34): error CS0103: The name 'y' does not exist in the current context
    //         System.Console.WriteLine(y); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(13, 34)
                );
        }

        public void InJoin_04()
        {
            var text = @"
using System.Linq;

class C
{
    static void Main()
    {
        var res = from x2 in new[] {42} 
                  join x1 in new[] {""41"", ""42"", ""43""}
                            on x2 equals int.TryParse(x1, out int y) ? y : 0
                  select new { x1, x2 };

        foreach (var item in res)
        {
            System.Console.WriteLine(item); 
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, new[] { SystemCoreRef }, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"{ x1 = 42, x2 = 42 }").VerifyDiagnostics();
        }

        public void InJoin_05()
        {
            var text = @"
using System.Linq;

class C
{
    static void Main()
    {
        var res = from x2 in new[] {42} 
                  join x1 in new[] {""41"", ""42"", ""43""}
                            on x2 + y equals int.TryParse(x1, out int y) ? y : 0 
                  select y;

        System.Console.WriteLine(y); 
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, new[] { SystemCoreRef }, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (10,37): error CS0103: The name 'y' does not exist in the current context
    //                             on x2 + y equals int.TryParse(x1, out int y) ? y : 0
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(10, 37),
    // (11,26): error CS0103: The name 'y' does not exist in the current context
    //                   select y;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(11, 26),
    // (13,34): error CS0103: The name 'y' does not exist in the current context
    //         System.Console.WriteLine(y); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(13, 34)
                );
        }

        [Fact, WorkItem(2)]
        public void InWhere_01()
        {
            var text = @"
using System.Linq;

class C
{
    static void Main()
    {
        var res = from x in new[] {""41"", ""42"", ""43""}
                  where int.TryParse(x, out int y) && y == 42
                  select x;

        foreach (var item in res)
        {
            System.Console.WriteLine(item); 
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, new[] { SystemCoreRef }, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"42").VerifyDiagnostics();
        }

        [Fact, WorkItem(2)]
        public void InWhere_02()
        {
            var text = @"
using System.Linq;

class C
{
    static void Main()
    {
        var res = from x in new[] { ""42"" }
                  where int.TryParse(x, out int y)
                  select y;

        System.Console.WriteLine(y); 

    }
}";
            var compilation = CreateCompilationWithMscorlib(text, new[] { SystemCoreRef }, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (10,26): error CS0103: The name 'y' does not exist in the current context
    //                   select y;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(10, 26),
    // (12,34): error CS0103: The name 'y' does not exist in the current context
    //         System.Console.WriteLine(y); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(12, 34)
                );
        }

        [Fact, WorkItem(2)]
        public void InOrderBy_01()
        {
            var text = @"
using System.Linq;

class C
{
    static void Main()
    {
        var res = from x in new[] {""41"", ""xx"", ""43""}
                  orderby int.TryParse(x, out int y) ? y : 0
                  select x;

        foreach (var item in res)
        {
            System.Console.WriteLine(item); 
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, new[] { SystemCoreRef }, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"xx
41
43").VerifyDiagnostics();
        }

        [Fact, WorkItem(2)]
        public void InOrderBy_02()
        {
            var text = @"
using System.Linq;

class C
{
    static void Main()
    {
        var res = from x in new[] { ""42"" }
                  orderby int.TryParse(x, out int y) ? y : 0
                  select y;

        System.Console.WriteLine(y); 

    }
}";
            var compilation = CreateCompilationWithMscorlib(text, new[] { SystemCoreRef }, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (10,26): error CS0103: The name 'y' does not exist in the current context
    //                   select y;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(10, 26),
    // (12,34): error CS0103: The name 'y' does not exist in the current context
    //         System.Console.WriteLine(y); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(12, 34)
                );
        }

        [Fact, WorkItem(2)]
        public void InSelect_01()
        {
            var text = @"
using System.Linq;

class C
{
    static void Main()
    {
        var res = from x in new[] { ""42"" }
                  select new { r = int.TryParse(x, out int y), y = y};

        foreach (var item in res)
        {
            System.Console.WriteLine(item); 
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, new[] { SystemCoreRef }, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"{ r = True, y = 42 }").VerifyDiagnostics();
        }

        [Fact, WorkItem(2)]
        public void InSelect_02()
        {
            var text = @"
using System.Linq;

class C
{
    static void Main()
    {
        var res = from x in new[] { ""42"" }
                  select new { r = int.TryParse(x, out int y), y = y};

        System.Console.WriteLine(y); 
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, new[] { SystemCoreRef }, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (11,34): error CS0103: The name 'y' does not exist in the current context
    //         System.Console.WriteLine(y); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(11, 34)
                );
        }

        [Fact, WorkItem(2)]
        public void InGroupBy_01()
        {
            var text = @"
using System.Linq;

class C
{
    static void Main()
    {
        var res = from x in new[] { ""42"", ""xx"", ""43"" }
                  group int.TryParse(x, out int y) ? y : 0 by int.TryParse(x, out int y) ? y : 43;

        foreach (var group in res)
        {
            System.Console.WriteLine(group.Key); 

            foreach (var item in group)
            {
                System.Console.WriteLine(""   {0}"", item); 
            }
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, new[] { SystemCoreRef }, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"42
   42
43
   0
   43").VerifyDiagnostics();
        }

        public void InGroupBy_02()
        {
            var text = @"
using System.Linq;

class C
{
    static void Main()
    {
        var res = from x in new[] { ""42"", ""xx"", ""43"" }
                  group int.TryParse(x, out int y) ? y : z 
                  by int.TryParse(x, out int z) ? y : z;

        System.Console.WriteLine(y); 
        System.Console.WriteLine(z); 
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, new[] { SystemCoreRef }, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
        // (10,51): error CS0103: The name 'y' does not exist in the current context
        //                   by int.TryParse(x, out int z) ? y : z;
        Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(10, 51),
        // (9,58): error CS0103: The name 'z' does not exist in the current context
        //                   group int.TryParse(x, out int y) ? y : z
        Diagnostic(ErrorCode.ERR_NameNotInContext, "z").WithArguments("z").WithLocation(9, 58),
        // (12,34): error CS0103: The name 'y' does not exist in the current context
        //         System.Console.WriteLine(y);
        Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(12, 34),
        // (13,34): error CS0103: The name 'z' does not exist in the current context
        //         System.Console.WriteLine(z);
        Diagnostic(ErrorCode.ERR_NameNotInContext, "z").WithArguments("z").WithLocation(13, 34)
                );
        }

        [Fact, WorkItem(6)]
        public void InConstructorInitializer_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var x = new Derived();
    }
}

class Base
{
    public Base(int x)
    { 
        System.Console.WriteLine(""Base: {0}"", x);
    }
}

class Derived : Base 
{
    public Derived() : base(int x = 123)
    { 
        //System.Console.WriteLine(""Derived: {0}"", x);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"Base: 123").VerifyDiagnostics();
        }

        [Fact, WorkItem(6)]
        public void InConstructorInitializer_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var x = new Derived();
    }
}

class Base
{
    public Base(out int x)
    { 
        x = 123;
    }
}

class Derived : Base 
{
    public Derived() : base(out var x)
    { 
        System.Console.WriteLine(""Derived: {0}"", x);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"Derived: 123").VerifyDiagnostics();
        }

        [Fact, WorkItem(6)]
        public void InConstructorInitializer_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var x = new Derived();
    }
}

class Base
{
    public Base(int x)
    { 
        System.Console.WriteLine(""Base: {0}"", x);
    }
}

class Derived : Base 
{
    private int y = 124;

    public Derived() : base(int x = 123)
    { 
        System.Console.WriteLine(""Derived: {0}"", x + y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"Base: 123
Derived: 247").VerifyDiagnostics();
        }

        [Fact, WorkItem(6)]
        public void InConstructorInitializer_04()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var x = new Derived();
    }
}

class Base
{
    public Base(int x, int y)
    { 
        System.Console.WriteLine(""Base: {0}, {1}"", x, y);
    }
}

class Derived() : Base(int x = 123, x + 1) 
{
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: @"Base: 123, 124").VerifyDiagnostics();
        }

        [Fact]
        public void InUsing()
        {
            var text = @"
using System;

struct S : IDisposable
{
    public void Dispose() { }
    static void Main()
    {
        using(S x) 
        {
            x = new S(); 
        }

        using(int y) 
        {
            y = 10; 
        }

        using(S a = new S()) 
        {
            a = new S(); 
        }

        using(S b, ) 
        {
            b = new S(); 
        }

        using(S c, d = new S()) 
        {
            c = new S(); 
            d = new S(); 
        }

        using(S e = ) 
        {
            e = new S(); 
        }

        using(S f = , g = new S()) 
        {
            f = new S(); 
            g = new S(); 
        }

        using(int h, ) 
        {
            h = 10; 
        }

        using(int i = ) 
        {
            i = 10; 
        }

        using(int 2) 
        {
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (24,20): error CS1001: Identifier expected
    //         using(S b, ) 
    Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(24, 20),
    // (35,21): error CS1525: Invalid expression term ')'
    //         using(S e = ) 
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(35, 21),
    // (40,21): error CS1525: Invalid expression term ','
    //         using(S f = , g = new S()) 
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(40, 21),
    // (46,22): error CS1001: Identifier expected
    //         using(int h, ) 
    Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(46, 22),
    // (51,23): error CS1525: Invalid expression term ')'
    //         using(int i = ) 
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(51, 23),
    // (56,19): error CS1001: Identifier expected
    //         using(int 2) 
    Diagnostic(ErrorCode.ERR_IdentifierExpected, "2").WithLocation(56, 19),
    // (56,19): error CS1026: ) expected
    //         using(int 2) 
    Diagnostic(ErrorCode.ERR_CloseParenExpected, "2").WithLocation(56, 19),
    // (56,20): error CS1002: ; expected
    //         using(int 2) 
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(56, 20),
    // (56,20): error CS1513: } expected
    //         using(int 2) 
    Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(56, 20),
    // (14,15): error CS1674: 'int': type used in a using statement must be implicitly convertible to 'System.IDisposable'
    //         using(int y) 
    Diagnostic(ErrorCode.ERR_NoConvToIDisp, "int y").WithArguments("int").WithLocation(14, 15),
    // (21,13): error CS1656: Cannot assign to 'a' because it is a 'using variable'
    //             a = new S(); 
    Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "a").WithArguments("a", "using variable").WithLocation(21, 13),
    // (24,17): error CS0210: You must provide an initializer in a fixed or using statement declaration
    //         using(S b, ) 
    Diagnostic(ErrorCode.ERR_FixedMustInit, "b").WithLocation(24, 17),
    // (24,20): error CS0210: You must provide an initializer in a fixed or using statement declaration
    //         using(S b, ) 
    Diagnostic(ErrorCode.ERR_FixedMustInit, "").WithLocation(24, 20),
    // (26,13): error CS1656: Cannot assign to 'b' because it is a 'using variable'
    //             b = new S(); 
    Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "b").WithArguments("b", "using variable").WithLocation(26, 13),
    // (29,17): error CS0210: You must provide an initializer in a fixed or using statement declaration
    //         using(S c, d = new S()) 
    Diagnostic(ErrorCode.ERR_FixedMustInit, "c").WithLocation(29, 17),
    // (31,13): error CS1656: Cannot assign to 'c' because it is a 'using variable'
    //             c = new S(); 
    Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "c").WithArguments("c", "using variable").WithLocation(31, 13),
    // (32,13): error CS1656: Cannot assign to 'd' because it is a 'using variable'
    //             d = new S(); 
    Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "d").WithArguments("d", "using variable").WithLocation(32, 13),
    // (37,13): error CS1656: Cannot assign to 'e' because it is a 'using variable'
    //             e = new S(); 
    Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "e").WithArguments("e", "using variable").WithLocation(37, 13),
    // (42,13): error CS1656: Cannot assign to 'f' because it is a 'using variable'
    //             f = new S(); 
    Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "f").WithArguments("f", "using variable").WithLocation(42, 13),
    // (43,13): error CS1656: Cannot assign to 'g' because it is a 'using variable'
    //             g = new S(); 
    Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "g").WithArguments("g", "using variable").WithLocation(43, 13),
    // (46,19): error CS0210: You must provide an initializer in a fixed or using statement declaration
    //         using(int h, ) 
    Diagnostic(ErrorCode.ERR_FixedMustInit, "h").WithLocation(46, 19),
    // (46,22): error CS0210: You must provide an initializer in a fixed or using statement declaration
    //         using(int h, ) 
    Diagnostic(ErrorCode.ERR_FixedMustInit, "").WithLocation(46, 22),
    // (46,15): error CS1674: 'int': type used in a using statement must be implicitly convertible to 'System.IDisposable'
    //         using(int h, ) 
    Diagnostic(ErrorCode.ERR_NoConvToIDisp, "int h, ").WithArguments("int").WithLocation(46, 15),
    // (48,13): error CS1656: Cannot assign to 'h' because it is a 'using variable'
    //             h = 10; 
    Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "h").WithArguments("h", "using variable").WithLocation(48, 13),
    // (51,15): error CS1674: 'int': type used in a using statement must be implicitly convertible to 'System.IDisposable'
    //         using(int i = ) 
    Diagnostic(ErrorCode.ERR_NoConvToIDisp, "int i = ").WithArguments("int").WithLocation(51, 15),
    // (53,13): error CS1656: Cannot assign to 'i' because it is a 'using variable'
    //             i = 10; 
    Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "using variable").WithLocation(53, 13),
    // (56,19): error CS0210: You must provide an initializer in a fixed or using statement declaration
    //         using(int 2) 
    Diagnostic(ErrorCode.ERR_FixedMustInit, "").WithLocation(56, 19),
    // (56,15): error CS1674: 'int': type used in a using statement must be implicitly convertible to 'System.IDisposable'
    //         using(int 2) 
    Diagnostic(ErrorCode.ERR_NoConvToIDisp, "int ").WithArguments("int").WithLocation(56, 15),
    // (14,15): error CS0165: Use of unassigned local variable 'y'
    //         using(int y) 
    Diagnostic(ErrorCode.ERR_UseDefViolation, "int y").WithArguments("y").WithLocation(14, 15)
                );
        }

        [Fact, WorkItem(134)]
        public void Compound_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        System.Console.WriteLine(""{0} {1}"", ++(var y = 2), y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "3 3").VerifyDiagnostics();
        }

        [Fact, WorkItem(134)]
        public void Compound_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        System.Console.WriteLine(""{0} {1}"", (var y = 2)++, y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "2 3").VerifyDiagnostics();
        }

        [Fact, WorkItem(134)]
        public void Compound_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        System.Console.WriteLine(""{0} {1}"", --(var y = 2), y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "1 1").VerifyDiagnostics();
        }

        [Fact, WorkItem(134)]
        public void Compound_04()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        System.Console.WriteLine(""{0} {1}"", (var y = 2)--, y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "2 1").VerifyDiagnostics();
        }

        [Fact, WorkItem(134)]
        public void Compound_05()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        System.Console.WriteLine(""{0} {1}"", (var y = 2) += 1, y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "3 3").VerifyDiagnostics();
        }

        [Fact, WorkItem(134)]
        public void Compound_06()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        System.Console.WriteLine(""{0} {1}"", (var y = 2) -= 1, y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "1 1").VerifyDiagnostics();
        }

        [Fact, WorkItem(134)]
        public void Compound_07()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        System.Console.WriteLine(""{0} {1}"", (var y = 2) *= 2, y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "4 4").VerifyDiagnostics();
        }

        [Fact, WorkItem(134)]
        public void Compound_08()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        System.Console.WriteLine(""{0} {1}"", (var y = 8) /= 2, y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "4 4").VerifyDiagnostics();
        }

        [Fact, WorkItem(134)]
        public void Compound_09()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        System.Console.WriteLine(""{0} {1}"", (var y = 8) %= 3, y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "2 2").VerifyDiagnostics();
        }

        [Fact, WorkItem(134)]
        public void Compound_10()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        System.Console.WriteLine(""{0} {1}"", (var y = 7) &= 3, y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "3 3").VerifyDiagnostics();
        }

        [Fact, WorkItem(134)]
        public void Compound_11()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        System.Console.WriteLine(""{0} {1}"", (var y = 2) |= 1, y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "3 3").VerifyDiagnostics();
        }

        [Fact, WorkItem(134)]
        public void Compound_12()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        System.Console.WriteLine(""{0} {1}"", (var y = 7) ^= 3, y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "4 4").VerifyDiagnostics();
        }

        [Fact, WorkItem(134)]
        public void Compound_13()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        System.Console.WriteLine(""{0} {1}"", (var y = 2) <<= 1, y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "4 4").VerifyDiagnostics();
        }

        [Fact, WorkItem(134)]
        public void Compound_14()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        System.Console.WriteLine(""{0} {1}"", (var y = 2) >>= 1, y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "1 1").VerifyDiagnostics();
        }

        [Fact]
        public void InitializationScope_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var t = new Test();
        System.Console.WriteLine(""{0} {1} {2}"", t.x, t.y, t.z);
    }
}

class Test
{
    public int x = (int x = 10);
    public int y = ++x + 1;
    public int z = x;
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "10 12 11").VerifyDiagnostics();
        }

        [Fact]
        public void InitializationScope_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var t1 = new Test();
        var t2 = new Test(1);
        System.Console.WriteLine(""{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}"", t1.x1, t1.y1, t1.z1, t1.x2, t1.y2, t1.z2, t2.x1, t2.y1, t2.z1, t2.x2, t2.y2, t2.z2);
    }
}

partial class Test
{
    public int x1 = (int x = 10);
    public int y1 = ++x + 1;
    public int z1 = x;

    public Test(){}
}

partial class Test
{
    public int x2 = (int x = 100);
    public int y2 = ++x + 1;
    public int z2 = x;

    public Test(int v){}
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "10 12 11 100 102 101 10 12 11 100 102 101").VerifyDiagnostics();
        }

        [Fact]
        public void InitializationScope_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var t1 = new Test();
        var t2 = new Test(1);
        System.Console.WriteLine(""{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}"", t1.x1, t1.y1, t1.z1, t1.x2, t1.y2, t1.z2, t2.x1, t2.y1, t2.z1, t2.x2, t2.y2, t2.z2);
    }
}

partial class Test
{
    public int x2 = (int x = 100);
    public int y2 = ++x + 1;
    public int z2 = x;

    public Test(){}
}

partial class Test
{
    public int x1 = (int x = 10);
    public int y1 = ++x + 1;
    public int z1 = x;

    public Test(int v){}
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "10 12 11 100 102 101 10 12 11 100 102 101").VerifyDiagnostics();
        }

        [Fact]
        public void InitializationScope_04()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var t1 = new Test();
        var t2 = new Test(1);
        System.Console.WriteLine(""{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}"", t1.x1, t1.y1, t1.z1(), t1.x2, t1.y2, t1.z2(), t2.x1, t2.y1, t2.z1(), t2.x2, t2.y2, t2.z2());
    }
}

partial class Test
{
    public int x1 = (int x = 10);
    public System.Func<int> z1 = () => x;
    public int y1 = ++x + 1;

    public Test(){}
}

partial class Test
{
    public int x2 = (int x = 100);
    public System.Func<int> z2 = () => x;
    public int y2 = ++x + 1;

    public Test(int v){}
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "10 12 11 100 102 101 10 12 11 100 102 101").VerifyDiagnostics();
        }

        [Fact]
        public void InitializationScope_05()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        System.Console.WriteLine(""{0} {1} {2}"", Test.x, Test.y, Test.z);
    }
}

class Test
{
    public static int x = (int x = 10);
    public static int y = ++x + 1;
    public static int z = x;
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "10 12 11").VerifyDiagnostics();
        }

        [Fact]
        public void InitializationScope_06()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        System.Console.WriteLine(""{0} {1} {2} {3} {4} {5}"", Test.x1, Test.y1, Test.z1, Test.x2, Test.y2, Test.z2);
    }
}

partial class Test
{
    public static int x1 = (int x = 10);
    public static int y1 = ++x + 1;
    public static int z1 = x;

    static Test(){}
}

partial class Test
{
    public static int x2 = (int x = 100);
    public static int y2 = ++x + 1;
    public static int z2 = x;
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "10 12 11 100 102 101").VerifyDiagnostics();
        }

        [Fact]
        public void InitializationScope_07()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        System.Console.WriteLine(""{0} {1} {2} {3} {4} {5}"", Test.x1, Test.y1, Test.z1, Test.x2, Test.y2, Test.z2);
    }
}

partial class Test
{
    public static int x2 = (int x = 100);
    public static int y2 = ++x + 1;
    public static int z2 = x;
}

partial class Test
{
    public static int x1 = (int x = 10);
    public static int y1 = ++x + 1;
    public static int z1 = x;
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "10 12 11 100 102 101").VerifyDiagnostics();
        }

        [Fact]
        public void InitializationScope_08()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        System.Console.WriteLine(""{0} {1} {2} {3} {4} {5}"", Test.x1, Test.y1, Test.z1(), Test.x2, Test.y2, Test.z2());
    }
}

partial class Test
{
    public static int x1 = (int x = 10);
    public static System.Func<int> z1 = () => x;
    public static int y1 = ++x + 1;
}

partial class Test
{
    public static int x2 = (int x = 100);
    public static System.Func<int> z2 = () => x;
    public static int y2 = ++x + 1;
            
    static Test(){}
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "10 12 11 100 102 101").VerifyDiagnostics();
        }

        [Fact]
        public void InitializationScope_09()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var t1 = new Test();
        System.Console.WriteLine(""{0} {1} {2} {3} {4} {5}"", t1.x1, t1.y1, t1.z1, Test.x2, Test.y2, Test.z2);
    }
}

partial class Test
{
    public int x1 = (int x = 10);
    public int y1 = ++x + 1;
    public int z1 = x;

    public static int x2 = (int y = 100);
    public static int y2 = ++y + 1;
    public static int z2 = y;
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "10 12 11 100 102 101").VerifyDiagnostics();
        }

        [Fact]
        public void InitializationScope_10()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var t1 = new Test();
        System.Console.WriteLine(""{0} {1} {2} {3} {4} {5}"", t1.x1, t1.y1, t1.z1, Test.x2, Test.y2, Test.z2);
    }
}

partial class Test
{
    public int x1 = (int x = 10);
    public static int x2 = (int x = 100);
    public int y1 = ++x + 1;
    public static int y2 = ++x + 1;
    public int z1 = x;
    public static int z2 = x;
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "10 12 11 100 102 101").VerifyDiagnostics();
        }

        [Fact]
        public void InitializationScope_11()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

class Base
{
    public Base(int x) {}
    public Base() {}
}

partial class Test0() : Base
{
    public int x1 = (int x = 10);
}

partial class Test1() : Base(x)
{
    public int x1 = (int x = 10);
}

partial class Test2 : Base
{
    public int x1 = (int x = 10);

    public Test2() : base(x) {} 
}

partial class Test3
{
    public int x1 = (int x = 10);

    public Test3() 
    {
        System.Console.WriteLine(x);
    } 
}

class Test4 : Base
{
    static int x1 = (int x = 10);

    static Test4() : base((int y = x) + y){}  
}

class Test5 : Base
{
    int x1 = (int x = 10);

    static Test5() : base(x){}  
}

partial class Test6(int p = x)
{
    public int x1 = (int x = 10);
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (46,22): error CS0514: 'Test4': static constructor cannot have an explicit 'this' or 'base' constructor call
    //     static Test4() : base((int y = x) + y){}  
    Diagnostic(ErrorCode.ERR_StaticConstructorWithExplicitConstructorCall, "base").WithArguments("Test4").WithLocation(46, 22),
    // (53,22): error CS0514: 'Test5': static constructor cannot have an explicit 'this' or 'base' constructor call
    //     static Test5() : base(x){}  
    Diagnostic(ErrorCode.ERR_StaticConstructorWithExplicitConstructorCall, "base").WithArguments("Test5").WithLocation(53, 22),
    // (56,29): error CS0103: The name 'x' does not exist in the current context
    // partial class Test6(int p = x)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(56, 29),
    // (56,25): error CS1750: A value of type '?' cannot be used as a default parameter because there are no standard conversions to type 'int'
    // partial class Test6(int p = x)
    Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "p").WithArguments("?", "int").WithLocation(56, 25),
    // (20,30): error CS0841: Cannot use local variable 'x' before it is declared
    // partial class Test1() : Base(x)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x").WithArguments("x").WithLocation(20, 30),
    // (29,27): error CS0103: The name 'x' does not exist in the current context
    //     public Test2() : base(x) {} 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(29, 27),
    // (38,34): error CS0103: The name 'x' does not exist in the current context
    //         System.Console.WriteLine(x);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(38, 34)
                );
        }

        [Fact]
        public void InitializationScope_12()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

class Base
{
    public Base(int x, int y) {}
    public Base() {}
}

partial class Test1() : Base(x, y)
{
    public int x1 = (int x = 10);
}

partial class Test1
{
    public int x2 = (int y = 10);
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (15,30): error CS0841: Cannot use local variable 'x' before it is declared
    // partial class Test1() : Base(x, y)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x").WithArguments("x").WithLocation(15, 30),
    // (15,33): error CS0103: The name 'y' does not exist in the current context
    // partial class Test1() : Base(x, y)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(15, 33)
                );
        }

        [Fact]
        public void InitializationScope_13()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

class Base
{
    public Base(int x, int y) {}
    public Base() {}
}

partial class Test1
{
    public int x2 = (int y = 10);
}

partial class Test1() : Base(x, y)
{
    public int x1 = (int x = 10);
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (20,30): error CS0841: Cannot use local variable 'x' before it is declared
    // partial class Test1() : Base(x, y)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x").WithArguments("x").WithLocation(20, 30),
    // (20,33): error CS0103: The name 'y' does not exist in the current context
    // partial class Test1() : Base(x, y)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(20, 33)
                );
        }

        [Fact]
        public void InitializationScope_14()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

class Base
{
    public Base(int x, int y, int z) {}
    public Base() {}
}

partial class Test1
{
    public int x2 = (int y = 10);
}

partial class Test1() : Base(x, y, z)
{
    public int x1 = (int x = 10);
}

partial class Test1
{
    public int x3 = (int z = 10);
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (20,30): error CS0841: Cannot use local variable 'x' before it is declared
    // partial class Test1() : Base(x, y, z)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x").WithArguments("x").WithLocation(20, 30),
    // (20,33): error CS0103: The name 'y' does not exist in the current context
    // partial class Test1() : Base(x, y, z)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(20, 33),
    // (20,36): error CS0103: The name 'z' does not exist in the current context
    // partial class Test1() : Base(x, y, z)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z").WithArguments("z").WithLocation(20, 36)
                );
        }

        [Fact]
        public void InitializationScope_15()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

class Base
{
    public Base(int x) {}
}
partial class Test1() : Base(int x = 10)
{
    public int x1 = x;
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (15,21): error CS0103: The name 'x' does not exist in the current context
    //     public int x1 = x;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(15, 21)
                );
        }

        [Fact]
        public void InitializationScope_16()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

partial class Test1(int x)
{
    public int x1 = (int x = 1);
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (11,26): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //     public int x1 = (int x = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(11, 26)
                );
        }

        [Fact]
        public void InitializationScope_17()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

partial class Test1(int x)
{
    public static int x1 = (int x = 1);
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
                );
        }

        [Fact]
        public void InitializationScope_18()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

partial class Test1(int x)
{
    const int x1 = (int x = 1);
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (11,20): error CS0133: The expression being assigned to 'Test1.x1' must be constant
    //     const int x1 = (int x = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int x = 1)").WithArguments("Test1.x1").WithLocation(11, 20)
                );
        }

        [Fact]
        public void InitializationScope_19()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

partial class Test1(int x)
{
    const decimal x1 = (int x = 1);
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (11,24): error CS0133: The expression being assigned to 'Test1.x1' must be constant
    //     const decimal x1 = (int x = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int x = 1)").WithArguments("Test1.x1").WithLocation(11, 24)
                );
        }

        [Fact]
        public void InitializationScope_20()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

class Base
{
    public Base(int x) {}
}
partial class Test1(int x) : Base(int x = 10)
{
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (13,39): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    // partial class Test1(int x) : Base(int x = 10)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(13, 39)
                );
        }

        [Fact]
        public void InitializationScope_21()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

partial class Test1
{
    int x1 = (int a = 1) + a + (int a = 2);
    int x2 {get;} = (int b = 1) + b + (int b = 2);
    event System.Action x3  = (System.Action c = null) += c += (System.Action c = null);

    static int x4 = (int d = 1) + d + (int d = 2);
    static int x5 {get;} = (int e = 1) + e + (int e = 2);
    static event System.Action x6  = (System.Action f = null) += f += (System.Action f = null);

    const int x7 = (int g = 1) + g + (int g = 2);
    const decimal x8 = (decimal h = 1) + h + (decimal h = 2);
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (19,43): error CS0128: A local variable named 'g' is already defined in this scope
    //     const int x7 = (int g = 1) + g + (int g = 2);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "g").WithArguments("g").WithLocation(19, 43),
    // (20,55): error CS0128: A local variable named 'h' is already defined in this scope
    //     const decimal x8 = (decimal h = 1) + h + (decimal h = 2);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "h").WithArguments("h").WithLocation(20, 55),
    // (15,44): error CS0128: A local variable named 'd' is already defined in this scope
    //     static int x4 = (int d = 1) + d + (int d = 2);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "d").WithArguments("d").WithLocation(15, 44),
    // (16,51): error CS0128: A local variable named 'e' is already defined in this scope
    //     static int x5 {get;} = (int e = 1) + e + (int e = 2);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "e").WithArguments("e").WithLocation(16, 51),
    // (17,86): error CS0128: A local variable named 'f' is already defined in this scope
    //     static event System.Action x6  = (System.Action f = null) += f += (System.Action f = null);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "f").WithArguments("f").WithLocation(17, 86),
    // (20,55): error CS0128: A local variable named 'h' is already defined in this scope
    //     const decimal x8 = (decimal h = 1) + h + (decimal h = 2);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "h").WithArguments("h").WithLocation(20, 55),
    // (11,37): error CS0128: A local variable named 'a' is already defined in this scope
    //     int x1 = (int a = 1) + a + (int a = 2);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "a").WithArguments("a").WithLocation(11, 37),
    // (12,44): error CS0128: A local variable named 'b' is already defined in this scope
    //     int x2 {get;} = (int b = 1) + b + (int b = 2);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "b").WithArguments("b").WithLocation(12, 44),
    // (13,79): error CS0128: A local variable named 'c' is already defined in this scope
    //     event System.Action x3  = (System.Action c = null) += c += (System.Action c = null);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c").WithArguments("c").WithLocation(13, 79)
                );
        }

        [Fact]
        public void InitializationScope_22()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

partial class Test1
{
    int x = (int a = 1)+(int b = 1)+(int c = 1)+(int d = 1)+(int e = 1)+(int f = 1)+(int g = 1)+(int h = 1);
    int y = (int a1 = 1)+(int b1 = 1)+(int c1 = 1)+(int d1 = 1)+(int e1 = 1)+(int f1 = 1)+(int g1 = 1)+(int h1 = 1);

    int x1 = (int a = 1);
    int x11 = a1;
    int x2 {get;} = (int b = 1);
    int x21 {get;} = b1;
    event System.Action x3  = (System.Action c = null);
    event System.Action x31  = ()=> c1 = 2;

    static int x4 = (int d = 1);
    static int x41 = d1;
    static int x5 {get;} = (int e = 1);
    static int x51 {get;} = e1;
    static event System.Action x6  = (System.Action f = null);
    static event System.Action x61  = ()=> f1 = 2;

    const int x7 = (int g = 1);
    const int x71 = g1;
    const decimal x8 = (decimal h = 1);
    const decimal x81 = (decimal)h1;
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (28,20): error CS0133: The expression being assigned to 'Test1.x7' must be constant
    //     const int x7 = (int g = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int g = 1)").WithArguments("Test1.x7").WithLocation(28, 20),
    // (29,21): error CS0103: The name 'g1' does not exist in the current context
    //     const int x71 = g1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "g1").WithArguments("g1").WithLocation(29, 21),
    // (30,24): error CS0133: The expression being assigned to 'Test1.x8' must be constant
    //     const decimal x8 = (decimal h = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(decimal h = 1)").WithArguments("Test1.x8").WithLocation(30, 24),
    // (31,34): error CS0103: The name 'h1' does not exist in the current context
    //     const decimal x81 = (decimal)h1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "h1").WithArguments("h1").WithLocation(31, 34),
    // (22,22): error CS0103: The name 'd1' does not exist in the current context
    //     static int x41 = d1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "d1").WithArguments("d1").WithLocation(22, 22),
    // (24,29): error CS0103: The name 'e1' does not exist in the current context
    //     static int x51 {get;} = e1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "e1").WithArguments("e1").WithLocation(24, 29),
    // (26,44): error CS0103: The name 'f1' does not exist in the current context
    //     static event System.Action x61  = ()=> f1 = 2;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "f1").WithArguments("f1").WithLocation(26, 44),
    // (31,34): error CS0103: The name 'h1' does not exist in the current context
    //     const decimal x81 = (decimal)h1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "h1").WithArguments("h1").WithLocation(31, 34),
    // (14,19): error CS0128: A local variable named 'a' is already defined in this scope
    //     int x1 = (int a = 1);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "a").WithArguments("a").WithLocation(14, 19),
    // (16,26): error CS0128: A local variable named 'b' is already defined in this scope
    //     int x2 {get;} = (int b = 1);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "b").WithArguments("b").WithLocation(16, 26),
    // (18,46): error CS0128: A local variable named 'c' is already defined in this scope
    //     event System.Action x3  = (System.Action c = null);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c").WithArguments("c").WithLocation(18, 46)
                );
        }

        [Fact]
        public void InitializationScope_23()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

partial class Test1
{
    int x {get;} = (int a = 1)+(int b = 1)+(int c = 1)+(int d = 1)+(int e = 1)+(int f = 1)+(int g = 1)+(int h = 1);
    int y {get;} = (int a1 = 1)+(int b1 = 1)+(int c1 = 1)+(int d1 = 1)+(int e1 = 1)+(int f1 = 1)+(int g1 = 1)+(int h1 = 1);

    int x1 = (int a = 1);
    int x11 = a1;
    int x2 {get;} = (int b = 1);
    int x21 {get;} = b1;
    event System.Action x3  = (System.Action c = null);
    event System.Action x31  = ()=> c1 = 2;

    static int x4 = (int d = 1);
    static int x41 = d1;
    static int x5 {get;} = (int e = 1);
    static int x51 {get;} = e1;
    static event System.Action x6  = (System.Action f = null);
    static event System.Action x61  = ()=> f1 = 2;

    const int x7 = (int g = 1);
    const int x71 = g1;
    const decimal x8 = (decimal h = 1);
    const decimal x81 = (decimal)h1;
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (28,20): error CS0133: The expression being assigned to 'Test1.x7' must be constant
    //     const int x7 = (int g = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int g = 1)").WithArguments("Test1.x7").WithLocation(28, 20),
    // (29,21): error CS0103: The name 'g1' does not exist in the current context
    //     const int x71 = g1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "g1").WithArguments("g1").WithLocation(29, 21),
    // (30,24): error CS0133: The expression being assigned to 'Test1.x8' must be constant
    //     const decimal x8 = (decimal h = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(decimal h = 1)").WithArguments("Test1.x8").WithLocation(30, 24),
    // (31,34): error CS0103: The name 'h1' does not exist in the current context
    //     const decimal x81 = (decimal)h1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "h1").WithArguments("h1").WithLocation(31, 34),
    // (22,22): error CS0103: The name 'd1' does not exist in the current context
    //     static int x41 = d1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "d1").WithArguments("d1").WithLocation(22, 22),
    // (24,29): error CS0103: The name 'e1' does not exist in the current context
    //     static int x51 {get;} = e1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "e1").WithArguments("e1").WithLocation(24, 29),
    // (26,44): error CS0103: The name 'f1' does not exist in the current context
    //     static event System.Action x61  = ()=> f1 = 2;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "f1").WithArguments("f1").WithLocation(26, 44),
    // (31,34): error CS0103: The name 'h1' does not exist in the current context
    //     const decimal x81 = (decimal)h1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "h1").WithArguments("h1").WithLocation(31, 34),
    // (14,19): error CS0128: A local variable named 'a' is already defined in this scope
    //     int x1 = (int a = 1);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "a").WithArguments("a").WithLocation(14, 19),
    // (16,26): error CS0128: A local variable named 'b' is already defined in this scope
    //     int x2 {get;} = (int b = 1);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "b").WithArguments("b").WithLocation(16, 26),
    // (18,46): error CS0128: A local variable named 'c' is already defined in this scope
    //     event System.Action x3  = (System.Action c = null);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c").WithArguments("c").WithLocation(18, 46)
                );
        }

        [Fact]
        public void InitializationScope_24()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

partial class Test1
{
    event System.Action x = (System.Action a = null)+=(System.Action b = null)+=(System.Action c = null)+=(System.Action d = null)+=(System.Action e = null)+=(System.Action f = null)+=(System.Action g = null)+=(System.Action h = null);
    event System.Action y = (System.Action a1 = null)+=(System.Action b1 = null)+=(System.Action c1= null)+=(System.Action d1 = null)+=(System.Action e1 = null)+=(System.Action f1 = null)+=(System.Action g1 = null)+=(System.Action h1 = null);

    System.Action x1 = (System.Action a = null);
    System.Action x11 = a1;
    System.Action x2 {get;} = (System.Action b = null);
    System.Action x21 {get;} = b1;
    event System.Action x3  = (System.Action c = null);
    event System.Action x31  = c1;

    static System.Action x4 = (System.Action d = null);
    static System.Action x41 = d1;
    static System.Action x5 {get;} = (System.Action e = null);
    static System.Action x51 {get;} = e1;
    static event System.Action x6  = (System.Action f = null);
    static event System.Action x61  = f1;

    const int x7 = (int g = 1);
    const int x71 = g1;
    const decimal x8 = (decimal h = 1);
    const decimal x81 = (decimal)h1;
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (28,20): error CS0133: The expression being assigned to 'Test1.x7' must be constant
    //     const int x7 = (int g = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int g = 1)").WithArguments("Test1.x7").WithLocation(28, 20),
    // (29,21): error CS0103: The name 'g1' does not exist in the current context
    //     const int x71 = g1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "g1").WithArguments("g1").WithLocation(29, 21),
    // (30,24): error CS0133: The expression being assigned to 'Test1.x8' must be constant
    //     const decimal x8 = (decimal h = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(decimal h = 1)").WithArguments("Test1.x8").WithLocation(30, 24),
    // (31,34): error CS0103: The name 'h1' does not exist in the current context
    //     const decimal x81 = (decimal)h1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "h1").WithArguments("h1").WithLocation(31, 34),
    // (22,32): error CS0103: The name 'd1' does not exist in the current context
    //     static System.Action x41 = d1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "d1").WithArguments("d1").WithLocation(22, 32),
    // (24,39): error CS0103: The name 'e1' does not exist in the current context
    //     static System.Action x51 {get;} = e1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "e1").WithArguments("e1").WithLocation(24, 39),
    // (26,39): error CS0103: The name 'f1' does not exist in the current context
    //     static event System.Action x61  = f1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "f1").WithArguments("f1").WithLocation(26, 39),
    // (31,34): error CS0103: The name 'h1' does not exist in the current context
    //     const decimal x81 = (decimal)h1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "h1").WithArguments("h1").WithLocation(31, 34),
    // (14,39): error CS0128: A local variable named 'a' is already defined in this scope
    //     System.Action x1 = (System.Action a = null);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "a").WithArguments("a").WithLocation(14, 39),
    // (16,46): error CS0128: A local variable named 'b' is already defined in this scope
    //     System.Action x2 {get;} = (System.Action b = null);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "b").WithArguments("b").WithLocation(16, 46),
    // (18,46): error CS0128: A local variable named 'c' is already defined in this scope
    //     event System.Action x3  = (System.Action c = null);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c").WithArguments("c").WithLocation(18, 46)
                );
        }

        [Fact]
        public void InitializationScope_25()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

partial class Test1
{
    static int x = (int a = 1)+(int b = 1)+(int c = 1)+(int d = 1)+(int e = 1)+(int f = 1)+(int g = 1)+(int h = 1);
    static int y = (int a1 = 1)+(int b1 = 1)+(int c1 = 1)+(int d1 = 1)+(int e1 = 1)+(int f1 = 1)+(int g1 = 1)+(int h1 = 1);

    int x1 = (int a = 1);
    int x11 = a1;
    int x2 {get;} = (int b = 1);
    int x21 {get;} = b1;
    event System.Action x3  = (System.Action c = null);
    event System.Action x31  = ()=> c1 = 2;

    static int x4 = (int d = 1);
    static int x41 = d1;
    static int x5 {get;} = (int e = 1);
    static int x51 {get;} = e1;
    static event System.Action x6  = (System.Action f = null);
    static event System.Action x61  = ()=> f1 = 2;

    const int x7 = (int g = 1);
    const int x71 = g1;
    const decimal x8 = (decimal h = 1);
    const decimal x81 = (decimal)h1;
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (28,20): error CS0133: The expression being assigned to 'Test1.x7' must be constant
    //     const int x7 = (int g = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int g = 1)").WithArguments("Test1.x7").WithLocation(28, 20),
    // (29,21): error CS0103: The name 'g1' does not exist in the current context
    //     const int x71 = g1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "g1").WithArguments("g1").WithLocation(29, 21),
    // (30,24): error CS0133: The expression being assigned to 'Test1.x8' must be constant
    //     const decimal x8 = (decimal h = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(decimal h = 1)").WithArguments("Test1.x8").WithLocation(30, 24),
    // (31,34): error CS0103: The name 'h1' does not exist in the current context
    //     const decimal x81 = (decimal)h1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "h1").WithArguments("h1").WithLocation(31, 34),
    // (21,26): error CS0128: A local variable named 'd' is already defined in this scope
    //     static int x4 = (int d = 1);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "d").WithArguments("d").WithLocation(21, 26),
    // (23,33): error CS0128: A local variable named 'e' is already defined in this scope
    //     static int x5 {get;} = (int e = 1);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "e").WithArguments("e").WithLocation(23, 33),
    // (25,53): error CS0128: A local variable named 'f' is already defined in this scope
    //     static event System.Action x6  = (System.Action f = null);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "f").WithArguments("f").WithLocation(25, 53),
    // (31,34): error CS0103: The name 'h1' does not exist in the current context
    //     const decimal x81 = (decimal)h1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "h1").WithArguments("h1").WithLocation(31, 34),
    // (15,15): error CS0103: The name 'a1' does not exist in the current context
    //     int x11 = a1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "a1").WithArguments("a1").WithLocation(15, 15),
    // (17,22): error CS0103: The name 'b1' does not exist in the current context
    //     int x21 {get;} = b1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "b1").WithArguments("b1").WithLocation(17, 22),
    // (19,37): error CS0103: The name 'c1' does not exist in the current context
    //     event System.Action x31  = ()=> c1 = 2;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "c1").WithArguments("c1").WithLocation(19, 37)
                );
        }

        [Fact]
        public void InitializationScope_26()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

partial class Test1
{
    static int x {get;} = (int a = 1)+(int b = 1)+(int c = 1)+(int d = 1)+(int e = 1)+(int f = 1)+(int g = 1)+(int h = 1);
    static int y {get;} = (int a1 = 1)+(int b1 = 1)+(int c1 = 1)+(int d1 = 1)+(int e1 = 1)+(int f1 = 1)+(int g1 = 1)+(int h1 = 1);

    int x1 = (int a = 1);
    int x11 = a1;
    int x2 {get;} = (int b = 1);
    int x21 {get;} = b1;
    event System.Action x3  = (System.Action c = null);
    event System.Action x31  = ()=> c1 = 2;

    static int x4 = (int d = 1);
    static int x41 = d1;
    static int x5 {get;} = (int e = 1);
    static int x51 {get;} = e1;
    static event System.Action x6  = (System.Action f = null);
    static event System.Action x61  = ()=> f1 = 2;

    const int x7 = (int g = 1);
    const int x71 = g1;
    const decimal x8 = (decimal h = 1);
    const decimal x81 = (decimal)h1;
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (28,20): error CS0133: The expression being assigned to 'Test1.x7' must be constant
    //     const int x7 = (int g = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int g = 1)").WithArguments("Test1.x7").WithLocation(28, 20),
    // (29,21): error CS0103: The name 'g1' does not exist in the current context
    //     const int x71 = g1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "g1").WithArguments("g1").WithLocation(29, 21),
    // (30,24): error CS0133: The expression being assigned to 'Test1.x8' must be constant
    //     const decimal x8 = (decimal h = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(decimal h = 1)").WithArguments("Test1.x8").WithLocation(30, 24),
    // (31,34): error CS0103: The name 'h1' does not exist in the current context
    //     const decimal x81 = (decimal)h1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "h1").WithArguments("h1").WithLocation(31, 34),
    // (21,26): error CS0128: A local variable named 'd' is already defined in this scope
    //     static int x4 = (int d = 1);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "d").WithArguments("d").WithLocation(21, 26),
    // (23,33): error CS0128: A local variable named 'e' is already defined in this scope
    //     static int x5 {get;} = (int e = 1);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "e").WithArguments("e").WithLocation(23, 33),
    // (25,53): error CS0128: A local variable named 'f' is already defined in this scope
    //     static event System.Action x6  = (System.Action f = null);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "f").WithArguments("f").WithLocation(25, 53),
    // (31,34): error CS0103: The name 'h1' does not exist in the current context
    //     const decimal x81 = (decimal)h1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "h1").WithArguments("h1").WithLocation(31, 34),
    // (15,15): error CS0103: The name 'a1' does not exist in the current context
    //     int x11 = a1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "a1").WithArguments("a1").WithLocation(15, 15),
    // (17,22): error CS0103: The name 'b1' does not exist in the current context
    //     int x21 {get;} = b1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "b1").WithArguments("b1").WithLocation(17, 22),
    // (19,37): error CS0103: The name 'c1' does not exist in the current context
    //     event System.Action x31  = ()=> c1 = 2;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "c1").WithArguments("c1").WithLocation(19, 37)
                );
        }

        [Fact]
        public void InitializationScope_27()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

partial class Test1
{
    static event System.Action x = (System.Action a = null)+=(System.Action b = null)+=(System.Action c = null)+=(System.Action d = null)+=(System.Action e = null)+=(System.Action f = null)+=(System.Action g = null)+=(System.Action h = null);
    static event System.Action y = (System.Action a1 = null)+=(System.Action b1 = null)+=(System.Action c1= null)+=(System.Action d1 = null)+=(System.Action e1 = null)+=(System.Action f1 = null)+=(System.Action g1 = null)+=(System.Action h1 = null);

    System.Action x1 = (System.Action a = null);
    System.Action x11 = a1;
    System.Action x2 {get;} = (System.Action b = null);
    System.Action x21 {get;} = b1;
    event System.Action x3  = (System.Action c = null);
    event System.Action x31  = c1;

    static System.Action x4 = (System.Action d = null);
    static System.Action x41 = d1;
    static System.Action x5 {get;} = (System.Action e = null);
    static System.Action x51 {get;} = e1;
    static event System.Action x6  = (System.Action f = null);
    static event System.Action x61  = f1;

    const int x7 = (int g = 1);
    const int x71 = g1;
    const decimal x8 = (decimal h = 1);
    const decimal x81 = (decimal)h1;
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (28,20): error CS0133: The expression being assigned to 'Test1.x7' must be constant
    //     const int x7 = (int g = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int g = 1)").WithArguments("Test1.x7").WithLocation(28, 20),
    // (29,21): error CS0103: The name 'g1' does not exist in the current context
    //     const int x71 = g1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "g1").WithArguments("g1").WithLocation(29, 21),
    // (30,24): error CS0133: The expression being assigned to 'Test1.x8' must be constant
    //     const decimal x8 = (decimal h = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(decimal h = 1)").WithArguments("Test1.x8").WithLocation(30, 24),
    // (31,34): error CS0103: The name 'h1' does not exist in the current context
    //     const decimal x81 = (decimal)h1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "h1").WithArguments("h1").WithLocation(31, 34),
    // (21,46): error CS0128: A local variable named 'd' is already defined in this scope
    //     static System.Action x4 = (System.Action d = null);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "d").WithArguments("d").WithLocation(21, 46),
    // (23,53): error CS0128: A local variable named 'e' is already defined in this scope
    //     static System.Action x5 {get;} = (System.Action e = null);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "e").WithArguments("e").WithLocation(23, 53),
    // (25,53): error CS0128: A local variable named 'f' is already defined in this scope
    //     static event System.Action x6  = (System.Action f = null);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "f").WithArguments("f").WithLocation(25, 53),
    // (31,34): error CS0103: The name 'h1' does not exist in the current context
    //     const decimal x81 = (decimal)h1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "h1").WithArguments("h1").WithLocation(31, 34),
    // (15,25): error CS0103: The name 'a1' does not exist in the current context
    //     System.Action x11 = a1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "a1").WithArguments("a1").WithLocation(15, 25),
    // (17,32): error CS0103: The name 'b1' does not exist in the current context
    //     System.Action x21 {get;} = b1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "b1").WithArguments("b1").WithLocation(17, 32),
    // (19,32): error CS0103: The name 'c1' does not exist in the current context
    //     event System.Action x31  = c1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "c1").WithArguments("c1").WithLocation(19, 32)
                );
        }

        [Fact]
        public void InitializationScope_28()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

partial class Test1
{
    const int x = (int a = 1)+(int b = 1)+(int c = 1)+(int d = 1)+(int e = 1)+(int f = 1)+(int g = 1)+(int h = 1);
    const int y = (int a1 = 1)+(int b1 = 1)+(int c1 = 1)+(int d1 = 1)+(int e1 = 1)+(int f1 = 1)+(int g1 = 1)+(int h1 = 1);

    int x1 = (int a = 1);
    int x11 = a1;
    int x2 {get;} = (int b = 1);
    int x21 {get;} = b1;
    event System.Action x3  = (System.Action c = null);
    event System.Action x31  = ()=> c1 = 2;

    static int x4 = (int d = 1);
    static int x41 = d1;
    static int x5 {get;} = (int e = 1);
    static int x51 {get;} = e1;
    static event System.Action x6  = (System.Action f = null);
    static event System.Action x61  = ()=> f1 = 2;

    const int x7 = (int g = 1);
    const int x71 = g1;
    const decimal x8 = (decimal h = 1);
    const decimal x81 = (decimal)h1;
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (12,19): error CS0133: The expression being assigned to 'Test1.y' must be constant
    //     const int y = (int a1 = 1)+(int b1 = 1)+(int c1 = 1)+(int d1 = 1)+(int e1 = 1)+(int f1 = 1)+(int g1 = 1)+(int h1 = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int a1 = 1)+(int b1 = 1)+(int c1 = 1)+(int d1 = 1)+(int e1 = 1)+(int f1 = 1)+(int g1 = 1)+(int h1 = 1)").WithArguments("Test1.y").WithLocation(12, 19),
    // (28,20): error CS0133: The expression being assigned to 'Test1.x7' must be constant
    //     const int x7 = (int g = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int g = 1)").WithArguments("Test1.x7").WithLocation(28, 20),
    // (29,21): error CS0103: The name 'g1' does not exist in the current context
    //     const int x71 = g1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "g1").WithArguments("g1").WithLocation(29, 21),
    // (30,24): error CS0133: The expression being assigned to 'Test1.x8' must be constant
    //     const decimal x8 = (decimal h = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(decimal h = 1)").WithArguments("Test1.x8").WithLocation(30, 24),
    // (31,34): error CS0103: The name 'h1' does not exist in the current context
    //     const decimal x81 = (decimal)h1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "h1").WithArguments("h1").WithLocation(31, 34),
    // (11,19): error CS0133: The expression being assigned to 'Test1.x' must be constant
    //     const int x = (int a = 1)+(int b = 1)+(int c = 1)+(int d = 1)+(int e = 1)+(int f = 1)+(int g = 1)+(int h = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int a = 1)+(int b = 1)+(int c = 1)+(int d = 1)+(int e = 1)+(int f = 1)+(int g = 1)+(int h = 1)").WithArguments("Test1.x").WithLocation(11, 19),
    // (22,22): error CS0103: The name 'd1' does not exist in the current context
    //     static int x41 = d1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "d1").WithArguments("d1").WithLocation(22, 22),
    // (24,29): error CS0103: The name 'e1' does not exist in the current context
    //     static int x51 {get;} = e1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "e1").WithArguments("e1").WithLocation(24, 29),
    // (26,44): error CS0103: The name 'f1' does not exist in the current context
    //     static event System.Action x61  = ()=> f1 = 2;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "f1").WithArguments("f1").WithLocation(26, 44),
    // (31,34): error CS0103: The name 'h1' does not exist in the current context
    //     const decimal x81 = (decimal)h1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "h1").WithArguments("h1").WithLocation(31, 34),
    // (15,15): error CS0103: The name 'a1' does not exist in the current context
    //     int x11 = a1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "a1").WithArguments("a1").WithLocation(15, 15),
    // (17,22): error CS0103: The name 'b1' does not exist in the current context
    //     int x21 {get;} = b1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "b1").WithArguments("b1").WithLocation(17, 22),
    // (19,37): error CS0103: The name 'c1' does not exist in the current context
    //     event System.Action x31  = ()=> c1 = 2;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "c1").WithArguments("c1").WithLocation(19, 37)
                );
        }

        [Fact]
        public void InitializationScope_29()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

partial class Test1
{
    const decimal x = (int a = 1)+(int b = 1)+(int c = 1)+(int d = 1)+(int e = 1)+(int f = 1)+(int g = 1)+(int h = 1);
    const decimal y = (int a1 = 1)+(int b1 = 1)+(int c1 = 1)+(int d1 = 1)+(int e1 = 1)+(int f1 = 1)+(int g1 = 1)+(int h1 = 1);

    int x1 = (int a = 1);
    int x11 = a1;
    int x2 {get;} = (int b = 1);
    int x21 {get;} = b1;
    event System.Action x3  = (System.Action c = null);
    event System.Action x31  = ()=> c1 = 2;

    static int x4 = (int d = 1);
    static int x41 = d1;
    static int x5 {get;} = (int e = 1);
    static int x51 {get;} = e1;
    static event System.Action x6  = (System.Action f = null);
    static event System.Action x61  = ()=> f1 = 2;

    const int x7 = (int g = 1);
    const int x71 = g1;
    const decimal x8 = (decimal h = 1);
    const decimal x81 = (decimal)h1;
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (12,23): error CS0133: The expression being assigned to 'Test1.y' must be constant
    //     const decimal y = (int a1 = 1)+(int b1 = 1)+(int c1 = 1)+(int d1 = 1)+(int e1 = 1)+(int f1 = 1)+(int g1 = 1)+(int h1 = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int a1 = 1)+(int b1 = 1)+(int c1 = 1)+(int d1 = 1)+(int e1 = 1)+(int f1 = 1)+(int g1 = 1)+(int h1 = 1)").WithArguments("Test1.y").WithLocation(12, 23),
    // (28,20): error CS0133: The expression being assigned to 'Test1.x7' must be constant
    //     const int x7 = (int g = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int g = 1)").WithArguments("Test1.x7").WithLocation(28, 20),
    // (29,21): error CS0103: The name 'g1' does not exist in the current context
    //     const int x71 = g1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "g1").WithArguments("g1").WithLocation(29, 21),
    // (30,24): error CS0133: The expression being assigned to 'Test1.x8' must be constant
    //     const decimal x8 = (decimal h = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(decimal h = 1)").WithArguments("Test1.x8").WithLocation(30, 24),
    // (31,34): error CS0103: The name 'h1' does not exist in the current context
    //     const decimal x81 = (decimal)h1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "h1").WithArguments("h1").WithLocation(31, 34),
    // (11,23): error CS0133: The expression being assigned to 'Test1.x' must be constant
    //     const decimal x = (int a = 1)+(int b = 1)+(int c = 1)+(int d = 1)+(int e = 1)+(int f = 1)+(int g = 1)+(int h = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int a = 1)+(int b = 1)+(int c = 1)+(int d = 1)+(int e = 1)+(int f = 1)+(int g = 1)+(int h = 1)").WithArguments("Test1.x").WithLocation(11, 23),
    // (22,22): error CS0103: The name 'd1' does not exist in the current context
    //     static int x41 = d1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "d1").WithArguments("d1").WithLocation(22, 22),
    // (24,29): error CS0103: The name 'e1' does not exist in the current context
    //     static int x51 {get;} = e1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "e1").WithArguments("e1").WithLocation(24, 29),
    // (26,44): error CS0103: The name 'f1' does not exist in the current context
    //     static event System.Action x61  = ()=> f1 = 2;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "f1").WithArguments("f1").WithLocation(26, 44),
    // (31,34): error CS0103: The name 'h1' does not exist in the current context
    //     const decimal x81 = (decimal)h1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "h1").WithArguments("h1").WithLocation(31, 34),
    // (15,15): error CS0103: The name 'a1' does not exist in the current context
    //     int x11 = a1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "a1").WithArguments("a1").WithLocation(15, 15),
    // (17,22): error CS0103: The name 'b1' does not exist in the current context
    //     int x21 {get;} = b1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "b1").WithArguments("b1").WithLocation(17, 22),
    // (19,37): error CS0103: The name 'c1' does not exist in the current context
    //     event System.Action x31  = ()=> c1 = 2;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "c1").WithArguments("c1").WithLocation(19, 37)
                );
        }

        [Fact]
        public void InitializationScope_30()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

class Base
{
    public Base(int x1, int x2, System.Action x3, int x4, int x5, System.Action x6, int x7, decimal x8){}
}

class Test1 : Base
{
    int x1 = (int a = 1);
    int x2 {get;} = (int b = 1);
    event System.Action x3  = (System.Action c = null);

    static int x4 = (int d = 1);
    static int x5 {get;} = (int e = 1);
    static event System.Action x6  = (System.Action f = null);

    const int x7 = (int g = 1);
    const decimal x8 = (decimal h = 1);

    Test1() : base(
        (int a = 1), 
        (int b = 1), 
        (System.Action c = null), 
        (int d = 1),
        (int e = 1),
        (System.Action f = null),
        (int g = 1),
        (decimal h = 1))
    {}
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (24,20): error CS0133: The expression being assigned to 'Test1.x7' must be constant
    //     const int x7 = (int g = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int g = 1)").WithArguments("Test1.x7").WithLocation(24, 20),
    // (25,24): error CS0133: The expression being assigned to 'Test1.x8' must be constant
    //     const decimal x8 = (decimal h = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(decimal h = 1)").WithArguments("Test1.x8").WithLocation(25, 24)
                );
        }

        [Fact]
        public void InitializationScope_31()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

class Base
{
    public Base(int x1, int x2, System.Action x3, int x4, int x5, System.Action x6, int x7, decimal x8){}
}

class Test1() : Base(
        (int a = 1), 
        (int b = 1), 
        (System.Action c = null), 
        (int d = 1),
        (int e = 1),
        (System.Action f = null),
        (int g = 1),
        (decimal h = 1))
{
    int x1 = (int a = 1);
    int x2 {get;} = (int b = 1);
    event System.Action x3  = (System.Action c = null);

    static int x4 = (int d = 1);
    static int x5 {get;} = (int e = 1);
    static event System.Action x6  = (System.Action f = null);

    const int x7 = (int g = 1);
    const decimal x8 = (decimal h = 1);
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (32,20): error CS0133: The expression being assigned to 'Test1.x7' must be constant
    //     const int x7 = (int g = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int g = 1)").WithArguments("Test1.x7").WithLocation(32, 20),
    // (33,24): error CS0133: The expression being assigned to 'Test1.x8' must be constant
    //     const decimal x8 = (decimal h = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(decimal h = 1)").WithArguments("Test1.x8").WithLocation(33, 24),
    // (15,14): error CS0136: A local or parameter named 'a' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         (int a = 1), 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a").WithArguments("a").WithLocation(15, 14),
    // (16,14): error CS0136: A local or parameter named 'b' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         (int b = 1), 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "b").WithArguments("b").WithLocation(16, 14),
    // (17,24): error CS0136: A local or parameter named 'c' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         (System.Action c = null), 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c").WithArguments("c").WithLocation(17, 24)
                );
        }

        [Fact]
        public void InitializationScope_32()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

class Base
{
    public Base(int x1, int x2, System.Action x3, int x4, int x5, System.Action x6, int x7, decimal x8){}
}

class Test1 : Base
{
    int x1 = (int a = 1);
    int x2 {get;} = (int b = 1);
    event System.Action x3  = (System.Action c = null);

    static int x4 = (int d = 1);
    static int x5 {get;} = (int e = 1);
    static event System.Action x6  = (System.Action f = null);

    const int x7 = (int g = 1);
    const decimal x8 = (decimal h = 1);

    Test1() : base(
        a, 
        b, 
        c, 
        d,
        e,
        f,
        g,
        h)
    {}
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (24,20): error CS0133: The expression being assigned to 'Test1.x7' must be constant
    //     const int x7 = (int g = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int g = 1)").WithArguments("Test1.x7").WithLocation(24, 20),
    // (25,24): error CS0133: The expression being assigned to 'Test1.x8' must be constant
    //     const decimal x8 = (decimal h = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(decimal h = 1)").WithArguments("Test1.x8").WithLocation(25, 24),
    // (28,9): error CS0103: The name 'a' does not exist in the current context
    //         a, 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(28, 9),
    // (29,9): error CS0103: The name 'b' does not exist in the current context
    //         b, 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(29, 9),
    // (30,9): error CS0103: The name 'c' does not exist in the current context
    //         c, 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "c").WithArguments("c").WithLocation(30, 9),
    // (31,9): error CS0103: The name 'd' does not exist in the current context
    //         d,
    Diagnostic(ErrorCode.ERR_NameNotInContext, "d").WithArguments("d").WithLocation(31, 9),
    // (32,9): error CS0103: The name 'e' does not exist in the current context
    //         e,
    Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(32, 9),
    // (33,9): error CS0103: The name 'f' does not exist in the current context
    //         f,
    Diagnostic(ErrorCode.ERR_NameNotInContext, "f").WithArguments("f").WithLocation(33, 9),
    // (34,9): error CS0103: The name 'g' does not exist in the current context
    //         g,
    Diagnostic(ErrorCode.ERR_NameNotInContext, "g").WithArguments("g").WithLocation(34, 9),
    // (35,9): error CS0103: The name 'h' does not exist in the current context
    //         h)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "h").WithArguments("h").WithLocation(35, 9)
                );
        }

        [Fact]
        public void InitializationScope_33()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

class Base
{
    public Base(int x1, int x2, System.Action x3, int x4, int x5, System.Action x6, int x7, decimal x8){}
}

class Test1() : Base(
        a, 
        b, 
        c, 
        d,
        e,
        f,
        g,
        h)
{
    int x1 = (int a = 1);
    int x2 {get;} = (int b = 1);
    event System.Action x3  = (System.Action c = null);

    static int x4 = (int d = 1);
    static int x5 {get;} = (int e = 1);
    static event System.Action x6  = (System.Action f = null);

    const int x7 = (int g = 1);
    const decimal x8 = (decimal h = 1);
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (32,20): error CS0133: The expression being assigned to 'Test1.x7' must be constant
    //     const int x7 = (int g = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int g = 1)").WithArguments("Test1.x7").WithLocation(32, 20),
    // (33,24): error CS0133: The expression being assigned to 'Test1.x8' must be constant
    //     const decimal x8 = (decimal h = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(decimal h = 1)").WithArguments("Test1.x8").WithLocation(33, 24),
    // (15,9): error CS0841: Cannot use local variable 'a' before it is declared
    //         a, 
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "a").WithArguments("a").WithLocation(15, 9),
    // (16,9): error CS0841: Cannot use local variable 'b' before it is declared
    //         b, 
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "b").WithArguments("b").WithLocation(16, 9),
    // (17,9): error CS0841: Cannot use local variable 'c' before it is declared
    //         c, 
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "c").WithArguments("c").WithLocation(17, 9),
    // (18,9): error CS0103: The name 'd' does not exist in the current context
    //         d,
    Diagnostic(ErrorCode.ERR_NameNotInContext, "d").WithArguments("d").WithLocation(18, 9),
    // (19,9): error CS0103: The name 'e' does not exist in the current context
    //         e,
    Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(19, 9),
    // (20,9): error CS0103: The name 'f' does not exist in the current context
    //         f,
    Diagnostic(ErrorCode.ERR_NameNotInContext, "f").WithArguments("f").WithLocation(20, 9),
    // (21,9): error CS0103: The name 'g' does not exist in the current context
    //         g,
    Diagnostic(ErrorCode.ERR_NameNotInContext, "g").WithArguments("g").WithLocation(21, 9),
    // (22,9): error CS0103: The name 'h' does not exist in the current context
    //         h)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "h").WithArguments("h").WithLocation(22, 9)
                );
        }

        [Fact]
        public void InitializationScope_34()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

class Test1
{
    int x1 = (int a = 1);
    int x2 {get;} = (int b = 1);
    event System.Action x3  = (System.Action c = null);

    static int x4 = (int d = 1);
    static int x5 {get;} = (int e = 1);
    static event System.Action x6  = (System.Action f = null);

    const int x7 = (int g = 1);
    const decimal x8 = (decimal h = 1);

    Test1()
    {
        System.Console.WriteLine(int a = 1); 
        System.Console.WriteLine(int b = 1); 
        System.Console.WriteLine(System.Action c = null); 
        System.Console.WriteLine(int d = 1);
        System.Console.WriteLine(int e = 1);
        System.Console.WriteLine(System.Action f = null);
        System.Console.WriteLine(int g = 1);
        System.Console.WriteLine(decimal h = 1);
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (19,20): error CS0133: The expression being assigned to 'Test1.x7' must be constant
    //     const int x7 = (int g = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int g = 1)").WithArguments("Test1.x7").WithLocation(19, 20),
    // (20,24): error CS0133: The expression being assigned to 'Test1.x8' must be constant
    //     const decimal x8 = (decimal h = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(decimal h = 1)").WithArguments("Test1.x8").WithLocation(20, 24)
                );
        }

        [Fact]
        public void InitializationScope_35()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

class Test1
{
    int x1 = (int a = 1);
    int x2 {get;} = (int b = 1);
    event System.Action x3  = (System.Action c = null);

    static int x4 = (int d = 1);
    static int x5 {get;} = (int e = 1);
    static event System.Action x6  = (System.Action f = null);

    const int x7 = (int g = 1);
    const decimal x8 = (decimal h = 1);

    Test1()
    {
        System.Console.WriteLine(a); 
        System.Console.WriteLine(b); 
        System.Console.WriteLine(c); 
        System.Console.WriteLine(d);
        System.Console.WriteLine(e);
        System.Console.WriteLine(f);
        System.Console.WriteLine(g);
        System.Console.WriteLine(h);
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (19,20): error CS0133: The expression being assigned to 'Test1.x7' must be constant
    //     const int x7 = (int g = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int g = 1)").WithArguments("Test1.x7").WithLocation(19, 20),
    // (20,24): error CS0133: The expression being assigned to 'Test1.x8' must be constant
    //     const decimal x8 = (decimal h = 1);
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "(decimal h = 1)").WithArguments("Test1.x8").WithLocation(20, 24),
    // (24,34): error CS0103: The name 'a' does not exist in the current context
    //         System.Console.WriteLine(a); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(24, 34),
    // (25,34): error CS0103: The name 'b' does not exist in the current context
    //         System.Console.WriteLine(b); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(25, 34),
    // (26,34): error CS0103: The name 'c' does not exist in the current context
    //         System.Console.WriteLine(c); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "c").WithArguments("c").WithLocation(26, 34),
    // (27,34): error CS0103: The name 'd' does not exist in the current context
    //         System.Console.WriteLine(d);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "d").WithArguments("d").WithLocation(27, 34),
    // (28,34): error CS0103: The name 'e' does not exist in the current context
    //         System.Console.WriteLine(e);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(28, 34),
    // (29,34): error CS0103: The name 'f' does not exist in the current context
    //         System.Console.WriteLine(f);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "f").WithArguments("f").WithLocation(29, 34),
    // (30,34): error CS0103: The name 'g' does not exist in the current context
    //         System.Console.WriteLine(g);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "g").WithArguments("g").WithLocation(30, 34),
    // (31,34): error CS0103: The name 'h' does not exist in the current context
    //         System.Console.WriteLine(h);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "h").WithArguments("h").WithLocation(31, 34)
                );
        }

        [Fact]
        public void InitializationScope_36()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

class Base
{
    public Base(int x1, int x2){}
}

class Test1() : Base(
        (int a = 1), 
        (int a = 2))
{
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (16,14): error CS0128: A local variable named 'a' is already defined in this scope
    //         (int a = 2))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "a").WithArguments("a").WithLocation(16, 14)
                );
        }

        [Fact]
        public void InitializationScope_37()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

class Base
{
    public Base(int x1, int x2){}
}

class Test1 : Base
{
    Test1() : base(
        (int a = 1), 
        (int a = 2))
    {}
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (18,14): error CS0128: A local variable named 'a' is already defined in this scope
    //         (int a = 2))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "a").WithArguments("a").WithLocation(18, 14)
                );
        }

        [Fact]
        public void InitializationScope_38()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
    }
}

class Base
{
    public Base(int x1, int x2){}
}

class Test1 : Base
{
    Test1() : base(
        (int a = 1), 
        (int b = 2))
    {
        System.Console.WriteLine(a);
        System.Console.WriteLine(int b = 3);
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            compilation.VerifyDiagnostics(
    // (21,38): error CS0136: A local or parameter named 'b' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         System.Console.WriteLine(int b = 3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "b").WithArguments("b").WithLocation(21, 38)
                );
        }

        [Fact]
        public void InitializationScope_39()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var x = new Test();
        if (x != null)
        {
            x = null;
        }
    }
}

class Base
{
    public Base(int x1, System.Func<int> x2, int x3)
    {
        System.Console.WriteLine(""{0} {1} {2}"", x1, x2(), x3);
    }
}

class Test() : Base((int x = 10)++, ()=>x, x++)
{
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "10 12 11").VerifyDiagnostics();
        }

        [Fact]
        public void InitializationScope_40()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var x = new Test();
        if (x != null)
        {
            x = null;
        }
    }
}

class Base
{
    public Base(int x1, System.Func<int> x2, int x3)
    {
        System.Console.WriteLine(""{0} {1} {2}"", x1, x2(), x3);
    }
}

class Test(int x1, System.Func<int> x2, int x3) : Base(x1, x2, x3)
{
    public Test() : this((int x = 100)++, ()=>x, x++)
    {}
}
"; 
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "100 102 101").VerifyDiagnostics();
        }

        [Fact]
        public void InitializationScope_41()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var x = new Test();
        if (x != null)
        {
            x = null;
        }
    }
}

class Test(int x1, System.Func<int> x2)
{
    public Test() : this((int x = 200)++, System.Func<int> y = ()=>x)
    {
        System.Console.WriteLine(""{0} {1}"", x++, y());
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "201 202").VerifyDiagnostics();
        }

        [Fact]
        public void InitializationScope_42()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var x = new Test();
        if (x != null)
        {
            x = null;
        }
    }
}

class Test(int x1)
{
    public Test() : this(int x = 300)
    {
        System.Func<int> y = ()=>x;
        System.Console.WriteLine(""{0} {1}"", x++, y());
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(compilation, expectedOutput: "300 301").VerifyDiagnostics();
        }

    }
}