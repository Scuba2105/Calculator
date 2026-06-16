using System;
using System.Data;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Mathos.Parser;
using System.Text.RegularExpressions;

namespace Calculator.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private string _displayText = "0";

        private bool isCalculatedResult = false;

        private string lastAnswer = "0";

        public string DisplayText
        {
            get => _displayText;
            set
            {
                if (_displayText != value)
                {
                    _displayText = value;
                    OnPropertyChanged();
                }
            }
        }

        // Triggered when a number button is pressed
        public void NumberCommand(string number)
        {
            // 1. Reset state if the screen shows a lone '0' or a finalized calculation result
            if (DisplayText == "0" || isCalculatedResult) 
            {
                DisplayText = number;
                isCalculatedResult = false;
                return;
            }

            // 2. Look at the last character on the screen
            char lastChar = DisplayText[DisplayText.Length - 1];

            // 3. Handle implicit multiplication triggers
            // If the last character is a closing parenthesis, inject an asterisk
            if (lastChar == ')')
            {
                DisplayText += $" * {number}";
            }
            // If the last character is a lone space (e.g. from an operator like "+ "), just attach the number
            else
            {
                DisplayText += number;
            }
        }

        // Triggered when an operator button is pressed
        public void OperatorCommand(string op)
        {
            // 1. Handle an empty screen or initial state
            if (string.IsNullOrEmpty(DisplayText) || DisplayText == "0")
            {
                if (op == "-")
                {
                    DisplayText = "-";
                    isCalculatedResult = false;
                }
                // Don't allow other operators or closing brackets at the start
                return; 
            }

            // 2. Handle the Closing Parenthesis Case uniquely
            if (op == ")")
            {
                // Safely count all opening brackets (both raw '(' and inside functions like 'sqrt(')
                int openCount = DisplayText.Split('(').Length - 1;
                int closeCount = DisplayText.Split(')').Length - 1;

                if (openCount > closeCount)
                {
                    char lastChar = DisplayText[DisplayText.Length - 1];
                    
                    // If it follows a space (like an operator "5 + "), don't allow a closing bracket yet
                    if (lastChar == ' ') return;

                    // Append tight without an extra leading space if closing a number or variable
                    DisplayText += ")";
                }
                return;
            }

            // 3. Prevent duplicate math operators or operators right after a function bracket
            if (DisplayText.EndsWith(" + ") || DisplayText.EndsWith(" - ") || DisplayText.EndsWith(" * ") || DisplayText.EndsWith(" / "))
            {
                if (op == "-")
                {
                    // Allow a negative symbol directly after a standard operator
                    DisplayText += "-";
                }
                else
                {
                    // Replace the last operator with the new one cleanly
                    DisplayText = DisplayText.Substring(0, DisplayText.Length - 3) + $" {op} ";
                }
            }
            // Prevent standard operators from being typed immediately after an open parenthesis (e.g., "sqrt( + ")
            else if (DisplayText.EndsWith("(") || DisplayText.EndsWith("(e, "))
            {
                if (op == "-")
                {
                    // Only allow the minus symbol for negative numbers inside a bracket
                    DisplayText += "-";
                }
                // Blocks +, *, / from breaking the function open token
            }
            // 4. Default Case: Append operator with clean spacing
            else
            {
                DisplayText += $" {op} ";
            }

            isCalculatedResult = false;
        }
        
        // This is the method bound directly to your RelayCommand / ReactiveCommand
        public void ImplicitMultCommand(string rawParameter)
        {
            if (string.IsNullOrEmpty(rawParameter)) return;

            // Split the "function,isExponential" string into an array
            string[] parts = rawParameter.Split(',');
            
            string functionName = parts[0];
            
            // Safely parse the boolean flag, default to false if parsing fails
            bool isExponential = parts.Length > 1 && bool.TryParse(parts[1], out bool result) && result;

            // Call your core layout function using the extracted values
            ExecuteImplicitMultLogic(functionName, isExponential);
        }

        // Used for implicit multiplication with functions like sqrt, pow, etc. Also handles parentheses intelligently.
        private void ExecuteImplicitMultLogic(string function, bool isExponential)
        {
            // 1. Defend against empty strings
            if (string.IsNullOrEmpty(DisplayText) || DisplayText == "0" || isCalculatedResult)
            {
                if (function == "(")
                    DisplayText = "(";
                else
                    DisplayText = isExponential ? $"{function}(e, " : $"{function}(";

                isCalculatedResult = false;
                return;
            }

            // 2. CASE: Custom Base Power button (x^n) -> e.g. "pow(5, "
            if (function == "pow" && !isExponential)
            {
                // Find the number or "Ans" sitting at the very end of the screen text
                var match = System.Text.RegularExpressions.Regex.Match(DisplayText, @"(\d+(\.\d+)?|Ans|s)$");
                
                if (match.Success)
                {
                    string baseValue = match.Value;
                    // Trim the base number away from the main display string
                    DisplayText = DisplayText.Substring(0, DisplayText.Length - baseValue.Length);
                    
                    // Re-append it natively formatted as the base of your pow function
                    DisplayText += $"pow({baseValue}, ";
                    isCalculatedResult = false;
                    return;
                }
            }

            // 3. CASE: Everything Else (sqrt, log, ln, (, and e^n)
            char lastChar = DisplayText[DisplayText.Length - 1];
            char secondLastChar = DisplayText.Length > 1 ? DisplayText[DisplayText.Length - 2] : ' ';

            // Build the string snippet
            string functionSnippet;
            if (function == "(")
                functionSnippet = "(";
            else
                functionSnippet = isExponential ? $"{function}(e, " : $"{function}(";

            // Check for implicit multiplication triggers
            if (char.IsDigit(lastChar) || lastChar == ')' || lastChar == 's')
            {
                DisplayText += function == "(" ? " * (" : $" * {functionSnippet}";
            }
            // Handle standard spaces to prevent double operators (+ pow() )
            else if (lastChar == ' ' && (secondLastChar == '+' || secondLastChar == '-' || secondLastChar == '*' || secondLastChar == '/'))
            {
                DisplayText += functionSnippet;
            }
            else if (lastChar == ' ')
            {
                DisplayText += function == "(" ? "* (" : $"* {functionSnippet}";
            }
            else
            {
                DisplayText += function == "(" ? "(" : $" {functionSnippet}";
            }

            isCalculatedResult = false;            
        }

        // Triggered when the decimal point button is pressed
        public void DecimalCommand(string decimalPoint)
        {
            // 1. Reset state if fresh or displaying a finalized calculation
            if (string.IsNullOrEmpty(DisplayText) || DisplayText == "0" || isCalculatedResult)
            {
                DisplayText = "0.";
                isCalculatedResult = false;
                return;
            }

            char lastChar = DisplayText[DisplayText.Length - 1];

            // 2. Prevent adding a decimal after a closing bracket (e.g., "sqrt(9).")
            if (lastChar == ')' || lastChar == 's') // 's' handles trailing text variables if any
            {
                return; 
            }

            // 3. If it follows an operator, a space, or an opening bracket, prepend a '0' (e.g., "+ ." -> "+ 0.")
            if (lastChar == ' ' || lastChar == '(')
            {
                DisplayText += "0.";
                return;
            }

            // 4. Scan backward through the current string to isolate the last active number
            bool currentNumberHasDecimal = false;
            for (int i = DisplayText.Length - 1; i >= 0; i--)
            {
                char c = DisplayText[i];
                
                // If we hit a space or an opening bracket, we've moved past our current number
                if (c == ' ' || c == '(') 
                    break;

                if (c == '.')
                {
                    currentNumberHasDecimal = true;
                    break;
                }
            }

            // 5. Only append the decimal point if the current number doesn't have one yet
            if (!currentNumberHasDecimal)
            {
                DisplayText += decimalPoint;
            }
        }

        // Triggered when 'Ans' is pressed
        public void AnswerCommand()
        {
            // If the screen is empty, on a fresh zero state, or showing a previous result
            if (string.IsNullOrEmpty(DisplayText) || DisplayText == "0" || isCalculatedResult)
            {
                DisplayText = lastAnswer.ToString();
                isCalculatedResult = false;
                return;
            }

            char lastChar = DisplayText[DisplayText.Length - 1];
            char secondLastChar = DisplayText.Length > 1 ? DisplayText[DisplayText.Length - 2] : ' ';

            // 1. Identify implicit-multiplication triggers
            // If the text ends in a digit, a decimal point, or a closing parenthesis, inject an asterisk
            if (char.IsDigit(lastChar) || lastChar == '.' || lastChar == ')')
            {
                DisplayText += $" * {lastAnswer}";
            }
            // 2. Handle standard spaces to prevent double operators (e.g., "5 + " -> "5 + 20")
            else if (lastChar == ' ' && (secondLastChar == '+' || secondLastChar == '-' || secondLastChar == '*' || secondLastChar == '/'))
            {
                DisplayText += lastAnswer.ToString();
            }
            else if (lastChar == ' ')
            {
                DisplayText += $"* {lastAnswer}";
            }
            // 3. Default fallback case
            else
            {
                DisplayText += $" {lastAnswer}";
            }

            isCalculatedResult = false;
        }

        // Triggered when '!' is pressed
        public void FactorialCommand()
        {
            if (string.IsNullOrEmpty(DisplayText) || DisplayText == "0" || isCalculatedResult)
            {
                return; // Can't start an expression with a factorial symbol
            }

            char lastChar = DisplayText[DisplayText.Length - 1];

            // Only allow factorial immediately after a digit, a closing bracket, or 'Ans'
            if (char.IsDigit(lastChar) || lastChar == ')' || lastChar == 's')
            {
                // Append tight against the character (e.g., "5!")
                DisplayText += "!";
                isCalculatedResult = false;
            }
        }

        // Triggered when 'C' is pressed
        public void ClearCommand()
        {
            DisplayText = "0";
            lastAnswer = "0";
        }

        public void BackspaceCommand()
        {
            if (DisplayText.Length > 1)
                DisplayText = DisplayText.Substring(0, DisplayText.Length - 1);
            else
                DisplayText = "0";
        }

        // Triggered when '=' is pressed
        public void EqualsCommand()
        {
            // Don't calculate if the expression ends with an incomplete operator
            if (DisplayText.EndsWith(" ") || DisplayText == "0") 
                return;

            try
            {
                // Safely handle division by zero before evaluating
                if (DisplayText.Contains("/ 0"))
                {
                    DisplayText = "Error: Div by 0";
                    return;
                }

               EvaluateExpression(DisplayText);
            }
            catch (Exception)
            {
                DisplayText = "Error";
            }
        }

        // 2. THIS IS THE CALCULATOR ENGINE HELPER METHOD
        public void EvaluateExpression(string expression)
        {
            if (string.IsNullOrEmpty(expression)) return;

            string expressionToParse = expression;

            try
            {
                // 1. Automatically balance unclosed brackets
                int openCount = expressionToParse.Split('(').Length - 1;
                int closeCount = expressionToParse.Split(')').Length - 1;
                while (openCount > closeCount)
                {
                    expressionToParse += ")";
                    closeCount++;
                }

                // 2. Convert Factorial symbols 'x!' into 'fact(x)' using Regex
                expressionToParse = Regex.Replace(expressionToParse, @"(\d+(\.\d+)?|\([^)]+\))\s*!", "fact($1)");

                // 3. Initialize Mathos Parser
                var parser = new MathParser();

                // Convert your string lastAnswer to double safely for the parser
                double ansValue = double.TryParse(lastAnswer, out double parsedAns) ? parsedAns : 0;
                parser.LocalVariables.Add("Ans", ansValue);
                
                // 5. FIX: Use 'LocalFunctions' and read inputs[0] 
                               
                // Add custom Factorial math rule
                parser.LocalFunctions.Add("fact", inputs => CalculateFactorial(inputs[0]));

                // 6. Parse the final expression
                double result = parser.Parse(expressionToParse);

                // 7. Update display state
                lastAnswer = result.ToString();
                DisplayText = lastAnswer;
                isCalculatedResult = true;
            }
            catch (Exception ex)
            {
                DisplayText = ex.Message;
                isCalculatedResult = true;
            }
        }

        // Helper method to compute factorials safely
        private double CalculateFactorial(double value)
        {
            // Factorials are only defined for non-negative integers
            if (value < 0 || value % 1 != 0) 
                throw new ArgumentException("Factorial must be a non-negative integer.");

            if (value == 0 || value == 1) return 1;

            double result = 1;
            for (int i = 2; i <= (int)value; i++)
            {
                result *= i;
            }
            return result;
        }

        // Standard boilerplate code to tell Avalonia to update the UI text
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
