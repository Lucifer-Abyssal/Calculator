using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Windows;
using System.Windows.Controls;

namespace Project
{
    internal class AdvancedParser // Honestly, this took two days to figure out
    {
        int InvalidInputCheck(string text, out string message)
        {
            // Code 0: Err: For empty input
            // Code 1: Err: Invalid character
            // Code 2: Err: Amount of opening brackets doesn't match amount of closing brackets
            // Code 3: Err: Operators used incorrectly 
            // Code 4: Err: Invalid operator placements relative to parenthesis
            // Code 5: Err: Invalid use of decimal numbers
            // Code 6: Err: Trailing operand
            // Code 7: Err: Empty parenthesies
            // Code 8: Ok

            message = "Ok";

            // Code 0
            if (text == "") return 0;
            text = Regex.Replace(text, @"\)\(", ")*(");

            // Code 1
            if (Regex.Match(text, @"[^\d+*\-/^.()]") is Match Code1 && Code1.Success) { message = $"The input '{Code1}' is not allowed."; return 1; }

            // Code 2
            int counter = default;
            foreach (Match i in Regex.Matches(text, "[()]")) counter += 1;
            if (counter % 2 != 0) { message = $"The amount of opening brackets, does not match the amount of closing brackets."; return 2; }

            // Code 3
            else if (Regex.Match(text, @"([-*+/^.])\1+") is Match Code2 && Code2.Success)
            {
                if (Code2.Value.ElementAt(0) == '-') message = $"The input '{Code2.Value}' was too long. Maybe you forgot to incase a negative number in brackets?\nEx. 2-(-1)";
                else message = $"The input '{Code2.Value}' was too long.\nCorrect: '{Code2.Value.ElementAt(0)}'";
                return 3;
            }

            // Code 4
            else if (Regex.Match(text, @"\([\^*/.]|[\^*=./+-]\)") is Match Code3 && Code3.Success)
            {
                message = $"The input '{Code3.Value}' is a invalid use case of parenthesies.";
                return 4;
            }

            // Code 5
            else if (Regex.Match(text, @"(\d+\.(?!\d))|((?<!\d)\.\d+)|((?<!\d)\.(?!\d))") is Match Code4 && Code4.Success)
            {
                message = $"The input '{Code4.Value}' is invalid use case of decimal numbers.";
                return 5;
            }

            // Code 6
            else if (Regex.Match(text, @"[-+*^/]$|[-+*^/]\)") is Match Code5 && Code5.Success)
            {
                message = $"The input '{Code5.Value}' is invalid because it is not followed by an operator.";
                return 6;
            }

            // Code 7
            else if (Regex.Replace(text, @"[()]", "") == "")
            {
                message = $"Empty parenthesies";
                return 7;
            }

            // Code 8
            return 8;
        }
        List<string> ReturnBracketed(string text)
        {
            // before i knew of depth search first, i made it myself
            // it goes the down the bracket nest levels and adds to matches until it reaches a level without brackets
            // if so, it removes that level, and removes that level from its parent level, and continues.

            string _text = text;
            List<string> matches = new List<string>();
            Stack<string> stack = new Stack<string>();
            Stack<string> stackOld = new Stack<string>();

            matches.Add(_text);
            stack.Push(_text);
            stackOld.Push(_text);

            if (!Regex.Match(_text, @"\(").Success) return matches;

            Regex regex = new Regex(@"\(((?:[^()]|(?<c>\()|(?<-c>\)))*(?(c)(?!)))\)");

            while (!(stack.Count == 1 && !Regex.Match(stack.Peek(), @"\(").Success))
            {
                _text = stack.Peek();
                Match match = regex.Match(_text);
                if (match.Success)
                {
                    stack.Push(match.Groups[1].Value);
                    stackOld.Push(match.Groups[1].Value);
                    matches.Add(match.Groups[1].Value);
                }
                else
                {
                    Regex rgx = new Regex($@"\({Regex.Escape(stackOld.Pop())}\)");
                    stack.Pop();
                    stack.Push(rgx.Replace(stack.Pop(), "", 1));
                }
            }
            return matches;
        }
        dynamic ReturnStringListed(string text, out bool success)
        {
            List<dynamic> list = new List<dynamic>();
            success = true;

            while (true)
            {
                Match match = Regex.Match(text, @"\d+\.\d+|\d+|[-+/*^]");

                if (match.Success)
                {
                    switch (match.Value)
                    {
                        case "^":
                        case "*":
                        case "/":
                        case "+":
                        case "-":
                            list.Add(match.Value);
                            Regex regex = new Regex($@"\{match.Value}");
                            text = regex.Replace(text, "", 1);
                            break;
                        default:
                            try { list.Add(Convert.ToDouble(match.Value)); }
                            catch (OverflowException) { success = false; return new List<dynamic> { }; }
                            regex = new Regex(match.Value);
                            text = regex.Replace(text, "", 1);
                            break;
                    }
                }
                else break;
            }
            return ParseUnaryMinus(list);
        }
        dynamic ParseUnaryMinus(List<dynamic> list)
        {
            if (list.Count > 1) if (list[0] is string && list[0] == "-" && list[1] is double) { list[0] = list[1] * -1; list.RemoveAt(1); } // shity edge case detector 9000
                                                                                                                                            // wait it doesnt fucking work for '2-(-1)', I NEED TWO SHITTY EDGE CASE DETECTORS
            if (list.Count > 2) for (int i = 0; i < list.Count - 2; i++) if (list[i] is string && list[i + 1] is string s && s == "-" && list[i + 2] is double d) { list[i + 1] = d * -1; list.RemoveAt(i + 2); }
            return list;
        }
        double ReturnResult(dynamic list)
        {
            double result = default;

            if (list.Count == 2)                                // handle cases where a input of 2-(-(-1)) turned into ReturnResult receiving a list with "-", "-", 1
            {
                switch (list[0])
                {
                    case "+":
                        list.RemoveAt(0);
                        return result = list[0];
                    case "-":
                        list[0] = list[1] * -1;
                        list.RemoveAt(1);
                        return result = list[0];
                    default:
                        throw new ArgumentException();
                }
            }

            Predicate<dynamic> exponentation = item => item is string && item == "^";
            Predicate<dynamic> multiplicationDevision = item => item is string && (item == "*" || item == "/");
            Predicate<dynamic> additionSubtraction = item => item is string && (item == "+" || item == "-");

            while (list.Count != 1)
            {
                int index = list.FindIndex(exponentation);
                if (index == -1) index = list.FindIndex(multiplicationDevision);
                if (index == -1) index = list.FindIndex(additionSubtraction);

                string operand = list[index];
                double num1 = list[index - 1];
                double num2 = list[index + 1];

                switch (operand)
                {
                    case "^":
                        result = Math.Pow(num1, num2);
                        break;
                    case "*":
                        result = num1 * num2;
                        break;
                    case "/":
                        result = num1 / num2;
                        break;
                    case "+":
                        result = num1 + num2;
                        break;
                    case "-":
                        result = num1 - num2;
                        break;
                }

                list[index - 1] = result;
                list.RemoveAt(index);
                list.RemoveAt(index);
            }
            return list[0];
        }
        double ReturnAnswer(List<string> list, out bool success) // I absolutely forgot why this exists
        {
            int counter = default;
            Regex regex = new Regex(@"[()]");
            success = true;

            if (list.Count == 1 && !regex.Match(list[counter]).Success) return ReturnResult(ReturnStringListed(list[counter], out bool _));

            while (list.Count != 1)
            {
                string text = list[counter];
                Match match = regex.Match(text);
                if (!match.Success)
                {
                    double result = ReturnResult(ReturnStringListed(text, out bool _success));
                    if (!_success) { success = false; return 0; }                                                           // overflow check

                    Regex rgx = new Regex($@"\({Regex.Escape(text)}\)");
                    for (int i = counter - 1; i != -1; i--) list[i] = rgx.Replace(list[i], Convert.ToString(result), 1);
                    list.RemoveAt(counter);                                                                                 // DO NOT, remove this line, otherwise it will be indefinitely stuck
                    counter = 0;
                }
                else counter++;
            }
            return ReturnResult(ReturnStringListed(list[0], out bool _));
        }
        public double EvaluateEquasion(string text, out bool success, out string message)
        {
            success = true;
            message = "";

            text = Regex.Replace(text, " ", "");

            if (InvalidInputCheck(text, out string _message) is int errCode && errCode != 8)
            {
                success = false;
                message = _message;
                return errCode;
            }
            double result = ReturnAnswer(ReturnBracketed(text), out bool _success);
            if (!_success) { message = "You're equation evaluates to a number too big. Overflow.\n\n"; success = false; return 0; }
            return result;
        }
    }
    public partial class Calculator : Window
    {
        AdvancedParser advParse = new AdvancedParser();
        private List<string> HistoryList = new List<string>();
        private string History(string option, string input = "", double result = 0)
        {
            if (option == "Add") // This is..... But it works!
            {
                string historyToAdd = $"'{input}' = '{result}'";
                if (HistoryList.Count == 0) HistoryList.Add($"{HistoryList.Count + 1}. {historyToAdd}");
                else if (!(HistoryList.Last() == $"{HistoryList.Count}. {historyToAdd}"))
                {
                    HistoryList.Add($"{HistoryList.Count + 1}. {historyToAdd}");
                }
            }
            else if (option == "Return")
            {
                string returnString = "Current history:\n";
                foreach (string s in HistoryList) returnString += $"{s}\n";
                return returnString;
            }
            return "";
        }
        public Calculator()
        {
            InitializeComponent();
        }
        private void Button_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void Tab_Change(object sender, SelectionChangedEventArgs e)
        {
            TabControl tabControl = sender as TabControl;
            if (tabControl.SelectedIndex == 1) HistoryText.Text = History("Return");
        }
        private void InputBox_Change(object sender, TextChangedEventArgs e)
        {
            string input = InputBox.Text;
            bool success = true;
            string errorMessage = "";
            double result = advParse.EvaluateEquasion(input, out success, out errorMessage);
            if (success)
            {
                OutputText.Text = $"{input} = {result}";
                History("Add", input, result);
            }
            else OutputText.Text = $"{errorMessage}";
        }
    }
    // Todo:
    // 1. COMPLETE: Make a working history list.
    // 2. COMPLETE: Learn how to make a real UI, no more console.
    // 3. Improve UI extensively
    // 4. Do extensive tests to check where I can improve AdvancedParser
    // 5. Add some fun and random things
    // 6. Add easter eggs
}
