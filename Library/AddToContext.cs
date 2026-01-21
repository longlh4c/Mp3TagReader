using System;

using System.Security.Permissions;
using System.Windows.Forms;
using Microsoft.Win32;

//http://www.howtogeek.com/howto/windows-vista/add-any-application-to-the-desktop-right-click-menu-in-vista/

namespace Mp3TagReader
{
    // Copyright Decebal Mihailescu 2005
    // You can include the following code in your product free of charge
    // as long as you include the existing copyright notice
    //[assembly:RegistryPermissionAttribute(RegistryPermissionAccess.AllAccess)]
    [RegistryPermissionAttribute(SecurityAction.Demand, Unrestricted = true)]
    public class AddToContext
    {
        private const string Command = "Directory\\Background\\shell\\Mp3TagReader List Files\\command";
        private const string MenuName = "Directory\\Background\\shell";

        public static String Add()
        {
            CheckSecurity();
            RegistryKey regmenu = null;
            RegistryKey regcmd = null;
            try
            {
                regmenu = Registry.ClassesRoot.CreateSubKey(MenuName);
                if (regmenu != null)
                    regmenu.SetValue("", "Mp3TagReader List Files");
                regcmd = Registry.ClassesRoot.CreateSubKey(Command);
                if (regcmd != null)
                    regcmd.SetValue("", AppDomain.CurrentDomain.BaseDirectory + "Mp3TagReader.exe");

                return "Added";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return ex.Message;
            }
            finally
            {
                if (regmenu != null)
                    regmenu.Close();
                if (regcmd != null)
                    regcmd.Close();
            }
        }

        public static String Remove()
        {
            try
            {
                RegistryKey reg = Registry.ClassesRoot.OpenSubKey(Command);
                if (reg != null)
                {
                    reg.Close();
                    Registry.ClassesRoot.DeleteSubKey(Command);
                }

                reg = Registry.ClassesRoot.OpenSubKey(MenuName + "\\Mp3TagReader List Files");
                if (reg != null)
                {
                    reg.Close();
                    Registry.ClassesRoot.DeleteSubKey(MenuName + "\\Mp3TagReader List Files");
                }

                return "Removed";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
            }
        }

        private static void CheckSecurity()
        {
            //check registry permissions
            RegistryPermission regPerm;
            regPerm = new RegistryPermission(RegistryPermissionAccess.Write, "HKEY_CLASSES_ROOT\\" + MenuName);
            regPerm.AddPathList(RegistryPermissionAccess.Write, "HKEY_CLASSES_ROOT\\" + Command);
            regPerm.Demand();
        }
    }
}