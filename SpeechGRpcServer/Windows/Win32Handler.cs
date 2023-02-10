using System;
using System.Windows;
using System.Runtime.InteropServices; // DLL Import

public class Win32Handler{
  [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
  private static extern IntPtr GetDesktopWindow();
}