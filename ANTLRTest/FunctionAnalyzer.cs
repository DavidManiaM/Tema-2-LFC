using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ANTLRTest
{
    /// <summary>
    /// Represents detailed information about a function.
    /// </summary>
    public class FunctionInfo
    {
        public string Name { get; set; } = string.Empty;
  public string ReturnType { get; set; } = string.Empty;
        public bool IsMain { get; set; }
        public bool IsRecursive { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
     public List<ParameterInfo> Parameters { get; set; } = new();
        public List<LocalVariableInfo> LocalVariables { get; set; } = new();
    public List<ControlStructureInfo> ControlStructures { get; set; } = new();
    }

    /// <summary>
    /// Represents a function parameter.
    /// </summary>
    public class ParameterInfo
    {
   public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public override string ToString() => $"{Type} {Name}";
    }

    /// <summary>
    /// Represents a local variable declared in a function.
    /// </summary>
    public class LocalVariableInfo
    {
     public string Type { get; set; } = string.Empty;
 public string Name { get; set; } = string.Empty;
        public string InitialValue { get; set; } = string.Empty;
  public int LineNumber { get; set; }

        public override string ToString() => string.IsNullOrEmpty(InitialValue)
  ? $"{Type} {Name}"
            : $"{Type} {Name} = {InitialValue}";
    }

    /// <summary>
    /// Represents a control structure (if, while, for) in a function.
    /// </summary>
    public class ControlStructureInfo
    {
        public string StructureType { get; set; } = string.Empty; // "if", "if...else", "while", "for"
     public int LineNumber { get; set; }

  public override string ToString() => $"<{StructureType}, {LineNumber}>";
    }

    /// <summary>
    /// Analyzes functions from the parsed program and extracts detailed information.
    /// </summary>
    public class FunctionAnalyzer
    {
     private readonly List<FunctionInfo> _functions = new();

     /// <summary>
        /// Analyzes the program context and extracts all function information.
        /// </summary>
     public List<FunctionInfo> Analyze(CompilerParser.ProgramContext context)
        {
            _functions.Clear();

            // Analyze user-defined functions from global declarations
         foreach (var globalDecl in context.globalDeclaration())
 {
      if (globalDecl.functionDeclaration() != null)
    {
     var funcInfo = AnalyzeFunctionDeclaration(globalDecl.functionDeclaration());
    _functions.Add(funcInfo);
     }
            }

  // Analyze main function
        var mainFunc = context.mainFunction();
            if (mainFunc != null)
      {
       var mainInfo = AnalyzeMainFunction(mainFunc);
     _functions.Add(mainInfo);
            }

            // Check for recursive calls after all functions are registered
          foreach (var func in _functions)
     {
     func.IsRecursive = CheckIfRecursive(func.Name, context);
          }

 return _functions;
}

        /// <summary>
        /// Analyzes a user-defined function declaration.
   /// </summary>
        private FunctionInfo AnalyzeFunctionDeclaration(CompilerParser.FunctionDeclarationContext context)
        {
       var funcInfo = new FunctionInfo
            {
     Name = context.ID().GetText(),
        ReturnType = context.varType().GetText(),
                IsMain = false,
     StartLine = context.Start.Line,
  EndLine = context.Stop.Line
    };

 // Extract parameters
         var paramList = context.parameterList();
       if (paramList != null)
 {
                foreach (var param in paramList.parameter())
        {
     funcInfo.Parameters.Add(new ParameterInfo
    {
      Type = param.varType().GetText(),
   Name = param.ID().GetText()
    });
    }
 }

      // Analyze statements for local variables and control structures
            foreach (var stmt in context.statement())
       {
             AnalyzeStatement(stmt, funcInfo);
 }

       return funcInfo;
        }

     /// <summary>
        /// Analyzes the main function.
        /// </summary>
     private FunctionInfo AnalyzeMainFunction(CompilerParser.MainFunctionContext context)
        {
            var funcInfo = new FunctionInfo
            {
 Name = "main",
 ReturnType = "int",
 IsMain = true,
      StartLine = context.Start.Line,
          EndLine = context.Stop.Line
   };

            // Main function has no parameters in this grammar

       // Analyze statements for local variables and control structures
  foreach (var stmt in context.statement())
            {
    AnalyzeStatement(stmt, funcInfo);
          }

            return funcInfo;
        }

        /// <summary>
    /// Analyzes a statement to extract local variables and control structures.
  /// </summary>
        private void AnalyzeStatement(CompilerParser.StatementContext context, FunctionInfo funcInfo)
        {
    // Check for variable declaration
        if (context.varDeclaration() != null)
            {
            ExtractLocalVariables(context.varDeclaration(), funcInfo);
        }

            // Check for if statement
 if (context.ifStatement() != null)
        {
     AnalyzeIfStatement(context.ifStatement(), funcInfo);
 }

     // Check for for statement
       if (context.forStatement() != null)
      {
         AnalyzeForStatement(context.forStatement(), funcInfo);
      }

            // Check for while statement
            if (context.whileStatement() != null)
        {
         AnalyzeWhileStatement(context.whileStatement(), funcInfo);
  }
        }

/// <summary>
        /// Extracts local variables from a variable declaration.
        /// </summary>
        private void ExtractLocalVariables(CompilerParser.VarDeclarationContext context, FunctionInfo funcInfo)
        {
            var varType = context.varType().GetText();
         var idNodes = context.ID();
            var assignContexts = context.ASSIGN();
            var arithmExprs = context.arithmExpr();
var stringExprs = context.@string();

for (int i = 0; i < idNodes.Length; i++)
     {
         var varName = idNodes[i].GetText();
      var idPosition = idNodes[i].Symbol.TokenIndex;
     var lineNumber = idNodes[i].Symbol.Line;

       string initialValue = "";

    // Check if there is an assignment for this variable
             var hasAssignment = assignContexts.Any(a => a.Symbol.TokenIndex > idPosition &&
       (i == idNodes.Length - 1 || a.Symbol.TokenIndex < idNodes[i + 1].Symbol.TokenIndex));

  if (hasAssignment)
         {
    var arithmExpr = arithmExprs.FirstOrDefault(e => e.Start.TokenIndex > idPosition &&
            (i == idNodes.Length - 1 || e.Start.TokenIndex < idNodes[i + 1].Symbol.TokenIndex));
        var stringExpr = stringExprs.FirstOrDefault(e => e.Start.TokenIndex > idPosition &&
          (i == idNodes.Length - 1 || e.Start.TokenIndex < idNodes[i + 1].Symbol.TokenIndex));

     if (arithmExpr != null)
                {
    initialValue = arithmExpr.GetText();
           }
else if (stringExpr != null)
        {
      initialValue = stringExpr.GetText();
       }
  }

     funcInfo.LocalVariables.Add(new LocalVariableInfo
  {
        Type = varType,
        Name = varName,
       InitialValue = initialValue,
      LineNumber = lineNumber
         });
            }
     }

 /// <summary>
        /// Analyzes an if statement and its nested structures.
      /// </summary>
 private void AnalyzeIfStatement(CompilerParser.IfStatementContext context, FunctionInfo funcInfo)
    {
   var structureType = context.ELSE_TOKEN() != null ? "if...else" : "if";
          var lineNumber = context.IF_TOKEN().Symbol.Line;

    funcInfo.ControlStructures.Add(new ControlStructureInfo
          {
   StructureType = structureType,
                LineNumber = lineNumber
          });

   // Analyze nested statements in if block
   var statements = context.statement();
          var rbraces = context.RBRACE();

     if (context.ELSE_TOKEN() != null && rbraces.Length > 0)
         {
     int firstRbraceIndex = rbraces[0].Symbol.StopIndex;
           foreach (var stmt in statements)
       {
        AnalyzeStatement(stmt, funcInfo);
 }
      }
    else
         {
                foreach (var stmt in statements)
        {
         AnalyzeStatement(stmt, funcInfo);
             }
            }
        }

   /// <summary>
        /// Analyzes a for statement and its nested structures.
        /// </summary>
      private void AnalyzeForStatement(CompilerParser.ForStatementContext context, FunctionInfo funcInfo)
     {
      var lineNumber = context.FOR_TOKEN().Symbol.Line;

  funcInfo.ControlStructures.Add(new ControlStructureInfo
 {
          StructureType = "for",
        LineNumber = lineNumber
            });

        // Extract loop variable from for initialization
            if (context.varDeclaration() != null)
        {
     ExtractLocalVariables(context.varDeclaration(), funcInfo);
            }

    // Analyze nested statements
            foreach (var stmt in context.statement())
            {
        AnalyzeStatement(stmt, funcInfo);
            }
 }

    /// <summary>
        /// Analyzes a while statement and its nested structures.
        /// </summary>
  private void AnalyzeWhileStatement(CompilerParser.WhileStatementContext context, FunctionInfo funcInfo)
        {
            var lineNumber = context.WHILE_TOKEN().Symbol.Line;

     funcInfo.ControlStructures.Add(new ControlStructureInfo
       {
   StructureType = "while",
   LineNumber = lineNumber
      });

  // Analyze nested statements
      foreach (var stmt in context.statement())
        {
    AnalyzeStatement(stmt, funcInfo);
            }
        }

  /// <summary>
        /// Checks if a function is recursive by looking for self-calls.
        /// </summary>
        private bool CheckIfRecursive(string functionName, CompilerParser.ProgramContext programContext)
        {
            // Find the function declaration or main function
        foreach (var globalDecl in programContext.globalDeclaration())
          {
    if (globalDecl.functionDeclaration() != null)
    {
           var funcDecl = globalDecl.functionDeclaration();
     if (funcDecl.ID().GetText() == functionName)
          {
             return ContainsCallTo(funcDecl.statement(), functionName);
                    }
  }
            }

// Check main function
            if (functionName == "main")
      {
         var mainFunc = programContext.mainFunction();
    if (mainFunc != null)
      {
       return ContainsCallTo(mainFunc.statement(), functionName);
     }
      }

            return false;
   }

        /// <summary>
        /// Checks if the statements contain a call to the specified function.
  /// </summary>
 private bool ContainsCallTo(CompilerParser.StatementContext[] statements, string functionName)
  {
          foreach (var stmt in statements)
         {
       if (ContainsCallToInStatement(stmt, functionName))
          return true;
  }
  return false;
        }

        /// <summary>
    /// Recursively checks if a statement contains a call to the specified function.
        /// </summary>
        private bool ContainsCallToInStatement(CompilerParser.StatementContext context, string functionName)
        {
 // Check direct function call
if (context.functionCall() != null)
   {
         var callName = context.functionCall().ID()?.GetText();
      if (callName == functionName)
            return true;
            }

 // Check in variable declaration (function calls in expressions)
         if (context.varDeclaration() != null)
            {
       foreach (var arithmExpr in context.varDeclaration().arithmExpr())
          {
            if (ContainsCallToInArithmExpr(arithmExpr, functionName))
     return true;
  }
            }

    // Check in variable assignment
            if (context.varAssignment() != null && context.varAssignment().varValue() != null)
  {
        var varValue = context.varAssignment().varValue();
 if (varValue.arithmExpr() != null && ContainsCallToInArithmExpr(varValue.arithmExpr(), functionName))
        return true;
         }

         // Check in arithmetic expression statement
   if (context.arithmExpr() != null)
  {
       if (ContainsCallToInArithmExpr(context.arithmExpr(), functionName))
                    return true;
     }

     // Check in if statement
if (context.ifStatement() != null)
          {
  foreach (var stmt in context.ifStatement().statement())
     {
    if (ContainsCallToInStatement(stmt, functionName))
   return true;
        }
   }

            // Check in for statement
      if (context.forStatement() != null)
       {
   foreach (var stmt in context.forStatement().statement())
     {
        if (ContainsCallToInStatement(stmt, functionName))
      return true;
     }
        }

            // Check in while statement
  if (context.whileStatement() != null)
      {
      foreach (var stmt in context.whileStatement().statement())
  {
        if (ContainsCallToInStatement(stmt, functionName))
     return true;
      }
      }

       // Check in return statement
   if (context.returnStatement() != null && context.returnStatement().expression() != null)
        {
   var expr = context.returnStatement().expression();
             if (expr.arithmExpr() != null && ContainsCallToInArithmExpr(expr.arithmExpr(), functionName))
     return true;
            }

return false;
        }

        /// <summary>
        /// Checks if an arithmetic expression contains a call to the specified function.
        /// </summary>
private bool ContainsCallToInArithmExpr(CompilerParser.ArithmExprContext context, string functionName)
   {
            return context switch
            {
      CompilerParser.AddSubExprContext addSub =>
          ContainsCallToInArithmExpr(addSub.arithmExpr(0), functionName) ||
          ContainsCallToInArithmExpr(addSub.arithmExpr(1), functionName),
       CompilerParser.MulDivExprContext mulDiv =>
    ContainsCallToInArithmExpr(mulDiv.arithmExpr(0), functionName) ||
             ContainsCallToInArithmExpr(mulDiv.arithmExpr(1), functionName),
       CompilerParser.AtomExprContext atomExpr => ContainsCallToInAtom(atomExpr.atom(), functionName),
                _ => false
  };
     }

        /// <summary>
    /// Checks if an atom contains a call to the specified function.
        /// </summary>
        private bool ContainsCallToInAtom(CompilerParser.AtomContext context, string functionName)
        {
            return context switch
    {
     CompilerParser.FuncCallAtomContext funcCall =>
      funcCall.functionCall().ID()?.GetText() == functionName,
         CompilerParser.ParenExprContext parenExpr =>
              ContainsCallToInArithmExpr(parenExpr.arithmExpr(), functionName),
         _ => false
     };
        }

   /// <summary>
        /// Writes all function information to a file.
   /// </summary>
      public static void WriteToFile(List<FunctionInfo> functions, string filePath)
        {
       var sb = new StringBuilder();
            sb.AppendLine("=".PadRight(80, '='));
    sb.AppendLine("FUNCTION ANALYSIS REPORT");
            sb.AppendLine("=".PadRight(80, '='));
    sb.AppendLine();

   foreach (var func in functions)
            {
                sb.AppendLine("-".PadRight(60, '-'));
     sb.AppendLine($"FUNCTION: {func.Name}");
  sb.AppendLine("-".PadRight(60, '-'));

     // Function type (iterative/recursive, main/non-main)
       var functionType = func.IsRecursive ? "Recursive" : "Iterative";
        var mainType = func.IsMain ? "Main" : "Non-Main";
             sb.AppendLine($"  Function Type: {functionType}, {mainType}");

        // Return type
                sb.AppendLine($"  Return Type: {func.ReturnType}");

                // Location
     sb.AppendLine($"  Location: Lines {func.StartLine} - {func.EndLine}");

      // Parameters
                sb.AppendLine($"  Parameters ({func.Parameters.Count}):");
    if (func.Parameters.Count == 0)
    {
         sb.AppendLine("    (none)");
    }
else
           {
         foreach (var param in func.Parameters)
         {
               sb.AppendLine($"    - {param}");
             }
             }

     // Local variables
                sb.AppendLine($"  Local Variables ({func.LocalVariables.Count}):");
           if (func.LocalVariables.Count == 0)
     {
          sb.AppendLine("(none)");
          }
      else
       {
       foreach (var localVar in func.LocalVariables)
          {
    sb.AppendLine($"    - {localVar} (line {localVar.LineNumber})");
       }
  }

             // Control structures
         sb.AppendLine($"  Control Structures ({func.ControlStructures.Count}):");
            if (func.ControlStructures.Count == 0)
          {
             sb.AppendLine("    (none)");
       }
      else
  {
        foreach (var ctrl in func.ControlStructures)
      {
             sb.AppendLine($"    - {ctrl}");
              }
                }

      sb.AppendLine();
       }

       sb.AppendLine("=".PadRight(80, '='));
   sb.AppendLine($"Total Functions: {functions.Count}");
       sb.AppendLine($"Main Functions: {functions.Count(f => f.IsMain)}");
            sb.AppendLine($"Recursive Functions: {functions.Count(f => f.IsRecursive)}");
sb.AppendLine("=".PadRight(80, '='));

   File.WriteAllText(filePath, sb.ToString());
        }
    }
}
