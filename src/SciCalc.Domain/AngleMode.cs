namespace SciCalc.Domain;

public enum AngleMode
{
    Radians,
    Degrees,
}

public static class AngleModeConversions
{
    public static double ToRadians(this AngleMode mode, double degrees) => degrees * Math.PI / 180;

    public static double ToDegrees(this AngleMode mode, double radians) => radians * 180 / Math.PI;
}
