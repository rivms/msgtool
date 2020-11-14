using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace azmsg.common
{
    class CertificateUtilities
    {
        public static async Task<X509Certificate2> LoadPemCACertificate(string caCertFile, string privateKey)
        {
            var caCert = new X509Certificate2(caCertFile);


            return await Task.FromResult(caCert);
        }

        public static async Task<X509Certificate2> LoadPfxCACertificate(string caCertFile, string password)
        {
            X509Certificate2 cert = null;
            if (password == null)
            {
                cert = new X509Certificate2(caCertFile);
            }
            else
            {
                cert = new X509Certificate2(caCertFile, password);
            }

            return await Task.FromResult(cert);
        }


        public static void RegisterCert2(X509Certificate2 caCert)
        {
            using (var caCertStore = new X509Store(StoreName.Root, StoreLocation.CurrentUser))
            {
                caCertStore.Open(OpenFlags.ReadWrite);
                caCertStore.Add(caCert);
                caCertStore.Close();
            }
        }
    }
}
