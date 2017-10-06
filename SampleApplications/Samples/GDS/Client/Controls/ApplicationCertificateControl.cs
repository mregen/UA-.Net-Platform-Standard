﻿/* ========================================================================
 * Copyright (c) 2005-2017 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using Opc.Ua.Gds;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Opc.Ua.GdsClient
{
    public partial class ApplicationCertificateControl : UserControl
    {
        public ApplicationCertificateControl()
        {
            InitializeComponent();
        }

        private GlobalDiscoveryClientConfiguration m_configuration;
        private GlobalDiscoveryServerMethods m_gds;
        private ServerPushConfigurationMethods m_server;
        private RegisteredApplication m_application;
        private X509Certificate2 m_certificate;
        private string m_certificatePassword;

        public async Task Initialize(
            GlobalDiscoveryClientConfiguration configuration,
            GlobalDiscoveryServerMethods gds,
            ServerPushConfigurationMethods server,
            RegisteredApplication application,
            bool isHttps)
        {
            m_configuration = configuration;
            m_gds = gds;
            m_server = server;
            m_application = application;
            m_certificate = null;
            m_certificatePassword = null;

            CertificateRequestTimer.Enabled = false;
            RequestProgressLabel.Visible = false;
            ApplyChangesButton.Enabled = false;

            CertificateControl.ShowNothing();

            X509Certificate2 certificate = null;

            if (!isHttps)
            {
                if (server.Endpoint != null && server.Endpoint.Description.ServerCertificate != null)
                {
                    certificate = new X509Certificate2(server.Endpoint.Description.ServerCertificate);
                }
                else if (application != null)
                {
                    if (!String.IsNullOrEmpty(application.CertificatePublicKeyPath))
                    {
                        string file = Utils.GetAbsoluteFilePath(application.CertificatePublicKeyPath, true, false, false);

                        if (file != null)
                        {
                            certificate = new X509Certificate2(file);
                        }
                    }
                    else if (!String.IsNullOrEmpty(application.CertificateStorePath))
                    {
                        CertificateIdentifier id = new CertificateIdentifier
                        {
                            StorePath = application.CertificateStorePath
                        };
                        id.StoreType = CertificateStoreIdentifier.DetermineStoreType(id.StorePath);
                        id.SubjectName = application.CertificateSubjectName.Replace("localhost", Utils.GetHostName());

                        certificate = await id.Find(true);
                    }
                }
            }
            else
            {
                if (application != null)
                {
                    if (!String.IsNullOrEmpty(application.HttpsCertificatePublicKeyPath))
                    {
                        string file = Utils.GetAbsoluteFilePath(application.HttpsCertificatePublicKeyPath, true, false, false);

                        if (file != null)
                        {
                            certificate = new X509Certificate2(file);
                        }
                    }
                    else
                    {
                        foreach (string disoveryUrl in application.DiscoveryUrl)
                        {
                            if (Uri.IsWellFormedUriString(disoveryUrl, UriKind.Absolute))
                            {
                                Uri url = new Uri(disoveryUrl);

                                CertificateIdentifier id = new CertificateIdentifier()
                                {
                                    StoreType = CertificateStoreType.X509Store,
                                    StorePath = "CurrentUser\\UA_MachineDefault",
                                    SubjectName = "CN=" + url.DnsSafeHost
                                };

                                certificate = await id.Find();
                            }
                        }
                    }
                }
            }

            if (certificate != null)
            {
                try
                {
                    CertificateControl.Tag = certificate.Thumbprint;
                }
                catch (Exception)
                {
                    MessageBox.Show(
                        Parent,
                        "The certificate does not appear to be valid. Please check configuration settings.",
                        Parent.Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    certificate = null;
                }
            }

            WarningLabel.Visible = certificate == null;

            if (certificate != null)
            {
                m_certificate = certificate;
                CertificateControl.ShowValue(null, "Application Certificate", new CertificateWrapper() { Certificate = certificate }, true);
            }
        }

        private string GetPrivateKeyFormat()
        {
            string privateKeyFormat = "PFX";

            if (m_application.RegistrationType != RegistrationType.ServerPush)
            {
                if (!String.IsNullOrEmpty(m_application.CertificatePrivateKeyPath))
                {
                    if (m_application.CertificatePrivateKeyPath.EndsWith("PEM", StringComparison.OrdinalIgnoreCase))
                    {
                        privateKeyFormat = "PEM";
                    }
                }
            }
            else
            {
                string[] privateKeyFormats = m_server.GetSupportedKeyFormats();

                if (privateKeyFormats == null || !privateKeyFormats.Contains("PFX"))
                {
                    privateKeyFormat = "PEM";
                }
            }

            return privateKeyFormat;
        }

        private string[] GetDomainNames()
        {
            List<string> domainNames = new List<string>();

            if (!String.IsNullOrEmpty(m_application.Domains))
            {
                var domains = m_application.Domains.Split(',');

                List<string> trimmedDomains = new List<string>();

                foreach (var domain in domains)
                {
                    var d = domain.Trim();

                    if (d.Length > 0)
                    {
                        trimmedDomains.Add(d);
                    }
                }

                if (trimmedDomains.Count > 0)
                {
                    return trimmedDomains.ToArray();
                }
            }

            if (m_application.DiscoveryUrl != null)
            {
                foreach (var discoveryUrl in m_application.DiscoveryUrl)
                {
                    if (Uri.IsWellFormedUriString(discoveryUrl, UriKind.Absolute))
                    {
                        string name = new Uri(discoveryUrl).DnsSafeHost;

                        if (name == "localhost")
                        {
                            name = Utils.GetHostName();
                        }

                        bool found = false;

                        foreach (var domainName in domainNames)
                        {
                            if (String.Compare(domainName, name, StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            domainNames.Add(name);
                        }
                    }
                }
            }

            if (domainNames != null && domainNames.Count > 0)
            {
                return domainNames.ToArray();
            }

            if (m_certificate != null)
            {
                var names = Utils.GetDomainsFromCertficate(m_certificate);

                if (names != null && names.Count > 0)
                {
                    domainNames.AddRange(names);
                    return domainNames.ToArray();
                }

                var fields = Utils.ParseDistinguishedName(m_certificate.Subject);

                string name = null;

                foreach (var field in fields)
                {
                    if (field.StartsWith("DC=", StringComparison.Ordinal))
                    {
                        if (name != null)
                        {
                            name += ".";
                        }

                        name += field.Substring(3);
                    }
                }

                if (names != null)
                {
                    domainNames.AddRange(names);
                    return domainNames.ToArray();
                }
            }

            return new string[] { Utils.GetHostName() };
        }

        private async void RequestNewButton_Click(object sender, EventArgs e)
        {
            try
            {
                // check if we already have a private key
                NodeId requestId = null;
                if (!string.IsNullOrEmpty(m_application.CertificateStorePath))
                {
                    CertificateIdentifier id = new CertificateIdentifier();
                    id.StoreType = CertificateStoreIdentifier.DetermineStoreType(m_application.CertificateStorePath);
                    id.StorePath = m_application.CertificateStorePath;
                    id.SubjectName = m_application.CertificateSubjectName.Replace("localhost", Utils.GetHostName());
                    m_certificate = await id.Find(true);
                    if (m_certificate != null &&
                        m_certificate.HasPrivateKey)
                    {
                        m_certificate = await id.LoadPrivateKey(string.Empty);
                    }
                }

                bool hasPrivateKeyFile = false;
                if (!string.IsNullOrEmpty(m_application.CertificatePrivateKeyPath))
                {
                    FileInfo file = new FileInfo(m_application.CertificatePrivateKeyPath);
                    hasPrivateKeyFile = file.Exists;
                }

                var domainNames = GetDomainNames();
                if (m_certificate == null)
                {
                    // no private key
                    requestId = m_gds.StartNewKeyPairRequest(
                        m_application.ApplicationId,
                        null,
                        null,
                        m_application.CertificateSubjectName.Replace("localhost", Utils.GetHostName()),
                        domainNames,
                        "PFX",
                        m_certificatePassword);
                }
                else
                {
                    X509Certificate2 csrCertificate = null;
                    if (m_certificate.HasPrivateKey)
                    {
                        csrCertificate = m_certificate;
                    }
                    else
                    {
                        string absoluteCertificatePrivateKeyPath = Utils.GetAbsoluteFilePath(m_application.CertificatePrivateKeyPath, true, false, false);
                        byte [] pkcsData = File.ReadAllBytes(absoluteCertificatePrivateKeyPath);
                        if (GetPrivateKeyFormat() == "PFX")
                        {
                            csrCertificate = CertificateFactory.CreateCertificateFromPKCS12(pkcsData, m_certificatePassword);
                        }
                        else
                        {
                            csrCertificate = CertificateFactory.CreateCertificateWithPEMPrivateKey(m_certificate, pkcsData, m_certificatePassword);
                        }
                    }
                    byte[] certificateRequest = CertificateFactory.CreateSigningRequest(csrCertificate, domainNames);
                    requestId = m_gds.StartSigningRequest(m_application.ApplicationId, null, null, certificateRequest);
                }

                m_application.CertificateRequestId = requestId.ToString();
                CertificateRequestTimer.Enabled = true;
                RequestProgressLabel.Visible = true;
                WarningLabel.Visible = false;
            }
            catch (Exception ex)
            {
                Opc.Ua.Client.Controls.ExceptionDlg.Show(Text, ex);
            }
        }

        private async void CertificateRequestTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                NodeId requestId = NodeId.Parse(m_application.CertificateRequestId);

                byte[] privateKeyPFX = null;
                byte[][] issuerCertificates = null;

                byte[] certificate = m_gds.FinishRequest(
                    m_application.ApplicationId,
                    requestId,
                    out privateKeyPFX,
                    out issuerCertificates);

                if (certificate == null)
                {
                    // request not done yet, try again in a few seconds
                    return;
                }
                
                CertificateRequestTimer.Enabled = false;
                RequestProgressLabel.Visible = false;

                if (m_application.RegistrationType != RegistrationType.ServerPush)
                {

                    X509Certificate2 newCert = new X509Certificate2(certificate);

                    if (!String.IsNullOrEmpty(m_application.CertificateStorePath) && !String.IsNullOrEmpty(m_application.CertificateSubjectName))
                    {
                        CertificateIdentifier cid = new CertificateIdentifier()
                        {
                            StorePath = m_application.CertificateStorePath,
                            StoreType = CertificateStoreIdentifier.DetermineStoreType(m_application.CertificateStorePath),
                            SubjectName = m_application.CertificateSubjectName.Replace("localhost", Utils.GetHostName())
                        };

                        // update store
                        using (var store = CertificateStoreIdentifier.OpenStore(m_application.CertificateStorePath))
                        {
                            // if we used a CSR, we already have a private key and therefore didn't request one from the GDS
                            // in this case, privateKey is null
                            if (privateKeyPFX == null)
                            {
                                X509Certificate2 oldCertificate = await cid.Find(true);
                                if (oldCertificate != null && oldCertificate.HasPrivateKey)
                                {
                                    oldCertificate = await cid.LoadPrivateKey(string.Empty);
                                    newCert = CertificateFactory.CreateCertificateWithPrivateKey(newCert, oldCertificate);
                                    await store.Delete(oldCertificate.Thumbprint);
                                }
                                else
                                {
                                    throw new ServiceResultException("Failed to merge signed certificate with the private key.");
                                }
                            }
                            else
                            {
                                newCert = new X509Certificate2(privateKeyPFX, string.Empty, X509KeyStorageFlags.Exportable);
                                newCert = CertificateFactory.Load(newCert, true);
                            }

                            await store.Add(newCert);
                        }
                    }
                    else
                    {
                        DialogResult result = DialogResult.Yes;
                        string absoluteCertificatePublicKeyPath = Utils.GetAbsoluteFilePath(m_application.CertificatePublicKeyPath, true, false, false) ?? m_application.CertificatePublicKeyPath;
                        FileInfo file = new FileInfo(absoluteCertificatePublicKeyPath);
                        if (file.Exists)
                        {
                            result = MessageBox.Show(
                                Parent,
                                "Replace certificate " +
                                absoluteCertificatePublicKeyPath +
                                "?",
                                Parent.Text,
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Exclamation);
                        }

                        if (result == DialogResult.Yes)
                        {
                            byte[] exportedCert;
                            if (string.Compare(file.Extension, ".PEM", true) == 0)
                            {
                                exportedCert = CertificateFactory.ExportCertificateAsPEM(newCert);
                            }
                            else
                            {
                                exportedCert = newCert.Export(X509ContentType.Cert);
                            }
                            File.WriteAllBytes(absoluteCertificatePublicKeyPath, exportedCert);
                        }

                        // if we provided a PFX or P12 with the private key, we need to merge the new cert with the private key
                        if (GetPrivateKeyFormat() == "PFX")
                        {
                            string absoluteCertificatePrivateKeyPath = Utils.GetAbsoluteFilePath(m_application.CertificatePrivateKeyPath, true, false, false) ?? m_application.CertificatePrivateKeyPath;
                            file = new FileInfo(absoluteCertificatePrivateKeyPath);
                            if (file.Exists)
                            {
                                result = MessageBox.Show(
                                    Parent,
                                    "Replace private key " +
                                    absoluteCertificatePrivateKeyPath +
                                    "?",
                                    Parent.Text,
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Exclamation);
                            }

                            if (result == DialogResult.Yes)
                            {
                                if (file.Exists)
                                {
                                    byte[] pkcsData = File.ReadAllBytes(absoluteCertificatePrivateKeyPath);
                                    X509Certificate2 oldCertificate = CertificateFactory.CreateCertificateFromPKCS12(pkcsData, m_certificatePassword);
                                    newCert = CertificateFactory.CreateCertificateWithPrivateKey(newCert, oldCertificate);
                                    pkcsData = newCert.Export(X509ContentType.Pfx, m_certificatePassword);
                                    File.WriteAllBytes(absoluteCertificatePrivateKeyPath, pkcsData);

                                    if (privateKeyPFX != null)
                                    {
                                        throw new ServiceResultException("Did not expect a private key for this operation.");
                                    }
                                }
                                else
                                {
                                    File.WriteAllBytes(absoluteCertificatePrivateKeyPath, privateKeyPFX);
                                }
                            }
                        }
                    }

                    // update trust list.
                    if (!String.IsNullOrEmpty(m_application.TrustListStorePath))
                    {
                        using (ICertificateStore store = CertificateStoreIdentifier.OpenStore(m_application.TrustListStorePath))
                        {
                            foreach (byte[] issuerCertificate in issuerCertificates)
                            {
                                X509Certificate2 x509 = new X509Certificate2(issuerCertificate);
                                X509Certificate2Collection certs = await store.FindByThumbprint(x509.Thumbprint);
                                if (certs.Count == 0)
                                {
                                    await store.Add(new X509Certificate2(issuerCertificate));
                                }
                            }
                        }
                    }

                    m_certificate = newCert;

                }
                else
                {
#if TODO_SERVERPUSH
                    if (privateKeyPFX != null && privateKeyPFX.Length > 0)
                    {
                        var x509 = new X509Certificate2(privateKeyPFX, m_certificatePassword, X509KeyStorageFlags.Exportable);
                        privateKeyPFX = x509.Export(X509ContentType.Pfx);
                    }
                    bool applyChanges = m_server.UpdateCertificate(null, null, certificate, GetPrivateKeyFormat(), privateKeyPFX, issuerCertificates);
                    
                    if (applyChanges)
                    {
                        MessageBox.Show(
                            Parent,
                            "The certificate was updated, however, the apply changes command must be sent before the server will use the new certificate.",
                            Parent.Text,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        ApplyChangesButton.Enabled = true;
                    }
#else
                    throw new ServiceResultException("Server Push is not yet implemented.");
#endif
                }

                CertificateControl.ShowValue(null, "Application Certificate", new CertificateWrapper() { Certificate = m_certificate }, true);
            }
            catch (Exception exception)
            {
                var sre = exception as ServiceResultException;

                if (sre != null && sre.StatusCode == StatusCodes.BadNothingToDo)
                {
                    return;
                }

                Opc.Ua.Client.Controls.ExceptionDlg.Show(Text, exception);
                CertificateRequestTimer.Enabled = false;
            }
        }
               
        private void ApplyChangesButton_Click(object sender, EventArgs e)
        {
            try
            {
                m_server.ApplyChanges();
            }
            catch (Exception exception)
            {
                var se = exception as ServiceResultException;

                if (se == null || se.StatusCode != StatusCodes.BadServerHalted)
                {
                    Opc.Ua.Client.Controls.ExceptionDlg.Show(Parent.Text, exception);
                }
            }

            try
            {
                m_server.Disconnect();
            }
            catch (Exception)
            {
                // ignore.
            }
        }

        private void Button_MouseEnter(object sender, EventArgs e)
        {
            ((Control)sender).BackColor = Color.CornflowerBlue;
        }

        private void Button_MouseLeave(object sender, EventArgs e)
        {
            ((Control)sender).BackColor = Color.MidnightBlue;
        }

    }
}
