using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace aviUtlDropper
{
    public class Dropper
    {
        public static void Drop()
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
            bool isMutexLocked = false;
            try
            {
                // ������܂��h���b�v�X�̃o�[�W��������ǎ��
                // var result = ReadAndValidateGcmzInfo(out var gcmzInfo, mutex != null);
                // copyDataStruct�����
                // lparam�����
                lparam = Marshal.AllocHGlobal(Marshal.SizeOf(data));
            }
            catch (Exception ex)
            {
                // 
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

        #region Win32 API import
        [StructLayout(LayoutKind.Sequential)]
        struct COPYDATASTRUCT
        {
            public UIntPtr Param;
            public int size;
            public IntPtr intPtr;
        }

        private const uint WM_COPYDATA = 0x004A;
        #endregion
    }
}
