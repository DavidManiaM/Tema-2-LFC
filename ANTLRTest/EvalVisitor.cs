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
        private readonly Dictionary<string, object> _variables = new(StringComparer.Ordinal);

        /// <summary>
        /// Entry point to evaluate a parsed program.
        /// </summary>
        public object? Evaluate(CompilerParser.ProgramContext context)
        {
            return VisitProgram(context);
        }

        /// <summary>
        /// Visit the root program rule: (varDeclaration SEMICOL)* mainFunction EOF
        /// </summary>
        public object? VisitProgram(CompilerParser.ProgramContext context)
        {
            object? result = null;

            // Visit global variable declarations
            foreach (var varDecl in context.varDeclaration())
            {
                VisitVarDeclaration(varDecl);
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
        /// Visit a statement.
        /// </summary>
        public object? VisitStatement(CompilerParser.StatementContext context)
        {
            if (context.varDeclaration() != null)
                return VisitVarDeclaration(context.varDeclaration());

            if (context.varAssignment() != null)
                return VisitVarAssignment(context.varAssignment());

            if (context.arithmExpr() != null)
                return EvaluateArithmExpr(context.arithmExpr());

            if (context.ifStatement() != null)
                return VisitIfStatement(context.ifStatement());

            if (context.forStatement() != null)
                return VisitForStatement(context.forStatement());

            if (context.whileStatement() != null)
                return VisitWhileStatement(context.whileStatement());

            if (context.returnStatement() != null)
                return VisitReturnStatement(context.returnStatement());

            if (context.functionCall() != null)
                return VisitFunctionCall(context.functionCall());

            return null;
        }

        /// <summary>
        /// Visit a variable declaration (e.g., "int a = 5;").
        /// </summary>
        public object? VisitVarDeclaration(CompilerParser.VarDeclarationContext context)
        {
            var idNodes = context.ID();
            var exprContexts = context.arithmExpr();
            var stringContexts = context.@string();

            object? lastValue = null;
            int exprIndex = 0;
            int stringIndex = 0;

            for (int i = 0; i < idNodes.Length; i++)
            {
                var varName = idNodes[i].GetText();
                object value;

                // Determine if this variable has an arithmetic or string initializer
                if (exprContexts != null && exprIndex < exprContexts.Length)
                {
                    value = EvaluateArithmExpr(exprContexts[exprIndex]);
                    exprIndex++;
                }
                else if (stringContexts != null && stringIndex < stringContexts.Length)
                {
                    value = stringContexts[stringIndex].GetText().Trim('"');
                    stringIndex++;
                }
                else
                {
                    value = 0.0;
                }

                _variables[varName] = value;
                lastValue = value;
            }
            return lastValue;
        }

        /// <summary>
        /// Visit a variable assignment with compound operators support.
        /// </summary>
        public object? VisitVarAssignment(CompilerParser.VarAssignmentContext context)
        {
            var varName = context.ID().GetText();

            // Handle increment: a++
            if (context.INCR() != null)
            {
                var current = GetVariableAsDouble(varName);
                _variables[varName] = current + 1;
                return current + 1;
            }

            // Handle decrement: a--
            if (context.DECR() != null)
            {
                var current = GetVariableAsDouble(varName);
                _variables[varName] = current - 1;
                return current - 1;
            }

            var varValueContext = context.varValue();
            if (varValueContext == null) return null;

            // Handle compound assignments: +=, -=, *=, /=, %=
            if (context.PLUS_EQ() != null)
            {
                var current = GetVariableAsDouble(varName);
                var value = EvaluateVarValue(varValueContext);
                _variables[varName] = current + value;
                return current + value;
            }

            if (context.MINUS_EQ() != null)
            {
                var current = GetVariableAsDouble(varName);
                var value = EvaluateVarValue(varValueContext);
                _variables[varName] = current - value;
                return current - value;
            }

            if (context.MUL_EQ() != null)
            {
                var current = GetVariableAsDouble(varName);
                var value = EvaluateVarValue(varValueContext);
                _variables[varName] = current * value;
                return current * value;
            }

            if (context.DIV_EQ() != null)
            {
                var current = GetVariableAsDouble(varName);
                var value = EvaluateVarValue(varValueContext);
                if (value == 0) throw new DivideByZeroException();
                _variables[varName] = current / value;
                return current / value;
            }

            if (context.MOD_EQ() != null)
            {
                var current = GetVariableAsDouble(varName);
                var value = EvaluateVarValue(varValueContext);
                _variables[varName] = current % value;
                return current % value;
            }

            // Handle simple assignment: a = value
            if (context.ASSIGN() != null)
            {
                if (varValueContext.arithmExpr() != null)
                {
                    var value = EvaluateArithmExpr(varValueContext.arithmExpr());
                    _variables[varName] = value;
                    return value;
                }
                if (varValueContext.@string() != null)
                {
                    var value = varValueContext.@string().GetText().Trim('"');
                    _variables[varName] = value;
                    return value;
                }
            }

            return null;
        }

        private double EvaluateVarValue(CompilerParser.VarValueContext context)
        {
            if (context.arithmExpr() != null)
                return EvaluateArithmExpr(context.arithmExpr());
            return 0;
        }

        /// <summary>
        /// Visit if statement.
        /// </summary>
        public object? VisitIfStatement(CompilerParser.IfStatementContext context)
        {
            var condition = EvaluateLogicalExpr(context.logicalExpr());
            var statements = context.statement();
            var lbraces = context.LBRACE();
            var rbraces = context.RBRACE();

            object? result = null;

            if (context.ELSE_TOKEN() != null)
            {
                // There's an else block - need to split statements between if and else
                // Find the index where the else block starts by comparing statement positions
                // The first RBRACE marks the end of the if-block
                int firstRbraceIndex = rbraces[0].Symbol.StopIndex;

                var ifStatements = new List<CompilerParser.StatementContext>();
                var elseStatements = new List<CompilerParser.StatementContext>();

                foreach (var stmt in statements)
                {
                    // Statements before the first RBRACE belong to the if-block
                    if (stmt.Start.StartIndex < firstRbraceIndex)
                    {
                        ifStatements.Add(stmt);
                    }
                    else
                    {
                        elseStatements.Add(stmt);
                    }
                }

                if (condition)
                {
                    foreach (var stmt in ifStatements)
                    {
                        result = VisitStatement(stmt);
                    }
                }
                else
                {
                    foreach (var stmt in elseStatements)
                    {
                        result = VisitStatement(stmt);
                    }
                }
            }
            else
            {
                // No else block - execute all statements if condition is true
                if (condition)
                {
                    foreach (var stmt in statements)
                    {
                        result = VisitStatement(stmt);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Visit for statement.
        /// </summary>
        public object? VisitForStatement(CompilerParser.ForStatementContext context)
        {
            // Initialize
            if (context.varDeclaration() != null)
                VisitVarDeclaration(context.varDeclaration());

            object? result = null;

            // Loop while condition is true (or forever if no condition)
            while (context.logicalExpr() == null || EvaluateLogicalExpr(context.logicalExpr()))
            {
                // Execute body
                foreach (var stmt in context.statement())
                {
                    result = VisitStatement(stmt);
                }

                // Update
                if (context.varAssignment() != null)
                    VisitVarAssignment(context.varAssignment());
            }

            return result;
        }

        /// <summary>
        /// Visit while statement.
        /// </summary>
        public object? VisitWhileStatement(CompilerParser.WhileStatementContext context)
        {
            object? result = null;

            while (context.logicalExpr() == null || EvaluateLogicalExpr(context.logicalExpr()))
            {
                foreach (var stmt in context.statement())
                {
                    result = VisitStatement(stmt);
                }
            }

            return result;
        }

        /// <summary>
        /// Visit return statement.
        /// </summary>
        public object? VisitReturnStatement(CompilerParser.ReturnStatementContext context)
        {
            if (context.expression() != null)
            {
                var expr = context.expression();
                if (expr.arithmExpr() != null)
                    return EvaluateArithmExpr(expr.arithmExpr());
                if (expr.@string() != null)
                    return expr.@string().GetText().Trim('"');
            }
            return null;
        }

        /// <summary>
        /// Visit function call (including built-in functions).
        /// </summary>
        public object? VisitFunctionCall(CompilerParser.FunctionCallContext context)
        {
            var funcName = context.BUILTIN_FUNC()?.GetText() ?? context.ID()?.GetText();
            var args = context.expression();

            return funcName switch
            {
                "sqrt" => Math.Sqrt(EvaluateExpression(args[0])),
                "log" => Math.Log(EvaluateExpression(args[0])),
                "sin" => Math.Sin(EvaluateExpression(args[0])),
                "cos" => Math.Cos(EvaluateExpression(args[0])),
                _ => throw new InvalidOperationException($"Unknown function: {funcName}")
            };
        }

        private double EvaluateExpression(CompilerParser.ExpressionContext context)
        {
            if (context.arithmExpr() != null)
                return EvaluateArithmExpr(context.arithmExpr());
            return 0;
        }

        /// <summary>
        /// Evaluate a logical expression.
        /// </summary>
        private bool EvaluateLogicalExpr(CompilerParser.LogicalExprContext context)
        {
            // singleExprs is a single context, not an array
            var firstExpr = context.singleLogicalExpr();
            var binaryOps = context.BINARY_LOGICAL_OP();
            var logicalExprs = context.logicalExpr();

            bool result = EvaluateSingleLogicalExpr(firstExpr);

            // If there are binary logical operators, process them with the right-hand logical expressions
            for (int i = 0; i < binaryOps.Length; i++)
            {
                var op = binaryOps[i].GetText();
                // logicalExprs[i] is the right-hand side for each binary op (0-indexed)
                if (i < logicalExprs.Length)
                {
                    var right = EvaluateLogicalExpr(logicalExprs[i]);

                    result = op switch
                    {
                        "&&" => result && right,
                        "||" => result || right,
                        _ => throw new InvalidOperationException($"Unknown logical operator: {op}")
                    };
                }
            }

            return result;
        }

        private bool EvaluateSingleLogicalExpr(CompilerParser.SingleLogicalExprContext context)
        {
            // Handle TRUE/FALSE/NULL
            if (context.TRUE() != null) return true;
            if (context.FALSE() != null) return false;
            if (context.NULL() != null) return false;

            // Handle unary NOT
            if (context.UNARY_LOGICAL_OP() != null)
                return !EvaluateSingleLogicalExpr(context.singleLogicalExpr());

            // Handle parenthesized expression
            if (context.logicalExpr() != null)
                return EvaluateLogicalExpr(context.logicalExpr());

            // Handle comparison: arithmExpr comparator arithmExpr
            var arithmExprs = context.arithmExpr();
            if (arithmExprs != null && arithmExprs.Length >= 2)
            {
                var left = EvaluateArithmExpr(arithmExprs[0]);
                var right = EvaluateArithmExpr(arithmExprs[1]);
                var comp = context.comparators().GetText();

                return comp switch
                {
                    "<" => left < right,
                    "<=" => left <= right,
                    ">" => left > right,
                    ">=" => left >= right,
                    "==" => Math.Abs(left - right) < double.Epsilon,
                    "!=" => Math.Abs(left - right) >= double.Epsilon,
                    _ => throw new InvalidOperationException($"Unknown comparator: {comp}")
                };
            }

            // Handle single arithmetic expression (truthy check)
            if (arithmExprs != null && arithmExprs.Length == 1)
                return EvaluateArithmExpr(arithmExprs[0]) != 0;

            return false;
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

        private double EvaluateAtom(CompilerParser.AtomContext context)
        {
            return context switch
            {
                CompilerParser.NumberAtomContext numCtx => double.Parse(numCtx.number().GetText()),
                CompilerParser.IdAtomContext idCtx => GetVariableAsDouble(idCtx.ID().GetText()),
                CompilerParser.ParenExprContext parenCtx => EvaluateArithmExpr(parenCtx.arithmExpr()),
                CompilerParser.FuncCallAtomContext funcCtx => (double)(VisitFunctionCall(funcCtx.functionCall()) ?? 0.0),
                _ => throw new InvalidOperationException($"Unknown atom type: {context.GetType().Name}")
            };
        }

        private double GetVariableAsDouble(string name)
        {
            if (_variables.TryGetValue(name, out var value))
            {
                return value switch
                {
                    double d => d,
                    int i => i,
                    string s => double.TryParse(s, out var parsed) ? parsed : 0,
                    _ => 0
                };
            }
            throw new InvalidOperationException($"Undefined variable: {name}");
        }

        // Public accessors
        public bool TryGetVariable(string name, out object? value) => _variables.TryGetValue(name, out value);
        public void SetVariable(string name, object value) => _variables[name] = value;
        public IReadOnlyDictionary<string, object> Variables => _variables;
    }
}