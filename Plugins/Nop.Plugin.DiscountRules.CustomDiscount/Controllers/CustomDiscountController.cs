using Nop.Plugin.DiscountRules.CustomDiscount.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nop.Core.Domain.Discounts;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.DiscountRules.CustomDiscount.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.Admin)]
[AutoValidateAntiforgeryToken]
public class CustomDiscount : BasePluginController
{
    #region Fields

    protected readonly ICustomerService _customerService;
    protected readonly IDiscountService _discountService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IPermissionService _permissionService;
    protected readonly ISettingService _settingService;

    #endregion

    #region Ctor

    public CustomDiscount(ICustomerService customerService,
        IDiscountService discountService,
        ILocalizationService localizationService,
        IPermissionService permissionService,
        ISettingService settingService)
    {
        _customerService = customerService;
        _discountService = discountService;
        _localizationService = localizationService;
        _permissionService = permissionService;
        _settingService = settingService;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Get errors message from model state
    /// </summary>
    /// <param name="modelState">Model state</param>
    /// <returns>Errors message</returns>
    protected IEnumerable<string> GetErrorsFromModelState(ModelStateDictionary modelState)
    {
        return ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
    }

    #endregion

    #region Methods

    public async Task<IActionResult> Configure(int discountId, int? discountRequirementId)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageDiscounts))
            return Content("Access denied");

        //load the discount
        var discount = await _discountService.GetDiscountByIdAsync(discountId)
                       ?? throw new ArgumentException("Discount could not be loaded");

        if (discount.DiscountType != DiscountType.AssignedToOrderSubTotal)
            return Content(await _localizationService.GetResourceAsync("Plugins.DiscountRules.CustomDiscount.OnlyApplicableToOrderSubtotal"));

        //check whether the discount requirement exists
        if (discountRequirementId.HasValue && await _discountService.GetDiscountRequirementByIdAsync(discountRequirementId.Value) is null)
            return Content("Failed to load requirement.");

        var model = new RequirementModel
        {
            RequirementId = discountRequirementId ?? 0,
            DiscountId = discountId,
        };
       
        return View("~/Plugins/DiscountRules.CustomerOrders/Views/Configure.cshtml", model);
    }

    [HttpPost]
    public async Task<IActionResult> Configure(RequirementModel model)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageDiscounts))
            return Content("Access denied");

        if (ModelState.IsValid)
        {
            //load the discount
            var discount = await _discountService.GetDiscountByIdAsync(model.DiscountId);
            if (discount == null)
                return NotFound(new { Errors = new[] { "Discount could not be loaded" } });

            if (discount.DiscountType != DiscountType.AssignedToOrderSubTotal)
                return Content(await _localizationService.GetResourceAsync("Plugins.DiscountRules.CustomDiscount.OnlyApplicableToOrderSubtotal"));

            //get the discount requirement
            var discountRequirement = await _discountService.GetDiscountRequirementByIdAsync(model.RequirementId);

            //the discount requirement does not exist, so create a new one
            if (discountRequirement == null)
            {
                discountRequirement = new DiscountRequirement
                {
                    DiscountId = discount.Id,
                    DiscountRequirementRuleSystemName = DiscountRequirementDefaults.SystemName
                };

                await _discountService.InsertDiscountRequirementAsync(discountRequirement);
            }

            return Ok(new { NewRequirementId = discountRequirement.Id });
        }

        return Ok(new { Errors = GetErrorsFromModelState(ModelState) });
    }

    #endregion
}
