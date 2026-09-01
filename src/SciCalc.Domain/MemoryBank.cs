namespace SciCalc.Domain;

public sealed class MemoryBank
{
    private double? m1;
    private double? m2;
    private double? m3;

    public void Store(MemorySlotId slot, double value) => Slot(slot) = value;

    public double? Recall(MemorySlotId slot) => Slot(slot);

    public void Clear(MemorySlotId slot) => Slot(slot) = null;

    public bool IsNonEmpty(MemorySlotId slot) => Slot(slot) is not null;

    private ref double? Slot(MemorySlotId slot)
    {
        switch (slot)
        {
            case MemorySlotId.M1: return ref m1;
            case MemorySlotId.M2: return ref m2;
            default: return ref m3;
        }
    }
}
