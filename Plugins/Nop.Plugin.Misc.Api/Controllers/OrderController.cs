using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.Api.Models.BaseModels;
using Nop.Services.Authentication;
using Nop.Services.Orders;
using Nop.Web.Framework.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.Api.Controllers
{
    [Route("api/orders")]
    [ApiController]
    [Authorize(Policy = JwtBearerDefaults.AuthenticationScheme, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class OrderApiController : BaseController
    {
        private readonly IOrderService _orderService;
        private readonly IAuthenticationService _authenticationService;

        public OrderApiController(IOrderService orderService,
            IAuthenticationService authenticationService)
        {
            _orderService = orderService;
            _authenticationService = authenticationService;
        }


        #region Utilities
        /// <summary>
        /// Return response for error
        /// </summary>
        /// <param name="message"></param>
        /// <param name="stausCode"></param>
        /// <returns></returns>
        [NonAction]
        private BaseResponseModel ErrorResponse(string message = "", HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            var response = new BaseResponseModel
            {
                StatusCode = (int)statusCode,
                Message = message,
            };

            //response.Errors.Add(message);
            return response;
        }


        /// <summary>
        /// Return response for success
        /// </summary>
        /// <param name="message"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [NonAction]
        private BaseResponseModel SuccessResponse(string message = "", object data = null, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new BaseResponseModel
            {
                StatusCode = (int)statusCode,
                Message = message,
                Data = data == null ? new List<object>() : data
            };
        }
        #endregion

        [NonAction]
        public override void SaveSelectedCardName(string cardName, bool persistForTheNextRequest = true)
        {
            //keep this method synchronized with
            //"GetSelectedCardName" method of \Nop.Web.Framework\Extensions\HtmlExtensions.cs
            if (string.IsNullOrEmpty(cardName))
                throw new ArgumentNullException(nameof(cardName));

            const string dataKey = "nop.selected-card-name";
            if (persistForTheNextRequest)
            {
                TempData[dataKey] = cardName;
            }
            else
            {
                ViewData[dataKey] = cardName;
            }
        }

        [NonAction]
        public override void SaveSelectedTabName(string tabName = "", bool persistForTheNextRequest = true)
        {
            //default root tab
            SaveSelectedTabName(tabName, "selected-tab-name", null, persistForTheNextRequest);
            //child tabs (usually used for localization)
            //Form is available for POST only
            if (!Request.Method.Equals(WebRequestMethods.Http.Post, StringComparison.InvariantCultureIgnoreCase))
                return;

            foreach (var key in Request.Form.Keys)
                if (key.StartsWith("selected-tab-name-", StringComparison.InvariantCultureIgnoreCase))
                    SaveSelectedTabName(null, key, key["selected-tab-name-".Length..], persistForTheNextRequest);
        }

        [HttpGet]
        public async Task<BaseResponseModel> GetOrders()
        {
            //current customer
            var customer = await _authenticationService.GetAuthenticatedCustomerAsync();
            if (customer is null)
                return ErrorResponse("Unauthorized! Customer not found", HttpStatusCode.Unauthorized);

            var orders = (await _orderService.SearchOrdersAsync()).Select(o => new
            {
                o.Id,
                o.OrderTotal,
                CreatedDate = o.CreatedOnUtc
            });

            return SuccessResponse("Order list", orders, HttpStatusCode.OK);
        }
    }
}
