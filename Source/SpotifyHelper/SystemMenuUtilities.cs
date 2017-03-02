using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using User32 = PInvoke.User32;

namespace SpotifyHelper
{
    public class SystemMenuUtilities : ISimpleModule
    {
        //TODO: Make PR to PInvoke project to get these included/working

        [DllImport("user32.dll")]
        private static extern bool DeleteMenu(IntPtr hMenu, uint uPosition, uint uFlags);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWinEventHook(User32.WindowsEventHookType eventMin, User32.WindowsEventHookType eventMax, IntPtr hmodWinEventProc, WinEventProc lpfnWinEventProc, int idProcess, int idThread, User32.WindowsEventHookFlags dwFlags);

        private delegate void WinEventProc(IntPtr hWinEventHook, uint iEvent, IntPtr hWnd, int idObject, int idChild, int dwEventThread, uint dwmsEventTime);

        private readonly IntPtr _systemMenuHandle;
        private readonly User32.SafeEventHookHandle _windowHook;

        private readonly Dictionary<int, Action> _actions = new Dictionary<int, Action>();

        private const int StartingCommandId = 45000;
        private int _nextId = StartingCommandId;

        private readonly WinEventProc _proc;
        private readonly IntPtr _mainWindowHandle;


        private void HookHandler(IntPtr hwineventhook, uint @event, IntPtr hwnd, int idobject, int idchild, int dweventthread, uint dwmseventtime)
        {
            const int OBJID_SYSMENU = -1;
            if (@event == (uint)User32.WindowsEventHookType.EVENT_OBJECT_INVOKED && idobject == OBJID_SYSMENU)
            {
                var cmd = idchild;
                if (_actions.ContainsKey(cmd))
                    _actions[cmd]();
            }
        }

        public void AddMenuItem(string text, Action onClick, bool precedeWithSeparator)
        {
            if (precedeWithSeparator)
            {
                var s =User32.AppendMenu(_systemMenuHandle, User32.MenuItemFlags.MF_SEPARATOR, new IntPtr(_nextId++), "");
                if(!s)
                    throw new Win32Exception();
            }

            _actions[_nextId] = onClick;
            var s2 = User32.AppendMenu(_systemMenuHandle, User32.MenuItemFlags.MF_STRING, new IntPtr(_nextId++), text);
            if (!s2)
                throw new Win32Exception();
        }

        public void Dispose(bool resetMenu)
        {
            GC.KeepAlive(_proc);
            _windowHook?.Dispose();
            foreach (var commandId in Enumerable.Range(StartingCommandId, _nextId - StartingCommandId))
            {
                DeleteMenu(_systemMenuHandle, (uint) commandId, 0);
            }
            if (resetMenu && _mainWindowHandle != IntPtr.Zero)
            {
                User32.GetSystemMenu(_mainWindowHandle, bRevert: true);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public SystemMenuUtilities(SpotifyInstance instance)
        {
            _proc = new WinEventProc(HookHandler);
            _mainWindowHandle = instance.MainWindowHandle;
            User32.GetSystemMenu(_mainWindowHandle, bRevert: true);
            _systemMenuHandle = User32.GetSystemMenu(_mainWindowHandle, bRevert: false);

            int processId;
            var threadId = User32.GetWindowThreadProcessId(_mainWindowHandle, out processId);

            var handle = SetWinEventHook(User32.WindowsEventHookType.EVENT_OBJECT_INVOKED,
                User32.WindowsEventHookType.EVENT_OBJECT_INVOKED, IntPtr.Zero, _proc, processId, threadId,
                User32.WindowsEventHookFlags.WINEVENT_OUTOFCONTEXT);


            _windowHook = new User32.SafeEventHookHandle(handle);

            AddMenuItem("Hello!", () => MessageBox.Show("HI!"), precedeWithSeparator: true);
        }
    }
}
