using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Linq;
using System.IO;
using System.Text;
using Codeplex.Data;

namespace aviUtlDropper
{
    public class Dropper
    {
        private const string FileMapName = @"GCMZDrops";
        public static void Drop(
            IntPtr ownWindowHandle,
            IEnumerable<string> filePathes,
            int stepFrameCount = 0,
            int layer = 0,
            int timeoutMilliseconds = -1)
        {
            Mutex mutex = null;
            // mutex�ɂ��r�����䂪�ł���ꍇ�Amutex�ɂ�郍�b�N���擾����
            try
            {
                Mutex.TryOpenExisting(@"GCMZDropsMutex", out mutex);
            }
            catch (Exception ex)
            {
                Console.WriteLine("mutex�J�����˂�" + ex);
            }
            var data = new COPYDATASTRUCT();
            var lparam = IntPtr.Zero;
            var handle = IntPtr.Zero;
            bool isMutexLocked = false;
            var dataAddress = IntPtr.Zero;
            GcmzDropsData gcmzDropsData = null;
            try
            {
                // ������܂��h���b�v�X�̃o�[�W��������ǎ��
                // var result = ReadAndValidateGcmzInfo(out var gcmzInfo, mutex != null);
                handle = OpenFileMapping(FILE_MAP_READ, false, FileMapName);
                dataAddress = MapViewOfFile(handle, FILE_MAP_READ, 0, 0, UIntPtr.Zero);
                var GcmzDropsData =
                    (GcmzDropsData.CurrentLayout)Marshal.PtrToStructure(
                        dataAddress,
                        typeof(GcmzDropsData.CurrentLayout));
                gcmzDropsData = new GcmzDropsData(ref GcmzDropsData);
                if (dataAddress != IntPtr.Zero)
                {
                    UnmapViewOfFile(dataAddress);
                }
                if (handle != IntPtr.Zero)
                {
                    CloseHandle(handle);
                }
                // copyDataStruct�����
                data = CreateCopyDataStruct(layer, stepFrameCount, filePathes);
                // lparam�����
                lparam = Marshal.AllocHGlobal(Marshal.SizeOf(data));
                Marshal.StructureToPtr(data, lparam, false);
                // sendMessage�̌Ăяo��
                 SendMessage(gcmzDropsData.WindowHandle, WM_COPYDATA, ownWindowHandle, lparam);
            }
            catch (Exception ex)
            {
                // 
                Console.WriteLine("�Ȃ񂩗�����"+ex);
            }
            finally
            {
                // mutex�ɂ�郍�b�N�̉���
                if (mutex != null)
                {
                    if (isMutexLocked)
                    {
                        mutex.ReleaseMutex();
                    }
                    mutex.Dispose();
                }
            }
        }
        /// <summary>
        /// �w������܂��h���b�v�X�x v0.3.12 �ȍ~�p�� COPYDATASTRUCT �l���쐬����B
        /// </summary>
        /// <param name="layer">���C���[�ʒu�w��B</param>
        /// <param name="frameAdvance">�h���b�v��ɐi�߂�t���[�����B</param>
        /// <param name="files">�t�@�C���p�X�񋓁B</param>
        /// <returns>
        /// COPYDATASTRUCT �l�B
        /// DataAddress �t�B�[���h�͗��p��� Marshal.FreeHGlobal �ŉ������K�v������B
        /// </returns>
        private static COPYDATASTRUCT CreateCopyDataStruct(
            int layer,
            int frameAdvance,
            IEnumerable<string> files)
        {
            // ���MJSON������쐬
            var json = DynamicJson.Serialize(new { layer, frameAdvance, files });
            var data = Encoding.UTF8.GetBytes(json);

            // COPYDATASTRUCT �쐬
            var cds = new COPYDATASTRUCT();
            try
            {
                cds.Param = new UIntPtr(1);
                cds.size = data.Length;
                cds.intPtr = Marshal.AllocHGlobal(data.Length);
                Marshal.Copy(data, 0, cds.intPtr, data.Length);
            }
            catch
            {
                if (cds.intPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(cds.intPtr);
                }
                throw;
            }

            return cds;
        }


        #region Win32 API import
        [StructLayout(LayoutKind.Sequential)]
        struct COPYDATASTRUCT
        {
            public UIntPtr Param;
            public int size;
            public IntPtr intPtr;
        }
        // ������܂��h���b�v�X�̃t�@�C���}�b�s���O�ǂݍ��݊֌W
        private const uint FILE_MAP_READ = 0x0004;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr OpenFileMapping(
            uint desiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
            string name);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr MapViewOfFile(
            IntPtr fileMapHandle,
            uint desiredAccess,
            uint fileOffsetHigh,
            uint fileOffsetLow,
            UIntPtr numberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UnmapViewOfFile(IntPtr address);

        // �f�X�N�g�b�v�̃E�B���h�E�擾

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr windowHandle, uint flags);
        // ������܂��h���b�v�X��API(sendMessage)�֌W
        private const uint WM_COPYDATA = 0x004A;
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SendMessage(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            IntPtr lparam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SendMessage(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            string lparam);

        #endregion
    }
}
