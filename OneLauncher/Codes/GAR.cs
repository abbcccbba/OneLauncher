using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OneLauncher.Core;
namespace OneLauncher.Codes
{
    public struct GAR
    {
        public readonly static string BasePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/OneLauncher/";
        public static DBManger configManger;

        //public static UserModel DefaultUserModel;
    }
}
