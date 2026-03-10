using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SantronWinApp.SystemSetup;

namespace SantronWinApp.Services
{
    public class SystemSetupService : BaseSetupService<SystemSetupModel>
    {
        protected override string FolderName => "SystemSetup";

        protected override string DefaultSetupName => "DefaultSetup";

        protected override SystemSetupModel GetDefaultSetup()
        {
            return new SystemSetupModel
            {
                Constant1 = "1500",
                Constant2 = "2000",
                DefualInfusion = "20"
            };
        }
    }
}
