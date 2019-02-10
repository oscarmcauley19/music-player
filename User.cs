using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectDesign
{
    static class User
    {
        // These attributes are ALL PRIVATE - this is shorthand
        // way of writing get-set methods in C#
        public static string username { get; set; }
        public static int id { get; set; }
        public static bool remember { get; set; }

        // Sets current user details
        static public void Login(int inputID, string inputName)
        {
            id = inputID;
            username = inputName;
        }

        // Removes current user details
        static public void Logout()
        {
            User.id = -1;
            User.username = null;

            // User no longer remembered on start-up
            Properties.Settings.Default.RememberUser = false;
        }
    }
}
