using System.Reflection;
using VMSService.Core.Static;
using Xunit;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Ticket 02's explicitly requested regression check: none of the four renamed status strings or
/// two typo'd action-name strings should remain anywhere <see cref="WithdrawConstants"/> exposes.
/// </summary>
public class WithdrawConstantsNoLegacyStringsTests
{
    private static readonly string[] OldStatusStrings =
    [
        "Pending RTGS Verification",
        "Pending Withdrawal Confirmation",
        "Verification Failed",
        "Reject Exceptional Approval",
    ];

    private static readonly string[] OldTypoedActionNames =
    [
        "ApproveToRTGSVefification",
        "ExceptionalWithdrawaRequest",
    ];

    [Fact]
    public void WithdrawalStatusNames_Does_Not_Expose_Any_Old_Status_String()
        => AssertNoConstantEquals(typeof(WithdrawConstants.WithdrawalStatusNames), OldStatusStrings);

    [Fact]
    public void WithdrawalStatusActionNames_Does_Not_Expose_Any_Old_Typoed_ActionName()
        => AssertNoConstantEquals(typeof(WithdrawConstants.WithdrawalStatusActionNames), OldTypoedActionNames);

    private static void AssertNoConstantEquals(Type constantsClass, string[] bannedValues)
    {
        var constantValues = constantsClass
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (Name: f.Name, Value: (string)f.GetRawConstantValue()!))
            .ToList();

        foreach (var banned in bannedValues)
        {
            var offender = constantValues.FirstOrDefault(c => c.Value == banned);
            Assert.True(offender.Name is null,
                $"{constantsClass.Name}.{offender.Name} still equals the old value \"{banned}\".");
        }
    }
}
