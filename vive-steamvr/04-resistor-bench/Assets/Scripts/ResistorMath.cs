using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>A resistor type as stocked in a bin: value, tolerance, rated power, and whether it is an anti-sulfur type.</summary>
public sealed class PartSpec
{
    public readonly double Ohms, TolerancePct, RatedWatts;
    public readonly bool AntiSulfur;

    public PartSpec(double ohms, double tolerancePct, double ratedWatts, bool antiSulfur)
    {
        Ohms = ohms;
        TolerancePct = tolerancePct;
        RatedWatts = ratedWatts;
        AntiSulfur = antiSulfur;
    }

    public string Describe()
    {
        return ResistorMath.FormatOhms(Ohms) + " ohm, " + TolerancePct.ToString("0.##", CultureInfo.InvariantCulture) + "%, "
            + ResistorMath.WattsText(RatedWatts) + (AntiSulfur ? ", anti-sulfur" : "");
    }
}

public enum LedState { Off, Dim, Normal, Over, Burnt }

/// <summary>Result of checking a part against the LED circuit requirements.</summary>
public sealed class CheckResult
{
    public bool ValueOk, ToleranceOk, PowerOk, FitsOk;
    public double NeededOhms, StandardOhms, RequiredWatts, DeratedWatts;
    public readonly List<string> Problems = new List<string>();
    public bool Passed { get { return ValueOk && ToleranceOk && PowerOk && FitsOk; } }
}

/// <summary>
/// Pure functions (no Unity types) for the resistor bench: color code, standard values, Ohm's law for the LED
/// circuit, power derating, and a simulated meter reading. General education only.
/// Color indices: 0 black, 1 brown, 2 red, 3 orange, 4 yellow, 5 green, 6 blue, 7 violet, 8 grey, 9 white, 10 gold, 11 silver.
/// </summary>
public static class ResistorMath
{
    public static readonly string[] ColorNames = { "black", "brown", "red", "orange", "yellow", "green", "blue", "violet", "grey", "white", "gold", "silver" };

    static readonly double[] E24Mantissa =
    {
        1.0, 1.1, 1.2, 1.3, 1.5, 1.6, 1.8, 2.0, 2.2, 2.4, 2.7, 3.0,
        3.3, 3.6, 3.9, 4.3, 4.7, 5.1, 5.6, 6.2, 6.8, 7.5, 8.2, 9.1,
    };

    static bool Near(double a, double b) { return Math.Abs(a - b) < 1e-9; }

    /// <summary>Splits a positive value into mantissa in [1,10) and a decade exponent.</summary>
    static int Decade(double v, out double mantissa)
    {
        int e = 0;
        mantissa = v;
        while (mantissa >= 10.0) { mantissa /= 10.0; e++; }
        while (mantissa < 1.0) { mantissa *= 10.0; e--; }
        return e;
    }

    // ------------------------------------------------------------------ color code

    /// <summary>Tolerance band color index for a tolerance in percent, or -1 if there is no standard color.</summary>
    public static int ToleranceColor(double pct)
    {
        if (Near(pct, 1)) return 1;
        if (Near(pct, 2)) return 2;
        if (Near(pct, 0.5)) return 5;
        if (Near(pct, 0.25)) return 6;
        if (Near(pct, 0.1)) return 7;
        if (Near(pct, 0.05)) return 8;
        if (Near(pct, 5)) return 10;
        if (Near(pct, 10)) return 11;
        return -1;
    }

    /// <summary>Tolerance in percent for a band color index, or -1 if that color is not a tolerance color.</summary>
    public static double ToleranceFromColor(int color)
    {
        switch (color)
        {
            case 1: return 1;
            case 2: return 2;
            case 5: return 0.5;
            case 6: return 0.25;
            case 7: return 0.1;
            case 8: return 0.05;
            case 10: return 5;
            case 11: return 10;
            default: return -1;
        }
    }

    /// <summary>Multiplier for a band color index (0..9 = 10^n, gold 0.1, silver 0.01), or -1 if invalid.</summary>
    public static double Multiplier(int color)
    {
        if (color >= 0 && color <= 9) return Math.Pow(10.0, color);
        if (color == 10) return 0.1;
        if (color == 11) return 0.01;
        return -1;
    }

    /// <summary>
    /// Color bands for a value and tolerance: 5 bands (3 digits, multiplier, tolerance) when the tolerance is 2% or
    /// better, otherwise 4 bands. Returns null when the value or tolerance cannot be coded.
    /// </summary>
    public static int[] ColorBands(double ohms, double tolerancePct)
    {
        if (!(ohms > 0) || double.IsInfinity(ohms)) return null;
        int tolColor = ToleranceColor(tolerancePct);
        if (tolColor < 0) return null;
        int digits = tolerancePct <= 2.0 ? 3 : 2;
        double ignored;
        int e = Decade(ohms, out ignored) - (digits - 1);
        long mant = (long)Math.Round(ohms / Math.Pow(10.0, e));
        long limit = digits == 3 ? 1000 : 100;
        if (mant >= limit) { mant /= 10; e++; }
        int multColor = (e >= 0 && e <= 9) ? e : (e == -1 ? 10 : (e == -2 ? 11 : -1));
        if (multColor < 0) return null;
        var bands = new int[digits + 2];
        if (digits == 3)
        {
            bands[0] = (int)(mant / 100);
            bands[1] = (int)((mant / 10) % 10);
            bands[2] = (int)(mant % 10);
        }
        else
        {
            bands[0] = (int)(mant / 10);
            bands[1] = (int)(mant % 10);
        }
        bands[digits] = multColor;
        bands[digits + 1] = tolColor;
        return bands;
    }

    /// <summary>Reads a 4- or 5-band color code back to ohms. Returns -1 for an invalid code.</summary>
    public static double ValueFromBands(int[] bands)
    {
        if (bands == null || (bands.Length != 4 && bands.Length != 5)) return -1;
        int digits = bands.Length - 2;
        double mantissa = 0;
        for (int i = 0; i < digits; i++)
        {
            if (bands[i] < 0 || bands[i] > 9) return -1;
            mantissa = mantissa * 10 + bands[i];
        }
        double mult = Multiplier(bands[digits]);
        if (mult < 0) return -1;
        return Math.Round(mantissa * mult, 9);
    }

    // ------------------------------------------------------------------ circuit

    /// <summary>Series resistor for an LED: R = (Vs - Vf) / I.</summary>
    public static double LedResistor(double supplyV, double ledDropV, double currentA)
    {
        return (supplyV - ledDropV) / currentA;
    }

    /// <summary>LED current for a series resistor. Infinity when the resistance is zero or negative.</summary>
    public static double LedCurrent(double supplyV, double ledDropV, double ohms)
    {
        if (ohms <= 0) return double.PositiveInfinity;
        return Math.Max(0.0, (supplyV - ledDropV) / ohms);
    }

    /// <summary>P = I x I x R.</summary>
    public static double PowerW(double currentA, double ohms) { return currentA * currentA * ohms; }

    /// <summary>
    /// Generic linear derating: full rated power up to 70 C, falling to 0 W at 155 C.
    /// (Illustrative curve, not any manufacturer's datasheet.)
    /// </summary>
    public static double DeratedWatts(double ratedWatts, double ambientC)
    {
        if (ambientC <= 70.0) return ratedWatts;
        if (ambientC >= 155.0) return 0.0;
        return ratedWatts * (155.0 - ambientC) / 85.0;
    }

    /// <summary>Simulated meter reading: the nominal value plus a repeatable error within half the tolerance.</summary>
    public static double MeasuredOhms(double nominal, double tolerancePct, int seed)
    {
        double s = Math.Sin(seed * 12.9898 + 78.233) * 43758.5453;
        double u = s - Math.Floor(s);
        return nominal * (1.0 + (u * 2.0 - 1.0) * 0.5 * tolerancePct / 100.0);
    }

    /// <summary>True when the measured value agrees with the nominal value within the tolerance.</summary>
    public static bool Confirms(double measured, double nominal, double tolerancePct)
    {
        return Math.Abs(measured - nominal) <= nominal * tolerancePct / 100.0 + 1e-9;
    }

    /// <summary>Nearest E24 standard value (by ratio).</summary>
    public static double NearestStandard(double ohms)
    {
        if (!(ohms > 0) || double.IsInfinity(ohms)) return 0;
        double m;
        int e = Decade(ohms, out m);
        double best = E24Mantissa[0];
        double bestErr = double.MaxValue;
        foreach (double c in E24Mantissa)
        {
            double err = Math.Abs(Math.Log(m / c));
            if (err < bestErr) { bestErr = err; best = c; }
        }
        if (Math.Abs(Math.Log(m / 10.0)) < bestErr) best = 10.0;
        return Math.Round(best * Math.Pow(10.0, e), 9);
    }

    /// <summary>LED behavior for a given current (generic 20 mA LED with a 30 mA absolute limit).</summary>
    public static LedState ClassifyLed(double currentA)
    {
        if (currentA > 0.030) return LedState.Burnt;
        if (currentA > 0.0235) return LedState.Over;
        if (currentA >= 0.0165) return LedState.Normal;
        if (currentA > 0.0001) return LedState.Dim;
        return LedState.Off;
    }

    /// <summary>
    /// Checks a stocked part for the LED circuit: nearest standard value, tolerance, derated power against the
    /// dissipation, and whether the body fits the board pads.
    /// </summary>
    public static CheckResult CheckPart(PartSpec p, double supplyV, double ledDropV, double targetAmps, double ambientC, double maxTolerancePct, double maxFitWatts)
    {
        var r = new CheckResult();
        r.NeededOhms = LedResistor(supplyV, ledDropV, targetAmps);
        r.StandardOhms = NearestStandard(r.NeededOhms);
        double amps = LedCurrent(supplyV, ledDropV, r.StandardOhms);
        r.RequiredWatts = PowerW(amps, r.StandardOhms);
        r.DeratedWatts = DeratedWatts(p.RatedWatts, ambientC);

        r.ValueOk = Math.Abs(Math.Log(p.Ohms / r.StandardOhms)) < 0.005;
        r.ToleranceOk = p.TolerancePct <= maxTolerancePct + 1e-9;
        r.PowerOk = r.DeratedWatts >= r.RequiredWatts;
        r.FitsOk = p.RatedWatts <= maxFitWatts + 1e-9;

        if (!r.ValueOk)
            r.Problems.Add("Value " + FormatOhms(p.Ohms) + " ohm is wrong: the circuit needs " + FormatOhms(r.NeededOhms) + " ohm, nearest standard " + FormatOhms(r.StandardOhms) + " ohm.");
        if (!r.ToleranceOk)
            r.Problems.Add("Tolerance " + p.TolerancePct.ToString("0.##", CultureInfo.InvariantCulture) + "% is looser than the " + maxTolerancePct.ToString("0.##", CultureInfo.InvariantCulture) + "% the card allows.");
        if (!r.PowerOk)
            r.Problems.Add(WattsText(p.RatedWatts) + " derates to " + r.DeratedWatts.ToString("0.000", CultureInfo.InvariantCulture) + " W at " + ambientC.ToString("0", CultureInfo.InvariantCulture)
                + " C, below the " + r.RequiredWatts.ToString("0.000", CultureInfo.InvariantCulture) + " W the circuit dissipates.");
        if (!r.FitsOk)
            r.Problems.Add(WattsText(p.RatedWatts) + " is oversize: the pads fit up to " + WattsText(maxFitWatts) + ", and a 1/8 W part already derates to "
                + DeratedWatts(0.125, ambientC).ToString("0.000", CultureInfo.InvariantCulture) + " W, enough for " + r.RequiredWatts.ToString("0.000", CultureInfo.InvariantCulture) + " W.");
        return r;
    }

    // ------------------------------------------------------------------ text

    /// <summary>Compact value text such as "150", "4.7 k", "1 M" (callers add "ohm").</summary>
    public static string FormatOhms(double ohms)
    {
        var inv = CultureInfo.InvariantCulture;
        if (ohms >= 1e6) return (ohms / 1e6).ToString("0.##", inv) + " M";
        if (ohms >= 1e3) return (ohms / 1e3).ToString("0.##", inv) + " k";
        return ohms.ToString("0.#", inv);
    }

    public static string WattsText(double watts)
    {
        if (Near(watts, 0.0625)) return "1/16 W";
        if (Near(watts, 0.125)) return "1/8 W";
        if (Near(watts, 0.25)) return "1/4 W";
        if (Near(watts, 0.5)) return "1/2 W";
        return watts.ToString("0.###", CultureInfo.InvariantCulture) + " W";
    }
}
