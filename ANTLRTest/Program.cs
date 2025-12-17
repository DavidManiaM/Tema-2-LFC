using Antlr4.Runtime;
using System;
using System.IO;
using System.Text;

namespace ANTLRTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var fileName = "../../../input.vex";
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: Input file not found at '{filePath}'");
                return;
            }

            // 1. Read the file content
            var fileContent = File.ReadAllText(filePath);

            // 2. Create the input stream
            var inputStream = new AntlrInputStream(fileContent);

            // 3. Create the lexer
            var lexer = new ANTLRTest.CompilerLexer(inputStream);

            // 4. Create the token stream
            var commonTokenStream = new CommonTokenStream(lexer);

            // This is where we'll extract and save the tokens
            SaveTokensToFile(commonTokenStream, "../../../tokens.txt");

            // Reset the stream to be used by the parser
            commonTokenStream.Seek(0);


            // 5. Create the parser
            var parser = new CompilerParser(commonTokenStream);

            // 6. Start parsing from the 'program' rule (the root rule in your grammar)
            var context = parser.program();

            // 7. Visit the parse tree with EvalVisitor
            var visitor = new EvalVisitor();
            visitor.Evaluate(context);

            // Save global variables to a file
            SaveGlobalVariablesToFile(visitor, "../../../global_variables.txt");

            Console.WriteLine("Parsing completed.");
            // Console.WriteLine(context.ToStringTree(parser)); // Print the LISP-style tree

            Console.WriteLine("\nParse Tree:");
            Console.WriteLine(PrettyPrintTree(context.ToStringTree(parser)));


            // 8. Print evaluated variables
            Console.WriteLine("\nVariables:");
            foreach (var kvp in visitor.Variables)
            {
                Console.WriteLine($"  {kvp.Key} = {kvp.Value.Value} (Type: {kvp.Value.Type}, Scope: {kvp.Value.Scope})");
            }
        }

        /// <summary>
        /// Writes all global variables, their types, and initial values to a file.
        /// </summary>
        private static void SaveGlobalVariablesToFile(EvalVisitor visitor, string outputPath)
        {
            var globalVars = visitor.Variables
                .Where(kvp => kvp.Value.Scope == "global")
                .Select(kvp => $"Variable: {kvp.Key}, Type: {kvp.Value.Type}, Initial Value: {kvp.Value.Value}");

            File.WriteAllLines(outputPath, globalVars);
            Console.WriteLine($"Global variables saved to '{outputPath}'");
        }

        /// <summary>
        /// Extracts all tokens from the stream and saves them to a file.
        /// </summary>
        private static void SaveTokensToFile(CommonTokenStream tokens, string outputPath)
        {
            // The Fill() method is called to load all tokens from the lexer
            tokens.Fill();

            var tokenTuples = tokens.GetTokens().Select(t =>
            {
                // For EOF, the symbolic name can be null, so we handle that case.
                string tokenName = CompilerLexer.DefaultVocabulary.GetSymbolicName(t.Type) ?? "EOF";
                string lexem = t.Text.Replace("\r", "\\r").Replace("\n", "\\n"); // Escape newlines
                int lineIndex = t.Line;
                return $"<{tokenName}, {lexem}, {lineIndex}>";
            });

            File.WriteAllLines(outputPath, tokenTuples);
            Console.WriteLine($"Tokens saved to '{outputPath}'");
        }

        /// <summary>
        /// Formats a LISP-style tree string with proper indentation.
        /// </summary>
        private static string PrettyPrintTree(string tree)
        {
            var sb = new StringBuilder();
            int indentLevel = 0;
            const string indent = "  ";

            for (int i = 0; i < tree.Length; i++)
            {
                char c = tree[i];

                if (c == '(')
                {
                    if (i > 0 && tree[i - 1] != '(' && tree[i - 1] != ' ')
                    {
                        sb.AppendLine();
                        sb.Append(string.Concat(Enumerable.Repeat(indent, indentLevel)));
                    }
                    sb.Append(c);
                    indentLevel++;
                    sb.AppendLine();
                    sb.Append(string.Concat(Enumerable.Repeat(indent, indentLevel)));
                }
                else if (c == ')')
                {
                    indentLevel--;
                    sb.AppendLine();
                    sb.Append(string.Concat(Enumerable.Repeat(indent, indentLevel)));
                    sb.Append(c);
                }
                else if (c == ' ' && i + 1 < tree.Length && tree[i + 1] == '(')
                {
                    // Skip space before opening paren
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }

    public class Variable
    {
        public string Type { get; }
        public object Value { get; set; }
        public string Scope { get; }

        public Variable(string type, object value, string scope)
        {
            Type = type;
            Value = value;
            Scope = scope;
        }

        public override string ToString()
        {
            return $"Type: {Type}, Value: {Value}, Scope: {Scope}";
        }
    }
}