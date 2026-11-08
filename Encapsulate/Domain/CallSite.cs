using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Encapsulate.Domain;

/// <summary>
/// The separate syntactic arguments of one invocation, object creation or constructor initializer.
/// Each argument contributes its outermost member access (plan R5); reduced extension receivers,
/// default values and implicit arguments are not separate arguments (plan R6).
/// </summary>
public sealed class CallSite(IEnumerable<IArgumentOperation> arguments)
{
    private IReadOnlyList<MemberAccess> CountedArguments => arguments
        .Where(IsSeparateArgument)
        .SelectMany(ValueOperations)
        .Select(value => new MemberAccess(Unwrap(value)))
        .Where(access => access.IsCountable)
        .ToList();

    public IReadOnlyList<SmellFinding> CollectFindings(string path) => CountedArguments
        .GroupBy(access => access.ReceiverKey)
        .Select(group => new Receiver(group.Key, group))
        .Where(receiver => receiver.HasRepeatedAccess)
        .Select(receiver => receiver.CreateFinding(path, SmellFinding.PrimitiveObsession))
        .ToList();

    private bool IsSeparateArgument(IArgumentOperation argument) =>
        argument.ArgumentKind is not ArgumentKind.DefaultValue
        && (IsExpandedParams(argument) || !argument.IsImplicit || IsWrittenArgument(argument.Syntax));

    private bool IsExpandedParams(IArgumentOperation argument) =>
        argument.ArgumentKind is ArgumentKind.ParamArray or ArgumentKind.ParamCollection;

    private bool IsWrittenArgument(SyntaxNode syntax) =>
        syntax is ArgumentSyntax || syntax.Ancestors().SkipWhile(node => node is ParenthesizedExpressionSyntax).FirstOrDefault() is ArgumentSyntax;

    private IEnumerable<IOperation> ValueOperations(IArgumentOperation argument) =>
        IsExpandedParams(argument)
            ? ExpandedElements(Unwrap(argument.Value))
            : [argument.Value];

    private IEnumerable<IOperation> ExpandedElements(IOperation value) => value switch
    {
        IArrayCreationOperation { Initializer: not null } array => array.Initializer!.ElementValues,
        ICollectionExpressionOperation collection => collection.Elements,
        _ => [value],
    };

    private IOperation Unwrap(IOperation value) => value switch
    {
        IConversionOperation conversion => Unwrap(conversion.Operand),
        IParenthesizedOperation parenthesized => Unwrap(parenthesized.Operand),
        IConditionalAccessOperation { WhenNotNull: not null } conditional => Unwrap(conditional.WhenNotNull),
        _ => value,
    };
}
