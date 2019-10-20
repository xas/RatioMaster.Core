using System;
using System.Text;

namespace RatioMaster.Core.Helpers
{
    public class RandomStringGenerator
    {
        private readonly char[] characterArray;
        private readonly Random randomNumbersGenerator;

        public RandomStringGenerator()
        {
            characterArray = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
            randomNumbersGenerator = new Random();
        }

        public char GetRandomCharacter()
        {
            return characterArray[(int)((characterArray.GetLength(0)) * randomNumbersGenerator.NextDouble())];
        }

        public string Generate(int stringLength)
        {
            return Generate(stringLength, false);
        }

        public string Generate(int stringLength, bool randomness)
        {
            var stringBuilder = new StringBuilder { Capacity = stringLength };
            for (int count = 0; count <= stringLength - 1; count++)
            {
                if (randomness)
                {
                    stringBuilder.Append((char)randomNumbersGenerator.Next(255));
                }
                else
                {
                    stringBuilder.Append(GetRandomCharacter());
                }
            }

            return stringBuilder.ToString();
        }

        public string Generate(int stringLength, char[] charArray)
        {
            var stringBuilder = new StringBuilder { Capacity = stringLength };
            for (int count = 0; count <= stringLength - 1; count++)
            {
                stringBuilder.Append(charArray[(int)((charArray.GetLength(0)) * randomNumbersGenerator.NextDouble())]);
            }

            return stringBuilder.ToString();
        }

        public string Generate(string inputString, bool upperCase)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < inputString.Length; i = i + 1)
            {
                if (char.IsLetterOrDigit(inputString[i]) && inputString[i] < 127)
                {
                    result.Append(inputString[i]);
                }
                else
                {
                    result.Append('%');
                    string temp = Convert.ToString(inputString[i], 16);
                    if (upperCase)
                    {
                        temp = temp.ToUpper();
                    }

                    if (temp.Length == 1)
                    {
                        result.Append('0').Append(temp);
                    }
                    else
                    {
                        result.Append(temp);
                    }
                }
            }

            return result.ToString();
        }
    }
}
