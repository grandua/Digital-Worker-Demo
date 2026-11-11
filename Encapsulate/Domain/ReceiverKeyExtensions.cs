namespace Encapsulate.Domain;

/// <summary>Receiver-key predicates shared by the smell rules; the receiver key is the owner of the containing-instance vocabulary.</summary>
public static class ReceiverKeyExtensions
{
    /// <summary>Whether a receiver key names the containing instance; Feature Envy excludes these receivers.</summary>
    public static bool IsContainingInstance(this string key) => key is Receiver.ThisKey or Receiver.BaseKey;
}
