using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace Project
{
    internal class AdvancedParser // Honestly, this took two days to figure out
    {
        int InvalidInputCheck(string text, out string message)
        {
            // Code 0: Err: For empty input
            // Code 1: Err: Invalid character
            // Code 2: Err: Operators used incorrectly
            // Code 3: Err: Invalid operator placements relative to parenthesis
            // Code 4: Err: Invalid use of decimal numbers
            // Code 5: Err: Amount of opening brackets doesn't match amount of closing brackets
            // Code 6: Ok

            message = "Ok"; // this code is about as good as this ok
            if (text == "") return 0;
            text = Regex.Replace(text, @"\)\(", ")*(");

            if (Regex.Match(text, @"[^\d+*\-/^.()]") is Match match && match.Success) { message = $"The input '{match}' is not allowed.\n\n"; return 1; }
            else if (Regex.Match(text, @"([-*+/^.])\1+") is Match _match && _match.Success)
            {
                if (_match.Value.ElementAt(0) == '-') message = $"The input '{_match.Value}' was too long. Maybe you forgot to incase a negative number in brackets?\nEx. 2-(-1)\n\n";
                else message = $"The input '{_match.Value}' was too long.\nCorrect: '{_match.Value.ElementAt(0)}'\n\n";
                return 2;
            }
            else if (Regex.Match(text, @"\([\^*/.]|[\^*=./+-]\)") is Match __match && __match.Success)
            {
                message = $"The input '{__match.Value}' is a invalid use case of parenthesies.\n\n";
                return 3;
            }
            else if (Regex.Match(text, @"(\d+\.(?!\d))|((?<!\d)\.\d+)|((?<!\d)\.(?!\d))") is Match ___match && ___match.Success)
            {
                message = $"The input '{___match.Value}' is invalid use case of decimal numbers.\n\n";
                return 4;
            }
            else
            {
                int counter = default;
                foreach (Match i in Regex.Matches(text, "[()]")) counter += 1;
                if (counter % 2 != 0) { message = $"The amount of opening brackets, does not match the amount of closing brackets.\n\n"; return 5; }
            }
            return 6;
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
        double ReturnAnswer(List<string> list, out bool success)
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

            if (InvalidInputCheck(text, out string _message) is int errCode && errCode != 6)
            {
                success = false;
                message = _message;
                return errCode;
            }

            List<string> bracketed = ReturnBracketed(text);
            double result = ReturnAnswer(bracketed, out bool _success);
            if (!_success) { message = "You're equation evaluates to a number too big. Overflow.\n\n"; success = false; return 0; }
            return result;
        }
    }
    internal class Calculator
    {
        AdvancedParser advParse = new AdvancedParser();

        private string Equation = "1+1";
        private string UpdateMessage = "";
        private bool Exit = false;
        private bool ResultSuccess;
        private string ResultError;
        private List<string> HistoryList = new List<string>();
        private double Result => advParse.EvaluateEquasion(Equation, out ResultSuccess, out ResultError);
        string ParseInput(string input)
        {
            switch(input)
            {
                case "help":
                    UpdateMessage += "Firstly,\nthis calculator allows parenthesis, exponentation, multiplication, division, addition, subtraction.\n\nSecondly,\nThe commands available to you are 'help', 'history', 'stats' and 'exit'\n\n";
                    break;
                case "history":
                    History("Return");
                    break;
                case "stats":
                    break;
                case "exit":
                    Exit = true;
                    break;
                default:
                    return "Invalid";
            }
            return "Ok";
        }
        void History(string option)
        {
            if (option == "Add")
            {
                HistoryList.Add($"{HistoryList.Count + 1}. '{Equation}' = '{Result}'");
            }
            else if (option == "Return")
            {
                UpdateMessage += "\n\nCurrent history:\n";
                foreach (string s in HistoryList) UpdateMessage += $"{s}\n";
            }
            else return;
        }
        void StartingMessage() // Needs some work
        {
            double grabResult = Result; // Even though 'Equation' is initialized before 'Result', it still causes advParse.EvaluateEquation to take an empty string at startup, which this solves.
                                        // I tried setting ResultError to true on initialization, but it doesn't help
            if (UpdateMessage == "") Console.Write($"Input an equation or,\nwrite 'help' for more info.");
            else { Console.Write(UpdateMessage); UpdateMessage = ""; };
            if (!ResultSuccess) Console.Write($"\n\nYour current equation is invalid.\n\n'{Equation}'\nErr Code {grabResult}: {ResultError}");
            else Console.Write($"\n\nCurrent equation: {Equation}\nAnswer: {grabResult}\n\n");
            Console.Write("> ");
        }

        public int UpdateDisplay() // Could be better
        {
            if (Exit) return 0;

            Console.Clear();
            StartingMessage();
            History("Add");

            string userInput = Console.ReadLine();

            if (String.IsNullOrEmpty(userInput)) return 1;
            else if (ParseInput(userInput) == "Ok") return 1;
            else { Equation = userInput; return 1; }
        }
    }
    internal class Calculator
    {
        static void Main()
        {

            Calculator calculator = new Calculator();

            Console.Write("Welcome to Calculator.exe!\n\n\nPress enter to continue\n> ");
            Console.ReadLine();

            while (calculator.UpdateDisplay() != 0) ;

            Console.Clear();
            Console.Write("Exited\n> ");
            Console.ReadLine();
        }
    }
    // Todo:
    // 1. Make a working history list.
    // 2. Learn how to make a real UI, no more console.
    // 3. Do extensive tests to check where I can improve AdvancedParser
    // 4. Add some fun and random things
    // 5. Add easter eggs
}
