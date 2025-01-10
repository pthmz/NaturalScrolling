using Microsoft.Win32;
using System;
using System.Management;
namespace NaturalScrolling
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Traverse the registry and modify specific key values
            TraverseAndModifyRegistry();

            // Restart mouse devices
            RestartMouseDevices();
        }

        static void TraverseAndModifyRegistry()
        {
            string registryPath = @"SYSTEM\CurrentControlSet\Enum\HID";
            try
            {
                using (RegistryKey rootKey = Registry.LocalMachine.OpenSubKey(registryPath))
                {
                    if (rootKey != null)
                    {
                        RecursiveTraversal(rootKey, "");
                    }
                    else
                    {
                        Console.WriteLine("The specified registry path does not exist or could not be opened.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            Console.WriteLine("Traversal completed.");
        }

        static void RecursiveTraversal(RegistryKey key, string path)
        {
            try
            {
                // Get all subkey names under the current path
                string[] subKeyNames = key.GetSubKeyNames();
                foreach (string subKeyName in subKeyNames)
                {
                    // Construct full path
                    string fullPath = path + "\\" + subKeyName;

                    // Output full path (trim leading backslash)
                    Console.WriteLine("Traverse the registry path: " + fullPath.TrimStart('\\'));

                    // Recursively traverse subkeys
                    using (RegistryKey subKey = key.OpenSubKey(subKeyName))
                    {
                        if (subKey != null)
                        {
                            if (subKeyName == "Device Parameters")
                            {
                                ModifyFlipFlopWheel(subKey);
                            }
                            else
                            {
                                RecursiveTraversal(subKey, fullPath);
                            }
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // When encountering insufficient permissions, log the error and continue traversal
                Console.WriteLine($"Skipping path {path} due to insufficient permissions: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Catch other types of exceptions, and output error information
                Console.WriteLine($"An error occurred at path {path}: {ex.Message}");
            }
        }

        static void ModifyFlipFlopWheel(RegistryKey deviceParametersKey)
        {
            try
            {
                // Try to open the Device Parameters subkey in read-write mode
                using (RegistryKey writableKey = deviceParametersKey.OpenSubKey("", true))
                {
                    if (writableKey != null)
                    {
                        // Set the value of FlipFlopWheel to 1
                        writableKey.SetValue("FlipFlopWheel", 1, RegistryValueKind.DWord);
                        Console.WriteLine("Successfully set FlipFlopWheel to 1.");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // If there's not enough permission to modify the registry key, print an error message and continue
                Console.WriteLine($"Unable to set FlipFlopWheel: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while setting FlipFlopWheel: {ex.Message}");
            }
        }

        static void RestartMouseDevices()
        {
            string query = "SELECT * FROM Win32_PnPEntity WHERE ClassGuid='{4D36E96F-E325-11CE-BFC1-08002BE10318}'";

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                {
                    bool foundDevices = false;
                    foreach (ManagementObject device in searcher.Get())
                    {
                        foundDevices = true;
                        Console.WriteLine("=== Device Information ===");
                        Console.WriteLine($"  Caption: {device["Caption"] ?? "N/A"}");
                        Console.WriteLine($"  Description: {device["Description"] ?? "N/A"}");
                        Console.WriteLine($"  Manufacturer: {device["Manufacturer"] ?? "N/A"}");
                        Console.WriteLine($"  DeviceID: {device["DeviceID"] ?? "N/A"}");
                        Console.WriteLine($"  PNPDeviceID: {device["PNPDeviceID"] ?? "N/A"}");
                        Console.WriteLine($"  Status: {device["Status"] ?? "N/A"}");
                        Console.WriteLine();

                        RestartDevice(device);
                    }

                    if (!foundDevices)
                    {
                        Console.WriteLine("No pointer devices were found.");
                    }
                }
            }
            catch (ManagementException ex)
            {
                Console.WriteLine($"An error occurred while querying WMI: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            }
        }

        static void RestartDevice(ManagementObject device)
        {
            try
            {
                // Create an input parameters object
                ManagementBaseObject inParams = null;

                // Disable the device
                inParams = device.GetMethodParameters("Disable");
                ManagementBaseObject outParams = device.InvokeMethod("Disable", inParams, null);
                if ((uint)outParams["ReturnValue"] == 0)
                {
                    Console.WriteLine($"Successfully disabled device: {device["Caption"] ?? "N/A"}");

                    // Wait for a moment to ensure the disable action is complete
                    System.Threading.Thread.Sleep(1000);

                    // Enable the device
                    inParams = device.GetMethodParameters("Enable");
                    outParams = device.InvokeMethod("Enable", inParams, null);
                    if ((uint)outParams["ReturnValue"] == 0)
                    {
                        Console.WriteLine($"Successfully enabled device: {device["Caption"] ?? "N/A"}");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to enable device: {device["Caption"] ?? "N/A"}");
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to disable device: {device["Caption"] ?? "N/A"}");
                }
            }
            catch (ManagementException ex)
            {
                Console.WriteLine($"An error occurred while restarting the device: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred while restarting the device: {ex.Message}");
            }
        }
    }
}