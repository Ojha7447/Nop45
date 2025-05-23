using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Core;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Plugins;

namespace Nop.Plugin.DiscountRules.CustomDiscount;

public class CustomRequirementRule : BasePlugin, IDiscountRequirementRule
{
    #region Fields
    private readonly IOrderService _orderService;
    private readonly IUrlHelperFactory _urlHelperFactory;
    private readonly ILocalizationService _localizationService;
    private readonly IActionContextAccessor _actionContextAccessor;
    private readonly IWebHelper _webHelper;
    private readonly IDiscountService _discountService;
    #endregion

    #region Ctor
    public CustomRequirementRule(IOrderService orderService,
        IUrlHelperFactory urlHelperFactory,
        IActionContextAccessor actionContextAccessor,
        ILocalizationService localizationService,
        IWebHelper webHelper,
        IDiscountService discountService)
    {
        _orderService = orderService;
        _urlHelperFactory = urlHelperFactory;
        _actionContextAccessor = actionContextAccessor;
        _localizationService = localizationService;
        _webHelper = webHelper;
        _discountService = discountService;
    }
    #endregion

    /// <summary>
    /// Check discount requirement
    /// </summary>
    /// <param name="request">Object that contains all information required to check the requirement (Current customer, discount, etc)</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the result
    /// </returns>
    public async Task<DiscountRequirementValidationResult> CheckRequirementAsync(DiscountRequirementValidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = new DiscountRequirementValidationResult();

        if (request.Customer == null)
            return result;

        var customerOrders = await _orderService.SearchOrdersAsync(customerId: request.Customer.Id);
        if(customerOrders == null) 
            return result;

        result.IsValid = customerOrders.Count >= DiscountRequirementDefaults.CustomerOrderCount;
        return result;
    }

    /// <summary>
    /// Get URL for rule configuration
    /// </summary>
    /// <param name="discountId">Discount identifier</param>
    /// <param name="discountRequirementId">Discount requirement identifier (if editing)</param>
    /// <returns>URL</returns>
    public string GetConfigurationUrl(int discountId, int? discountRequirementId)
    {
        var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);

        return urlHelper.Action("Configure", "CustomDiscount",
            new { discountId = discountId, discountRequirementId = discountRequirementId }, _webHelper.GetCurrentRequestProtocol());
    }

    /// <summary>
    /// Install the plugin
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task InstallAsync()
    {
        //locales
        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.DiscountRules.CustomDiscount.Fields.DiscountId.Required"] = "Discount is required",
            ["Plugins.DiscountRules.CustomDiscount.OnlyApplicableToOrderSubtotal"] = "This plugin only works with discounts where the 'Discount type' is set to 'Assigned to order subtotal'."
        });

        await base.InstallAsync();
    }

    /// <summary>
    /// Uninstall the plugin
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task UninstallAsync()
    {
        //discount requirements
        var discountRequirements = (await _discountService.GetAllDiscountRequirementsAsync())
            .Where(discountRequirement => discountRequirement.DiscountRequirementRuleSystemName == DiscountRequirementDefaults.SystemName);
        foreach (var discountRequirement in discountRequirements)
        {
            await _discountService.DeleteDiscountRequirementAsync(discountRequirement, false);
        }

        await _localizationService.DeleteLocaleResourcesAsync("Plugins.DiscountRules.CustomDiscount");

        await base.UninstallAsync();
    }

}
