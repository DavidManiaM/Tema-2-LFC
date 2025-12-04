using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;

namespace ANTLRTest
{
    /// <summary>
    /// Visitor that evaluates arithmetic expressions and variable assignments
    /// for the Compiler grammar.
    /// </summary>
    public class EvalVisitor : AbstractParseTreeVisitor<object?>, IParseTreeVisitor<object?>
    {
        // Symbol table to store variable values
        private readonly Dictionary<string, double> _variables = new(StringComparer.Ordinal);

        /// <summary>
        /// Entry point to evaluate a parsed program.
        /// </summary>
        public object? Evaluate(CompilerParser.ProgramContext context)
        {
            return VisitProgram(context);
        }

        /// <summary>
        /// Visit the root program rule: program* EOF
        /// </summary>
        public object? VisitProgram(CompilerParser.ProgramContext context)
        {
            object? result = null;

            // Visit top-level statements (if any)
            foreach (var statement in context.statement())
            {
                result = VisitStatement(statement);
            }

            // Visit statements inside main function
            var mainFunc = context.mainFunction();
            if (mainFunc != null)
            {
                foreach (var statement in mainFunc.statement())
                {
                    result = VisitStatement(statement);
                }
            }

            return result;
        }

        /// <summary>
        /// Visit a statement (declaration, assignment, or expression).
        /// </summary>
        public object? VisitStatement(CompilerParser.StatementContext context)
        {
            // Handle variable declaration: e.g., "int a = 5;"
            if (context.varDeclaration() != null)
            {
                return VisitVarDeclaration(context.varDeclaration());
            }

            // Handle variable assignment: e.g., "a = 5;"
            if (context.varAssignment() != null)
            {
                return VisitVarAssignment(context.varAssignment());
            }

            // Handle standalone expression: arithmExpr SEMICOL
            if (context.arithmExpr() != null)
            {
                return EvaluateArithmExpr(context.arithmExpr());
            }

            return null;
        }

        /// <summary>
        /// Visit a variable declaration (e.g., "int a = 5;").
        /// </summary>
        public object? VisitVarDeclaration(CompilerParser.VarDeclarationContext context)
        {
            // Fix: context.ID() returns ITerminalNode[], not a single node
            var idNodes = context.ID();
            var exprContexts = context.arithmExpr();

            // Support multiple declarations: int a = 5, b = 6;
            object? lastValue = null;
            for (int i = 0; i < idNodes.Length; i++)
            {
                var varName = idNodes[i].GetText();
                double value = 0.0;
                if (exprContexts != null && i < exprContexts.Length && exprContexts[i] != null)
                {
                    value = EvaluateArithmExpr(exprContexts[i]);
                }
                _variables[varName] = value;
                lastValue = value;
            }
            return lastValue;
        }

        /// <summary>
        /// Visit a variable assignment (e.g., "a = 5;").
        /// </summary>
        public object? VisitVarAssignment(CompilerParser.VarAssignmentContext context)
        {
            var varName = context.ID().GetText();
            // Fix: Use varValue().arithmExpr() instead of context.arithmExpr()
            var varValueContext = context.varValue();
            if (varValueContext != null && varValueContext.arithmExpr() != null)
            {
                var value = EvaluateArithmExpr(varValueContext.arithmExpr());
                _variables[varName] = value;
                return value;
            }
            return null;
        }

        /// <summary>
        /// Evaluate an arithmetic expression and return its numeric value.
        /// </summary>
        private double EvaluateArithmExpr(CompilerParser.ArithmExprContext context)
        {
            return context switch
            {
                CompilerParser.AddSubExprContext addSub => EvaluateAddSub(addSub),
                CompilerParser.MulDivExprContext mulDiv => EvaluateMulDiv(mulDiv),
                CompilerParser.AtomExprContext atomExpr => EvaluateAtom(atomExpr.atom()),
                _ => throw new InvalidOperationException($"Unknown expression type: {context.GetType().Name}")
            };
        }

        /// <summary>
        /// Evaluate addition/subtraction expressions.
        /// </summary>
        private double EvaluateAddSub(CompilerParser.AddSubExprContext context)
        {
            var left = EvaluateArithmExpr(context.arithmExpr(0));
            var right = EvaluateArithmExpr(context.arithmExpr(1));
            var op = context.op.Text;

            return op switch
            {
                "+" => left + right,
                "-" => left - right,
                _ => throw new InvalidOperationException($"Unknown operator: {op}")
            };
        }

        /// <summary>
        /// Evaluate multiplication/division expressions.
        /// </summary>
        private double EvaluateMulDiv(CompilerParser.MulDivExprContext context)
        {
            var left = EvaluateArithmExpr(context.arithmExpr(0));
            var right = EvaluateArithmExpr(context.arithmExpr(1));
            var op = context.op.Text;

            return op switch
            {
                "*" => left * right,
                "/" => right != 0 ? left / right : throw new DivideByZeroException(),
                "%" => left % right,
                _ => throw new InvalidOperationException($"Unknown operator: {op}")
            };
        }

        /// <summary>
        /// Evaluate an atom (number, identifier, or parenthesized expression).
        /// </summary>
        private double EvaluateAtom(CompilerParser.AtomContext context)
        {
            return context switch
            {
                CompilerParser.NumberAtomContext numCtx => double.Parse(numCtx.number().GetText()),
                CompilerParser.IdAtomContext idCtx => GetVariable(idCtx.ID().GetText()),
                CompilerParser.ParenExprContext parenCtx => EvaluateArithmExpr(parenCtx.arithmExpr()),
                _ => throw new InvalidOperationException($"Unknown atom type: {context.GetType().Name}")
            };
        }

        /// <summary>
        /// Get a variable's value from the symbol table.
        /// </summary>
        private double GetVariable(string name)
        {
            if (_variables.TryGetValue(name, out var value))
            {
                return value;
            }
            throw new InvalidOperationException($"Undefined variable: {name}");
        }

        // Public accessors for the symbol table
        public bool TryGetVariable(string name, out double value) => _variables.TryGetValue(name, out value);
        public void SetVariable(string name, double value) => _variables[name] = value;
        public IReadOnlyDictionary<string, double> Variables => _variables;
    }
}