class Order { public int X; public int Y; }
class Sample {
    event System.Action E = Make(new Order().X, new Order().Y);
    static System.Action Make(int x, int y) => null;
    void M(Order o) { Make((o.X), (o.Y)); }
}
