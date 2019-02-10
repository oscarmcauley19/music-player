using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ProjectDesign
{
    class PasswordEncrypter
    {
        // Salt & hash lengths are set beforehand
        private const int SaltLength = 16;
        private const int HashLength = 20;

        public static string Hash(string password, int iterations)
        {
            //create salt
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[SaltLength]);

            //create hash
            var encryptionFunction = new Rfc2898DeriveBytes(password, salt, iterations);
            var hash = encryptionFunction.GetBytes(HashLength);

            //combine salt and hash
            var hashBytes = new byte[SaltLength + HashLength];
            Array.Copy(salt, 0, hashBytes, 0, SaltLength);
            Array.Copy(hash, 0, hashBytes, SaltLength, HashLength);

            //convert to string
            var base64Hash = Convert.ToBase64String(hashBytes);

            // format hash with custom stamp, num of iterations
            // and the hash itself
            return string.Format("£OSCARHASH£1£{0}£{1}", iterations, base64Hash);
            //return string.Format("£MYHASH£V1£{0}£{1}", iterations, base64Hash);
        }

        public static string Hash(string password)
        {
            return Hash(password, 10000);
        }

        // Checks for custom stamp to see if hash supported
        public static bool IsHashSupported(string hashString)
        {
            return hashString.Contains("£OSCARHASH£1£");
        }


        // verify entered password against hash from database
        public static bool Verify(string password, string hashedPassword)
        {
            //check hash to prevent error
            if (!IsHashSupported(hashedPassword))
            {
                //throw new NotSupportedException("The hashtype is not supported");
            }

            // Remove custom stamp to get actual hash
            // Split into string array of two cells - 1st gives num of iterations
            // and 2nd gives the actual hash
            var actualHashString = hashedPassword.Replace("£OSCARHASH£1£", "").Split('£');
            var iterations = int.Parse(actualHashString[0]);
            var base64Hash = actualHashString[1];

            // Convert to unsigned 8-bit int array based on characters
            var hashBytes = Convert.FromBase64String(base64Hash);

            // Copies elements from hashBytes starting at index 0
            // and pastes them to another salt array
            // starting at index 0 - does so for num of elements
            // equal to SaltLength (so all elements are copied)
            var salt = new byte[SaltLength];
            Array.Copy(hashBytes, 0, salt, 0, SaltLength);

            // create secure hash from USER-ENTERED PASSWORD
            // using same salt & hash from one in database
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations);
            byte[] hash = pbkdf2.GetBytes(HashLength);

            // Check results against each other
            for (var i = 0; i < HashLength; i++)
            {
                // If any characters fon't add up, pass is rejected
                if (hashBytes[i + SaltLength] != hash[i])
                {
                    return false;
                }
            }
            // If no characters out of place then pass is correct
            return true;
        }
    }
}
