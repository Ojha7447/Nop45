using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Misc.Api.Models.TokenModels;
using Nop.Plugin.Misc.Api.Models.BaseModels;
using Nop.Services.Authentication;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Web.Framework.Controllers;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Nop.Plugin.Misc.Api.Controllers
{
    public class TokenController : BaseController
    {
        #region Fields
        private readonly ILocalizationService _localizationService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IAuthenticationService _authenticationService;
        private readonly ICustomerRegistrationService _customerRegistrationService;
        private readonly ICustomerService _customerService;
        private readonly CustomerSettings _customerSettings;
        #endregion

        #region Ctor
        public TokenController(ILocalizationService localizationService,
            ICustomerActivityService customerActivityService,
            IAuthenticationService authenticationService,
            ICustomerRegistrationService customerRegistrationService,
            ICustomerService customerService,
            CustomerSettings customerSettings)
        {
             _localizationService = localizationService;
            _customerActivityService = customerActivityService;
            _authenticationService = authenticationService;
            _customerRegistrationService = customerRegistrationService;
            _customerService = customerService;
            _customerSettings = customerSettings;
        }
        #endregion

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


        private int GetTokenExpiryInDays()
        {
            return 1;
        }

        private async Task<TokenResponse> GenerateToken(Customer customer)
        {
            var currentTime = DateTimeOffset.Now;
            var expirationTime = currentTime.AddDays(GetTokenExpiryInDays());

            var claims = new List<Claim>
                         {
                             new Claim(JwtRegisteredClaimNames.Nbf, currentTime.ToUnixTimeSeconds().ToString()),
                             new Claim(JwtRegisteredClaimNames.Exp, expirationTime.ToUnixTimeSeconds().ToString()),
                             new Claim("CustomerId", customer.Id.ToString()),
                             new Claim(ClaimTypes.NameIdentifier, customer.CustomerGuid.ToString()),
                         };

            if (!string.IsNullOrEmpty(customer.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, customer.Email));
            }

            if (_customerSettings.UsernamesEnabled)
            {
                if (!string.IsNullOrEmpty(customer.Username))
                {
                    claims.Add(new Claim(ClaimTypes.Name, customer.Username));
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(customer.Email))
                {
                    claims.Add(new Claim(ClaimTypes.Name, customer.Email));
                }
            }

            string securityKey = "62499582-2692-4fce-8349-d55a2261debd-62499582-2692-4fce-8349-d55a2261debd-4fce-8349-d55a2261debd-4fce-8349-d55a2261debd";

            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey)), SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(new JwtHeader(signingCredentials), new JwtPayload(claims));
            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            return await Task.FromResult(new TokenResponse(accessToken, currentTime.UtcDateTime, expirationTime.UtcDateTime)
            {
                CustomerId = customer.Id,
                CustomerGuid = customer.CustomerGuid,
                Username = _customerSettings.UsernamesEnabled ? customer.Username : customer.Email,
                TokenType = "Bearer",
            });
        }
        #endregion

        #region Methods

        [HttpPost]
        [AllowAnonymous]
        [Route("/token", Name = "RequestToken")]
        [ProducesResponseType(typeof(TokenResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Forbidden)]
        public async Task<BaseResponseModel> GetToken([FromBody] TokenRequestModel model)
        {

            Customer oldCustomer = await _authenticationService.GetAuthenticatedCustomerAsync();
            Customer newCustomer;

           if (string.IsNullOrEmpty(model.Username))
               return ErrorResponse("Missing username", HttpStatusCode.BadRequest);

           if (string.IsNullOrEmpty(model.Password))
               return ErrorResponse("Missing password", HttpStatusCode.BadRequest);

           //validate customer
           var loginResult = await _customerRegistrationService.ValidateCustomerAsync(model.Username,model.Password);
           switch (loginResult)
                {
                    case CustomerLoginResults.Successful:
                        {
                            newCustomer = await (_customerSettings.UsernamesEnabled
                                      ? _customerService.GetCustomerByUsernameAsync(model.Username)
                                      : _customerService.GetCustomerByEmailAsync(model.Username));
                            break;
                        }
                    case CustomerLoginResults.CustomerNotExist:
                        return ErrorResponse(await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.CustomerNotExist"));
                    case CustomerLoginResults.Deleted:
                        return ErrorResponse(await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.Deleted"));
                    case CustomerLoginResults.NotActive:
                        return ErrorResponse(await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.NotActive"));
                    case CustomerLoginResults.NotRegistered:
                        return ErrorResponse(await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.NotRegistered"));
                    case CustomerLoginResults.LockedOut:
                        return ErrorResponse(await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.LockedOut"));
                    case CustomerLoginResults.WrongPassword:
                    default:
                        return ErrorResponse(await _localizationService.GetResourceAsync("Account.Login.WrongCredentials"));
                }

           //confirm not null
           if (newCustomer is null)
               return ErrorResponse("Wrong username or password", HttpStatusCode.Forbidden);

            var message = "Token";
            //Customer Account not approved
            if (!newCustomer.Active)
            {
                message = await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.AccountNotApproved");
                return ErrorResponse(message, HttpStatusCode.BadRequest);
            }

            //token response
            var tokenResponse = await GenerateToken(newCustomer);

            await _authenticationService.SignInAsync(newCustomer, false); // update cookie-based authentication - not needed for api, avoids automatic generation of guest customer with each request to api

            await _customerActivityService.InsertActivityAsync(newCustomer, "Api.TokenRequest", "API token request", newCustomer);

            //send success even if registeration otp not verified error exist.
            return SuccessResponse(message, tokenResponse);
        }

        [HttpGet]
        [Authorize(Policy = JwtBearerDefaults.AuthenticationScheme, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Route("/token/check", Name = "ValidateToken")]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
        public async Task<BaseResponseModel> ValidateToken()
        {
            Customer currentCustomer = await _authenticationService.GetAuthenticatedCustomerAsync(); // this gets customer entity from db if it exists
            if (currentCustomer is null)
                return ErrorResponse("Invalid! Customer not found", HttpStatusCode.NotFound);

            return SuccessResponse("Valid token", "Authorized");
        }
        #endregion
    }
}
