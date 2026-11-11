class Order { public int X; public int Y; }
class Probe
{
    static void Take(int a, int b) { }
    static void M(Order o) { Take((o.X), (o.Y)); }
}
