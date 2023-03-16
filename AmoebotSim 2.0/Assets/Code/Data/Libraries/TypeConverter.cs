using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AS2
{

    /// <summary>
    /// Utility class for converting strings to various types.
    /// </summary>
    public static class TypeConverter
    {

        private static bool floatsUseComma = 0.5f.ToString().Contains(",");

        /// <summary>
        /// Converts a string to an object of the given type.
        /// Supported types are bool, int, float, string and enum.
        /// </summary>
        /// <param name="type">The desired type.</param>
        /// <param name="text">The string that should be converted.</param>
        /// <returns>An object of the specified <paramref name="type"/>,
        /// if possible, otherwise the original input string <paramref name="text"/>.</returns>
        public static object ConvertStringToObjectOfType(Type type, string text)
        {
            if (type == typeof(bool)) return bool.Parse(text);
            else if (type == typeof(int)) return int.Parse(text);
            else if (type == typeof(float)) return ConvertStringToFloat(text).obj;
            else if (type == typeof(string)) return text;
            else if (type.IsSubclassOf(typeof(Enum))) return Enum.Parse(type, text);
            else
            {
                Log.Error("TypeConverter: ConvertStringToObjectOfType: For text " + text + " and type " + type.ToString() + " there is not conversion method available. Returning text.");
                return text;
            }
        }

        /// <summary>
        /// Result of the conversion. Check
        /// <see cref="conversionSuccessful"/> to see if it worked.
        /// </summary>
        public struct ConversionResult
        {
            public object obj;
            public bool conversionSuccessful;

            public ConversionResult(object obj, bool conversionSuccessful)
            {
                this.obj = obj;
                this.conversionSuccessful = conversionSuccessful;
            }
        }

        /// <summary>
        /// Depending on the local settings, there are multiple possibilities how to convert
        /// a string to float (, or . can be used).
        /// Here we try out all possible ways to convert until we find the right one. :)
        /// </summary>
        /// <param name="text">Text to convert to float.</param>
        /// <returns>A <see cref="ConversionResult"/> that represents the outcome of
        /// the conversion attempt.</returns>
        public static ConversionResult ConvertStringToFloat(string text)
        {
            float output;
            if (floatsUseComma && float.TryParse(text.Replace('.', ','), out output) || floatsUseComma == false && float.TryParse(text.Replace(',', '.'), out output))
            {
                return new ConversionResult(output, true);
            }
            return new ConversionResult(0, false);
        }

        /// <summary>
        /// We might want to deliver strings that are easily converted to floats by float.Parse(..)
        /// to other classes. This is the method for that.
        /// </summary>
        /// <param name="text">Text to convert to convertible text.</param>
        /// <returns>A modified string that can be parsed into a float value easily.</returns>
        public static string ConvertStringInStringThatCanBeConvertedToFloat(string text)
        {
            float output;
            if (floatsUseComma && float.TryParse(text.Replace('.', ','), out output)) return text.Replace('.', ',');
            else if (floatsUseComma == false && float.TryParse(text.Replace(',', '.'), out output)) return text.Replace(',', '.');
            else return text; // not possible
        }

        /// <summary>
        /// Detects if the local settings use a comma for the conversion of float to string
        /// and string to float.
        /// </summary>
        /// <returns><c>true</c> if and only if the local settings use the comma
        /// ',' instead of the period '.' as decimal point.</returns>
        public static bool FloatsUseCommaInsteadOfPoint()
        {
            return floatsUseComma;
        }

    }

} // namespace AS2
