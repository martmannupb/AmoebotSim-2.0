// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AS2
{

    public struct MinMax
    {

        public float min;
        public float max;
        public bool wholeNumbersOnly;

        public MinMax(float min, float max, bool wholeNumbersOnly)
        {
            this.min = min;
            this.max = max;
            this.wholeNumbersOnly = wholeNumbersOnly;
        }

        public static bool operator ==(MinMax minMax1, MinMax minMax2)
        {
            return minMax1.min == minMax2.min && minMax1.max == minMax2.max && minMax1.wholeNumbersOnly == minMax2.wholeNumbersOnly;
        }

        public static bool operator !=(MinMax minMax1, MinMax minMax2)
        {
            return !(minMax1 == minMax2);
        }

        public override bool Equals(object minMax)
        {
            return minMax is MinMax && (MinMax)minMax == this;
        }

        public override int GetHashCode()
        {
            return 42;
        }

        public override string ToString()
        {
            return min + "-" + max + "(" + (wholeNumbersOnly ? "int" : "float") + ")";
        }

        public static MinMax Parse(string text)
        {
            string numbers = text.Substring(0, text.IndexOf('('));
            string number1 = numbers.Substring(0, text.IndexOf('-'));
            string number2 = numbers.Substring(text.IndexOf('-') + 1, text.IndexOf('(') - text.IndexOf('-') - 1);
            Log.Debug("MinMax.Parse: " + numbers + "===" + number1 + "===" + number2); // test
            string type = text.Substring(text.IndexOf('('));
            if (type.Equals("(int)"))
            {
                // int
                int i1 = 0;
                int i2 = 0;
                if (int.TryParse(number1, out i1) == false || int.TryParse(number2, out i2) == false)
                {
                    Log.Error("MinMax: Parse: Conversion for " + text + " not successful! R0. (float numbers stored in MinMax with only whole numbers)");
                    return new MinMax(0, 0, true);
                }
                return new MinMax(i1, i2, true);
            }
            else if (type.Equals("(float)"))
            {
                // float
                TypeConverter.ConversionResult r1 = TypeConverter.ConvertStringToFloat(number1);
                TypeConverter.ConversionResult r2 = TypeConverter.ConvertStringToFloat(number2);
                if (r1.conversionSuccessful == false || r2.conversionSuccessful == false)
                {
                    Log.Error("MinMax: Parse: Conversion for " + text + " not successful! R1.");
                    return new MinMax(0, 0, false);
                }
                return new MinMax((float)r1.obj, (float)r2.obj, false);
            }
            else
            {
                Log.Error("MinMax: Parse: Conversion for " + text + " not successful! R2.");
                return new MinMax(0, 0, true);
            }
        }
    }

} // namespace AS2
