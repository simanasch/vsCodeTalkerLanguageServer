using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Linq;
using System.IO;
using System.Text;
using Codeplex.Data;
using aviUtlDropper.utilities;

namespace aviUtlConnector
{
    public class AviutlConnector
    {
        private const string FileMapName = @"GCMZDrops";
        public static void SendFile(
            IntPtr fromWindowHandle,
            string filePath,
            int layer = 0,
            int stepFrameCount = 0,
            int timeoutMilliseconds = -1)
        {
            SendFiles(
                fromWindowHandle,
                new[] { filePath },
                layer,
                stepFrameCount,
                timeoutMilliseconds);
        }

        public static void SendFiles(
            IntPtr fromWindowHandle,
            string[] filePathes,
            int layer = 0,
            int stepFrameCount = 0,
            int timeoutMilliseconds = -1)
        {
            // TODO:mutex�ɂ�郍�b�N��Ԃ̓N���X�ϐ��ɂ���
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
            bool isMutexLocked = false;
            try
            {
                GcmzDropsProjectConfig gcmzDropsData = null;
                var data = new COPYDATASTRUCT();
                var dataAddress = IntPtr.Zero;
                var lparam = IntPtr.Zero;
                // ������܂��h���b�v�X�̃o�[�W��������ǎ��
                var handle = OpenFileMapping(FILE_MAP_READ, false, FileMapName);
                dataAddress = MapViewOfFile(handle, FILE_MAP_READ, 0, 0, UIntPtr.Zero);
                var GcmzDropsData =
                    (GcmzDropsProjectConfig.CurrentLayout)Marshal.PtrToStructure(
                        dataAddress,
                        typeof(GcmzDropsProjectConfig.CurrentLayout));
                gcmzDropsData = new GcmzDropsProjectConfig(ref GcmzDropsData);
                if (dataAddress != IntPtr.Zero)
                {
                    UnmapViewOfFile(dataAddress);
                }
                if (handle != IntPtr.Zero)
                {
                    CloseHandle(handle);
                }
                // �����𕪂���̂ł�����ւ��gcmzDropsData�����Ԃ�?
                // wav�t�@�C�����J���A�t�@�C���p�X����^���ʂ���
                // copyDataStruct�����
                data = CreateCopyDataStruct(filePathes, layer);
                // lparam�����
                lparam = Marshal.AllocHGlobal(Marshal.SizeOf(data));
                Marshal.StructureToPtr(data, lparam, false);
                // sendMessage�̌Ăяo��
                SendMessage(gcmzDropsData.WindowHandle, WM_COPYDATA, fromWindowHandle, lparam);
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
            String[] files,
            int layer,
            int frameAdvance = 0)
        {
            // ���t���[���i�߂邩�̎擾����
            foreach(String filePath in files)
            {
                // .wav�t�@�C������frameAdvance���ݒ肳��ĂȂ��ꍇ�A�ŏ���wav�t�@�C���̒��������i�߂�
                String fileExt = Path.GetExtension(filePath);
                if(fileExt.ToLower() == ".wav" && frameAdvance <= 0)
                {
                    // �ԋp�l���~���b�Ȃ̂�/1000*�t���[�����[�g���i�߂�ׂ��t���[����
                    double rawFrameCount = Audio.getAudioFileMilliSecondsLength(filePath);
                    frameAdvance = (int)Math.Round(rawFrameCount / 1000 * 60);
                    break;
                }
            }
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
