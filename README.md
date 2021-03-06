<h1 align="center">ScriptBlockDisassembler</h1>

<p align="center">
    <sub>
        Show a pseudo-C# representation of the code that the PowerShell compiler generates for a given ScriptBlock.
    </sub>
    <br /><br />
    <a title="Commits" href="https://github.com/SeeminglyScience/ScriptBlockDisassembler/commits/master">
        <img alt="Build Status" src="https://github.com/SeeminglyScience/ScriptBlockDisassembler/workflows/build/badge.svg" />
    </a>
    <a title="ScriptBlockDisassembler on PowerShell Gallery" href="https://www.powershellgallery.com/packages/ScriptBlockDisassembler">
        <img alt="PowerShell Gallery Version (including pre-releases)" src="https://img.shields.io/powershellgallery/v/ScriptBlockDisassembler?include_prereleases&label=gallery">
    </a>
    <a title="LICENSE" href="https://github.com/SeeminglyScience/ScriptBlockDisassembler/blob/master/LICENSE">
         <img alt="GitHub" src="https://img.shields.io/github/license/SeeminglyScience/ScriptBlockDisassembler">
    </a>
</p>

## Install

```powershell
Install-Module ScriptBlockDisassembler -Scope CurrentUser -Force
```

## Why

Ever try to read [`Compiler.cs`][compiler] in [PowerShell/PowerShell][powershell]? It's doable, but tedious. Especially for more complex issues it'd be nice to just get a readable version of the final expression tree.

So I wrote this. It forces the `ScriptBlock` to be compiled, and then digs into it with reflection to find the LINQ expression tree it generated. Then runs it through [ReadableExpressions][readable] with some PowerShell specific customizations and boom we got something much easier to understand.

You may want this if:

1. You're working on the compiler
2. You're just curious how certain PowerShell code is compiled

## Demo

```powershell
{ $a = 10 } | Get-ScriptBlockDisassembly
```

```csharp
// ScriptBlock.EndBlock
(FunctionContext funcContext) =>
{
    ExecutionContext context;
    try
    {
        context = funcContext._executionContext;
        MutableTuple<object, object[], object, object, PSScriptCmdlet, PSBoundParametersDictionary, InvocationInfo, string, string, object, LanguagePrimitives.Null, LanguagePrimitives.Null, LanguagePrimitives.Null, LanguagePrimitives.Null, LanguagePrimitives.Null, LanguagePrimitives.Null> locals = (MutableTuple<object, object[], object, object, PSScriptCmdlet, PSBoundParametersDictionary, InvocationInfo, string, string, object, LanguagePrimitives.Null, LanguagePrimitives.Null, LanguagePrimitives.Null, LanguagePrimitives.Null, LanguagePrimitives.Null, LanguagePrimitives.Null>)funcContext._localsTuple;
        funcContext._functionName = "<ScriptBlock>";
        funcContext._currentSequencePointIndex = 0;
        context._debugger.EnterScriptFunction(funcContext);
        try
        {
            funcContext._currentSequencePointIndex = 1;

            if (context._debuggingMode > 0)
            {
                context._debugger.OnSequencePointHit(funcContext);
            }

            // Note, this here is the actual $a = 10
            locals.Item009 = 10;
            context.QuestionMarkVariableValue = true;
        }
        catch (FlowControlException)
        {
            throw;
        }
        catch (Exception exception)
        {
            ExceptionHandlingOps.CheckActionPreference(funcContext, exception);
        }
        funcContext._currentSequencePointIndex = 2;

        if (context._debuggingMode > 0)
        {
            context._debugger.OnSequencePointHit(funcContext);
        }

    }
    finally
    {
        context._debugger.ExitScriptFunction();
    }
}
```

## How do I read it?

Each named block has it's own delegate (of type `Action<FunctionContext>`) that will be disassembled. So at minimum
each block will look like this:

```csharp
// ScriptBlock.EndBlock
(FunctionContext context) =>
{
}
```

Any time you see a static method called from the class `Fake`, that is a representation of something
not directly translatable to C#.

### Fake.Dynamic

Any time you see `Fake.Dynamic` you should imagine it as having this signature:

```csharp
class Fake
{
    public static TDelegate Dynamic<TDelegate>(DynamicMetaObjectBinder binder);
}
```

So a call to `Fake.Dynamic<Action<int>>(PSPipeWriterBinder.Get())(10)` would get a `Action<int>` from
`Fake.Dynamic` using the `PSPipeWriterBinder` call site binder and then invoke it.

In reality the call looks roughly more like:

```csharp
closure.Constants[0].Target.Invoke(closure.Constants[0], closure.Constants[1], 10);
```

But that doesn't really tell you anything helpful without examining the constants directly.

### Fake.Const

This represents the retrieval of a constant that is embedded in the delegate.

```csharp
Fake.Const<CommandBaseAst>(typeof(CommandExpressionAst), "10");
//             |                      |                    |
//             |                      |                    -- ToString value
//             |                      -- Optional runtime type if different than static type
//             -- What the constant is statically typed as
```

## What about binders? How can I read those?

I've added the `Format-ExpressionTree` command to help with that. You're currently
on your own as far as obtaining the expression (I recommend using [ImpliedReflection][ImpliedReflection]),
but once you've obtained one you can pipe it to that command.

## Should I use this in a production environment?

No. I don't know why you would, but don't. It relies heavily on implementation detail
and will certainly break eventually. Maybe even with a minor release.

This module should only really ever be used interactively for troubleshooting or exploration.

The code is also far from optimal in a lot of places. Please don't use it as an example of what you should do.

## Can I compile the C#?

No. The LINQ expression tree that PowerShell generates makes *heavy* use of constant
expressions that cannot easily be translated to pure source code. Any time you see
a method call on a class called `Fake`, that's just some psuedo code I put in to
express what is happening.

It also makes heavy use of dynamic expressions. For these I use the state of the passed
binder to recreate an approximation of it's construction.

Also most of the API's called in the disassemblied result are non-public.

Also LINQ expression trees let you do things like fit a whole block of statements into a single expression.

## Optimized vs unoptimized

There are two modes for the compiler, optimized and unoptimized. By default this command will return the optimized version, but the `-Unoptimized` switch can be specified to change that.

Here are some common reasons the compiler will naturally enter the unoptimized mode:

1. Dot sourcing
2. Static analysis found the use of a `*-Variable` command
3. Static analysis found the use of *any* debugger command
4. Static analysis found references to any `AllScope` variables

Optimization mostly affects how access of local variables are generated.

## Why doesn't this work on PowerShell versions older than 7.2

I just didn't see the need and it would require me to make sure all the private fields for every binder are still the same. If you need this please open an issue.

[readable]: https://github.com/agileobjects/ReadableExpressions "agileobjects/ReadableExpressions"
[compiler]: https://github.com/PowerShell/PowerShell/blob/master/src/System.Management.Automation/engine/parser/Compiler.cs "Compiler.cs"
[powershell]: https://github.com/PowerShell/PowerShell "PowerShell/PowerShell"
[ImpliedReflection]: https://github.com/SeeminglyScience/ImpliedReflection "SeeminglyScience/ImpliedReflection"
