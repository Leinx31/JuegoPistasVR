using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace CrimeVR.Editor
{
    public sealed class MetaAndroidNamespaceFixPostprocessor : IPostGenerateGradleAndroidProject
    {
        private static readonly IReadOnlyDictionary<string, string> NamespaceByAarName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "InteractionSdk.aar", "com.oculus.integration.interactionsdk" },
            { "SDKTelemetry.aar", "com.oculus.integration.sdktelemetry" },
            { "OVRPlugin.aar", "com.oculus.integration.ovrplugin" }
        };

        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            try
            {
                string libsDirectory = Path.Combine(path, "unityLibrary", "libs");
                if (!Directory.Exists(libsDirectory))
                    return;

                foreach (KeyValuePair<string, string> entry in NamespaceByAarName)
                {
                    string aarPath = Path.Combine(libsDirectory, entry.Key);
                    if (!File.Exists(aarPath))
                        continue;

                    RewriteAarManifestPackage(aarPath, entry.Value);
                }

                Debug.Log("Meta Android namespace fix applied to generated AAR manifests.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to rewrite Meta Android AAR manifests: {exception}");
                throw;
            }
        }

        private static void RewriteAarManifestPackage(string aarPath, string manifestPackage)
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), $"CrimeVR_{Path.GetFileNameWithoutExtension(aarPath)}_{Guid.NewGuid():N}");
            string extractedRoot = Path.Combine(tempRoot, "extracted");
            string rebuiltZipPath = Path.Combine(tempRoot, Path.GetFileName(aarPath));

            Directory.CreateDirectory(extractedRoot);

            try
            {
                ZipFile.ExtractToDirectory(aarPath, extractedRoot);

                string manifestPath = Path.Combine(extractedRoot, "AndroidManifest.xml");
                if (!File.Exists(manifestPath))
                    return;

                XDocument manifest = XDocument.Load(manifestPath, LoadOptions.PreserveWhitespace);
                XElement root = manifest.Root;
                if (root == null)
                    return;

                XAttribute packageAttribute = root.Attribute("package");
                if (packageAttribute == null)
                {
                    root.Add(new XAttribute("package", manifestPackage));
                }
                else
                {
                    packageAttribute.Value = manifestPackage;
                }

                manifest.Save(manifestPath, SaveOptions.DisableFormatting);

                if (File.Exists(rebuiltZipPath))
                    File.Delete(rebuiltZipPath);

                ZipFile.CreateFromDirectory(extractedRoot, rebuiltZipPath, System.IO.Compression.CompressionLevel.Optimal, false);
                File.Copy(rebuiltZipPath, aarPath, true);
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, true);
            }
        }
    }
}
