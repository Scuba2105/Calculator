using System;
using System.Data;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Calculator.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private string _displayText = "0";

        private bool isCalculatedResult = false;

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
            if (DisplayText == "0" || isCalculatedResult) {
                DisplayText = number;
                isCalculatedResult = false;
        }
            else
                DisplayText += number;
        }

        // Triggered when an operator button is pressed
        public void OperatorCommand(string op)
        {
            DisplayText += $" {op} ";
            isCalculatedResult = false;
        }

        // Triggered when 'C' is pressed
        public void ClearCommand()
        {
            DisplayText = "0";
        }

        // Triggered when '=' is pressed
        public void EqualCommand()
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

                string result = EvaluateExpression(DisplayText);
                DisplayText = result;
                isCalculatedResult = true;
            }
            catch (Exception)
            {
                DisplayText = "Error";
            }
        }

        // 2. THIS IS THE CALCULATOR ENGINE HELPER METHOD
        private string EvaluateExpression(string expression)
        {
            // DataTable.Compute is a built-in .NET tool that parses and solves string math
            using (var dt = new DataTable())
            {
                var result = dt.Compute(expression, "");
                
                // Convert object result to string, and handle decimal formatting
                double doubleResult = Convert.ToDouble(result);
                return doubleResult.ToString();
            }
        }

        // Standard boilerplate code to tell Avalonia to update the UI text
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
