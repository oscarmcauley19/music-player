using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectDesign
{
    static class User
    {
        public static string username { get; set; }
        public static int id { get; set; }
        public static bool remember { get; set; }

        static public void Login(int inputID, string inputName)
        {
            id = inputID;
            username = inputName;
        }

        static public void Logout()
        {
            User.id = -1;
            User.username = null;
            Properties.Settings.Default.RememberUser = false;
        }
    }
}
