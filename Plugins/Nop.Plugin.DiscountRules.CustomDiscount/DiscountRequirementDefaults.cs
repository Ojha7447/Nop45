namespace Nop.Plugin.DiscountRules.CustomDiscount;
public class DiscountRequirementDefaults
{
    /// <summary>
    /// The system name of the discount requirement rule
    /// </summary>
    public static string SystemName => "DiscountRequirement.CustomDiscount";

    /// <summary>
    /// Discount will be apply after number of orders
    /// </summary>
    public static int CustomerOrderCount => 3;
}
