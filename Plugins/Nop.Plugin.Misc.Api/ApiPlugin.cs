using Microsoft.VisualBasic;
using Nop.Core.Domain.Customers;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Plugins;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.Api
{
    public class ApiPlugin : BasePlugin, IMiscPlugin
    {
        #region Fields
        private readonly ICustomerService _customerService;
        #endregion

        #region Ctor
        public ApiPlugin(ICustomerService customerService)
        {
           _customerService = customerService;
        }
        #endregion

        public override async Task InstallAsync()
        {
            var apiRole = await _customerService.GetCustomerRoleBySystemNameAsync(Constants.API_ROLE_SYSTEM_NAME);

            if (apiRole == null)
            {
                apiRole = new CustomerRole
                {
                    Name = Constants.API_ROLE_NAME,
                    Active = true,
                    SystemName = Constants.API_ROLE_SYSTEM_NAME
                };

                await _customerService.InsertCustomerRoleAsync(apiRole);
            }
            else if (apiRole.Active == false)
            {
                apiRole.Active = true;
                await _customerService.UpdateCustomerRoleAsync(apiRole);
            }

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            var apiRole = await _customerService.GetCustomerRoleBySystemNameAsync(Constants.API_ROLE_SYSTEM_NAME);
            if (apiRole != null)
            {
                apiRole.Active = false;
                await _customerService.UpdateCustomerRoleAsync(apiRole);
            }

            await base.UninstallAsync();
        }
    }

}
